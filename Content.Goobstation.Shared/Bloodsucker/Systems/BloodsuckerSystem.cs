using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Flash;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.Containers;
using Content.Shared.Alert;
using Content.Shared.Damage.Events;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Physics.Events;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Goobstation.Shared.Bloodsuckers.Systems;

/// <summary>
/// Handles all passive effects of the Bloodsucker antagonist.
/// </summary>
public sealed partial class BloodsuckerSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerComponent, FlashDurationMultiplierEvent>(OnFlashAttempt);
        SubscribeLocalEvent<BloodsuckerComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<BloodsuckerComponent, SleepStateChangedEvent>(OnSleepStateChanged);
        SubscribeLocalEvent<BloodsuckerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BloodsuckerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BloodsuckerComponent, ComponentInit>(OnBloodsuckerInit);
        SubscribeLocalEvent<BloodsuckerComponent, ComponentShutdown>(OnBloodsuckerShutdown);
        SubscribeLocalEvent<BloodsuckerComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<BloodsuckerComponent, ProjectileReflectAttemptEvent>(OnProjectileHitAttempt);
        SubscribeLocalEvent<BloodsuckerComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<BloodsuckerComponent, AttackedEvent>(OnAttacked);
    }

    private void OnBloodsuckerInit(Entity<BloodsuckerComponent> ent, ref ComponentInit args)
    {
        _alerts.ShowAlert(ent.Owner, ent.Comp.BloodAlert, 0);
        _alerts.ShowAlert(ent.Owner, ent.Comp.RankAlert, 0);
        _alerts.ShowAlert(ent.Owner, ent.Comp.SolAlert, 0);
    }

    private void OnBloodsuckerShutdown(Entity<BloodsuckerComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.BloodAlert);
        _alerts.ClearAlert(ent.Owner, ent.Comp.RankAlert);
        _alerts.ClearAlert(ent.Owner, ent.Comp.SolAlert);
    }

    private void OnFlashAttempt(Entity<BloodsuckerComponent> ent, ref FlashDurationMultiplierEvent args)
    {
        var ev = new BloodsuckerFlashedEvent(1f);
        RaiseLocalEvent(ent, ref ev);

        args.Multiplier *= ev.ModifiedDuration * ent.Comp.FlashDurationMultiplier;
    }

    // Bullets pass straight through the undead.
    private void OnPreventCollide(Entity<BloodsuckerComponent> ent, ref PreventCollideEvent args)
    {
        if (IsBullet(ent, args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnProjectileHitAttempt(Entity<BloodsuckerComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (IsBullet(ent, args.ProjUid))
            args.Cancelled = true;
    }

    private bool IsBullet(Entity<BloodsuckerComponent> ent, EntityUid projectile)
    {
        return !ent.Comp.IsMasquerading
            && HasComp<ProjectileComponent>(projectile)
            && TryComp<ReflectiveComponent>(projectile, out var reflective)
            && (reflective.Reflective & ReflectType.NonEnergy) != 0;
    }

    private void OnBeforeStaminaDamage(Entity<BloodsuckerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (ent.Comp.IsMasquerading || args.Value <= 0 || !HasComp<ProjectileComponent>(args.Source))
            return;

        // Projectile stamina damage ignores resistances, so cancel it and deal the reduced amount instead.
        args.Cancelled = true;
        _stamina.TakeStaminaDamage(ent, args.Value * ent.Comp.ProjectileStaminaMultiplier, ignoreResist: true);
    }

    // Only blessed weapons cut deep into the undead.
    private void OnAttacked(EntityUid uid, BloodsuckerComponent comp, AttackedEvent args)
    {
        if (comp.IsMasquerading || HasComp<BlessedWeaponComponent>(args.Used))
            return;

        args.ModifiersList.Add(comp.UnblessedMeleeModifiers);
        _popup.PopupPredicted(Loc.GetString("bloodsucker-melee-resisted", ("target", uid)), uid, args.User);
    }

    private void OnDamageModify(Entity<BloodsuckerComponent> ent, ref DamageModifyEvent args)
    {
        // Skip immunities while masquerading
        if (ent.Comp.IsMasquerading)
            return;

        // Hitscan lasers are the only damage whose origin is the gun itself.
        if (HasComp<GunComponent>(args.Origin))
            args.Damage *= ent.Comp.HitscanDamageMultiplier;

        // Negative damage = healing
        if (args.Damage.DamageDict.TryGetValue("Burn", out var burn) && burn < 0)
        {
            var cancel = new BloodsuckerBurnHealAttemptEvent();
            RaiseLocalEvent(ent, ref cancel);

            if (cancel.Cancelled)
            {
                args.Damage.DamageDict["Burn"] = 0;
                return;
            }

            if (ent.Comp.ClaimedCoffin is not { } coffin || !EntityManager.EntityExists(coffin))
            {
                args.Damage.DamageDict["Burn"] = 0;
                return;
            }

            if (!IsNearCoffin(ent, coffin))
            {
                args.Damage.DamageDict["Burn"] = 0;
                return;
            }
        }

        if (args.Damage.DamageDict.TryGetValue("Toxin", out _))
            args.Damage.DamageDict["Toxin"] = 0;

        if (args.Damage.DamageDict.TryGetValue("Asphyxiation", out _))
            args.Damage.DamageDict["Asphyxiation"] = 0;

        if (args.Damage.DamageDict.TryGetValue("Bloodloss", out _))
            args.Damage.DamageDict["Bloodloss"] = 0;
    }

    private void OnSleepStateChanged(Entity<BloodsuckerComponent> ent, ref SleepStateChangedEvent args)
    {
        if (!args.FellAsleep)
            return;

        // Immediately wake the vampire back up because they aren't supposed to sleep
        _statusEffects.TryRemoveStatusEffect(ent, "ForcedSleep");
        _statusEffects.TryRemoveStatusEffect(ent, "Unconscious");
    }

    private void OnMobStateChanged(Entity<BloodsuckerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        if (ent.Comp.IsMasquerading)
            return;

        // Force the entity back to alive — bloodsuckers skip crit entirely
        _mobState.ChangeMobState(ent, MobState.Alive);
    }

    private void OnExamined(Entity<BloodsuckerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsMasquerading)
            return;

        args.PushMarkup(
            $"[color=lightblue]{Loc.GetString("bloodsucker-examined-pale", ("target", ent.Owner))}[/color]");

        if (!TryComp<BloodsuckerHumanityComponent>(ent, out var humanity))
            return;

        if (humanity.CurrentHumanity < humanity.GatedActionMinHumanity)
            args.PushMarkup($"[color=red]{Loc.GetString("bloodsucker-examined-monster", ("target", ent.Owner))}[/color]");
        else if (humanity.CurrentHumanity < humanity.HighHumanity)
            args.PushMarkup($"[color=red]{Loc.GetString("bloodsucker-examined-eyes", ("target", ent.Owner))}[/color]");
    }

    #region helpers

    /// <summary>
    /// Returns true if within one tile of coffin.
    /// </summary>
    private bool IsNearCoffin(EntityUid bloodsucker, EntityUid coffin)
    {
        if (!TryComp(bloodsucker, out TransformComponent? suckerXform) ||
            !TryComp(coffin, out TransformComponent? coffinXform))
            return false;

        if (suckerXform.MapID != coffinXform.MapID)
            return false;

        var delta = suckerXform.WorldPosition - coffinXform.WorldPosition;
        return MathF.Abs(delta.X) <= 1.5f && MathF.Abs(delta.Y) <= 1.5f;
    }

    #endregion
}
