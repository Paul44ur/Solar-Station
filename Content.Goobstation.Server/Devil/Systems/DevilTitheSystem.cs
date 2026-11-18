// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;

namespace Content.Goobstation.Server.Devil.Systems;

/// <summary>
/// Solar: devil-side contract clauses. The mortal pays with life force, speed or might, and the devil gains it.
/// Everything is returned when the devil dies.
/// </summary>
public sealed partial class DevilTitheSystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <summary>The fastest and strongest a devil can become from tithes.</summary>
    private const float MaxDevilMultiplier = 1.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevilContractLifeForceEvent>(OnLifeForce);
        SubscribeLocalEvent<DevilContractSwiftnessEvent>(OnSwiftness);
        SubscribeLocalEvent<DevilContractMightEvent>(OnMight);
    }

    private void OnLifeForce(DevilContractLifeForceEvent args)
    {
        if (!TryGetDevil(args, out var devil, out var tithed))
            return;

        tithed.HealthLost += args.Amount;
        Dirty(args.Target, tithed);
        AdjustThresholds(args.Target, -args.Amount);

        var bonus = EnsureComp<DevilTitheBonusComponent>(devil);
        bonus.HealthGained += args.Amount;
        Dirty(devil, bonus);
        AdjustThresholds(devil, args.Amount);
    }

    private void OnSwiftness(DevilContractSwiftnessEvent args)
    {
        if (!TryGetDevil(args, out var devil, out var tithed))
            return;

        tithed.SpeedMultiplier *= args.TargetMultiplier;
        Dirty(args.Target, tithed);
        _speed.RefreshMovementSpeedModifiers(args.Target);

        var bonus = EnsureComp<DevilTitheBonusComponent>(devil);
        bonus.SpeedMultiplier = MathF.Min(bonus.SpeedMultiplier + args.DevilBonus, MaxDevilMultiplier);
        Dirty(devil, bonus);
        _speed.RefreshMovementSpeedModifiers(devil);
    }

    private void OnMight(DevilContractMightEvent args)
    {
        if (!TryGetDevil(args, out var devil, out var tithed))
            return;

        tithed.MeleeMultiplier *= args.TargetMultiplier;
        Dirty(args.Target, tithed);

        var bonus = EnsureComp<DevilTitheBonusComponent>(devil);
        bonus.MeleeMultiplier = MathF.Min(bonus.MeleeMultiplier + args.DevilBonus, MaxDevilMultiplier);
        Dirty(devil, bonus);
    }

    private bool TryGetDevil(BaseDevilContractEvent args, out EntityUid devil, out DevilTithedComponent tithed)
    {
        devil = default;
        tithed = default!;

        if (args.Contract?.ContractOwner is not { } owner || TerminatingOrDeleted(owner))
            return false;

        devil = owner;
        tithed = EnsureComp<DevilTithedComponent>(args.Target);
        if (_mind.TryGetMind(owner, out var mindId, out _))
            tithed.DevilMind = mindId;

        return true;
    }

    /// <summary>
    /// Re-applies a devil's gained health after it changes form, since the new body has its own thresholds.
    /// </summary>
    public void ReapplyBonus(EntityUid newDevil, DevilTitheBonusComponent oldBonus)
    {
        var bonus = EnsureComp<DevilTitheBonusComponent>(newDevil);
        bonus.HealthGained = oldBonus.HealthGained;
        bonus.SpeedMultiplier = oldBonus.SpeedMultiplier;
        bonus.MeleeMultiplier = oldBonus.MeleeMultiplier;
        Dirty(newDevil, bonus);

        AdjustThresholds(newDevil, bonus.HealthGained);
        _speed.RefreshMovementSpeedModifiers(newDevil);
    }

    /// <summary>
    /// Solar: a lawyer voided a tithe clause, so the mortal gets that part back.
    /// </summary>
    public void VoidTithe(EntityUid target, BaseDevilContractEvent clauseEvent)
    {
        if (!TryComp<DevilTithedComponent>(target, out var tithed))
            return;

        switch (clauseEvent)
        {
            case DevilContractLifeForceEvent lifeForce:
                var amount = FixedPoint2.Min(lifeForce.Amount, tithed.HealthLost);
                tithed.HealthLost -= amount;
                AdjustThresholds(target, amount);
                break;
            case DevilContractSwiftnessEvent swiftness when swiftness.TargetMultiplier > 0:
                tithed.SpeedMultiplier = MathF.Min(tithed.SpeedMultiplier / swiftness.TargetMultiplier, 1f);
                _speed.RefreshMovementSpeedModifiers(target);
                break;
            case DevilContractMightEvent might when might.TargetMultiplier > 0:
                tithed.MeleeMultiplier = MathF.Min(tithed.MeleeMultiplier / might.TargetMultiplier, 1f);
                break;
        }

        Dirty(target, tithed);
    }

    /// <summary>
    /// When the devil is banished for good, everything it took is given back.
    /// </summary>
    public void ReleaseTithes(EntityUid ent)
    {
        if (!_mind.TryGetMind(ent, out var devilMind, out _))
            return;

        var query = EntityQueryEnumerator<DevilTithedComponent>();
        while (query.MoveNext(out var uid, out var tithed))
        {
            if (tithed.DevilMind != devilMind)
                continue;

            AdjustThresholds(uid, tithed.HealthLost);
            RemComp<DevilTithedComponent>(uid);
            _speed.RefreshMovementSpeedModifiers(uid);
            _popup.PopupEntity(Loc.GetString("devil-tithe-returned"), uid, uid, PopupType.Medium);
        }

        // The devil loses what it took as well.
        if (TryComp<DevilTitheBonusComponent>(ent, out var bonus))
        {
            AdjustThresholds(ent, -bonus.HealthGained);
            RemComp<DevilTitheBonusComponent>(ent);
            _speed.RefreshMovementSpeedModifiers(ent);
        }
    }

    private void AdjustThresholds(EntityUid uid, FixedPoint2 delta)
    {
        foreach (var state in new[] { MobState.Critical, MobState.Dead })
        {
            if (!_thresholds.TryGetThresholdForState(uid, state, out var threshold))
                continue;

            _thresholds.SetMobStateThreshold(uid, FixedPoint2.Max(threshold.Value + delta, 1), state);
        }
    }
}
