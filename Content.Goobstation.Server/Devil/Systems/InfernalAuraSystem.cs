// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Actions;
using Content.Goobstation.Shared.Devil.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Devil.Systems;

/// <summary>
/// Solar: burns everything near an Archdevil while its aura is on, and lets it switch the aura on and off.
/// Also makes the Archdevil take more holy damage, so the finale stays winnable.
/// </summary>
public sealed partial class InfernalAuraSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private readonly HashSet<Entity<MobStateComponent>> _nearby = new();

    /// <summary>
    /// How much more holy damage an Archdevil takes.
    /// </summary>
    public const float ArchdevilHolyMultiplier = 2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfernalAuraComponent, DevilToggleInfernalAuraEvent>(OnToggle);
        SubscribeLocalEvent<ArchdevilComponent, DamageModifyEvent>(OnArchdevilDamageModify);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<InfernalAuraComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var aura, out var xform))
        {
            if (!aura.Enabled || now < aura.NextTick)
                continue;

            aura.NextTick = now + aura.Delay;

            _nearby.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, aura.Range, _nearby);
            foreach (var mob in _nearby)
            {
                if (mob.Owner == uid
                    || mob.Comp.CurrentState == MobState.Dead
                    || HasComp<DevilComponent>(mob)
                    || HasComp<DevilMinionComponent>(mob))
                    continue;

                _damageable.TryChangeDamage(mob.Owner, aura.Damage, origin: uid);
            }
        }
    }

    private void OnToggle(Entity<InfernalAuraComponent> ent, ref DevilToggleInfernalAuraEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        args.Handled = true;

        var msg = ent.Comp.Enabled ? "devil-infernal-aura-on" : "devil-infernal-aura-off";
        _popup.PopupEntity(Loc.GetString(msg), ent, ent, PopupType.Medium);
    }

    private void OnArchdevilDamageModify(Entity<ArchdevilComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.DamageDict.TryGetValue("Holy", out var holy) || holy <= 0)
            return;

        // Work on a copy so the weapon's own damage values are never changed.
        args.Damage = new DamageSpecifier(args.Damage);
        args.Damage.DamageDict["Holy"] = holy * ArchdevilHolyMultiplier;
    }
}
