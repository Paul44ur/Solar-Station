// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Devil.Components;
using Content.Shared.Examine;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Goobstation.Shared.Devil.Systems;

/// <summary>
/// Solar: applies the stat changes from devil tithe clauses (life force, swiftness, might).
/// </summary>
public sealed partial class SharedDevilTitheSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevilTithedComponent, RefreshMovementSpeedModifiersEvent>(OnTithedSpeed);
        SubscribeLocalEvent<DevilTithedComponent, GetUserMeleeDamageEvent>(OnTithedMelee);
        SubscribeLocalEvent<DevilTithedComponent, ExaminedEvent>(OnTithedExamined);
        SubscribeLocalEvent<DevilTitheBonusComponent, RefreshMovementSpeedModifiersEvent>(OnBonusSpeed);
        SubscribeLocalEvent<DevilTitheBonusComponent, GetUserMeleeDamageEvent>(OnBonusMelee);
    }

    private void OnTithedSpeed(Entity<DevilTithedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }

    private void OnTithedMelee(Entity<DevilTithedComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        args.Damage *= ent.Comp.MeleeMultiplier;
    }

    private void OnTithedExamined(Entity<DevilTithedComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange && ent.Comp.HealthLost > 0)
            args.PushMarkup(Loc.GetString("devil-tithe-burns-examine"));
    }

    private void OnBonusSpeed(Entity<DevilTitheBonusComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }

    private void OnBonusMelee(Entity<DevilTitheBonusComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        args.Damage *= ent.Comp.MeleeMultiplier;
    }
}
