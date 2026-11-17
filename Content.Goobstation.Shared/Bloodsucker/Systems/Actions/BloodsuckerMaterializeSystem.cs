using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Actions;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Goobstation.Shared.Bloodsuckers.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Server.Bloodsuckers.Systems;

public sealed partial class BloodsuckerMaterializeSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private BloodsuckerHumanitySystem _humanity = default!;
    [Dependency] private BloodsuckerClaimSystem _claim = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodsuckerComponent, BloodsuckerMaterializeEvent>(OnMaterialize);
    }

    private void OnMaterialize(Entity<BloodsuckerComponent> ent, ref BloodsuckerMaterializeEvent args)
    {
        if (!TryComp(ent, out BloodsuckerMaterializeComponent? comp))
            return;

        // Must be inside lair
        if (!_claim.IsInLair(ent.Owner))
        {
            _popup.PopupPredicted(Loc.GetString("bloodsucker-materialize-no-lair"),
                ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        // Check blood cost
        if (!TryComp(ent.Owner, out BloodstreamComponent? bloodstream))
            return;

        var currentBlood = bloodstream.BloodSolution is { } sol
            ? (float) sol.Comp.Solution.Volume
            : 0f;

        if (currentBlood < comp.BloodCost)
        {
            _popup.PopupPredicted(Loc.GetString("bloodsucker-materialize-no-blood"),
                ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        // Deduct blood
        _bloodstream.TryModifyBloodLevel(
            new Entity<BloodstreamComponent?>(ent.Owner, bloodstream),
            -FixedPoint2.New(comp.BloodCost));

        // Deduct health
        var damage = new DamageSpecifier();
        damage.DamageDict["Blunt"] = comp.HealthCost;
        _damageable.TryChangeDamage(ent.Owner, damage, ignoreResistances: true);

        if (comp.HumanityCost != 0f && TryComp(ent, out BloodsuckerHumanityComponent? humanity))
            _humanity.ChangeHumanity(
                new Entity<BloodsuckerHumanityComponent>(ent.Owner, humanity),
                -comp.HumanityCost);

        // Spawn at feet
        var coords = Transform(ent.Owner).Coordinates;
        Spawn(comp.StructureProto, coords);

        _popup.PopupPredicted(Loc.GetString("bloodsucker-materialize-success"),
            ent.Owner, ent.Owner, PopupType.Large);

        args.Handled = true;
    }
}
