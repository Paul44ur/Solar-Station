// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Devil.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Devil.Objectives.Systems;

public sealed partial class DevilObjectiveSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignContractConditionComponent, ObjectiveGetProgressEvent>(OnContractGetProgress);
        SubscribeLocalEvent<MeetContractWeightConditionComponent, ObjectiveGetProgressEvent>(OnWeightGetProgress);
        SubscribeLocalEvent<DevilDepartmentContractsConditionComponent, ObjectiveGetProgressEvent>(OnDepartmentGetProgress);
        SubscribeLocalEvent<DevilSoulPercentObjectiveComponent, ObjectiveAssignedEvent>(OnSoulPercentAssigned);
        SubscribeLocalEvent<DevilSoulPercentObjectiveComponent, ObjectiveAfterAssignEvent>(OnSoulPercentAfterAssign);
        SubscribeLocalEvent<DevilSoulPercentObjectiveComponent, ObjectiveGetProgressEvent>(OnSoulPercentGetProgress);
    }

    private void OnContractGetProgress(EntityUid uid, SignContractConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        args.Progress = target != 0 ? MathF.Min((float)comp.ContractsSigned / target, 1f) : 1f;
    }

    // Solar: the soul goal scales with how many people are playing.
    private void OnSoulPercentAssigned(Entity<DevilSoulPercentObjectiveComponent> ent, ref ObjectiveAssignedEvent args)
    {
        var fraction = _random.NextFloat(ent.Comp.MinFraction, ent.Comp.MaxFraction);
        ent.Comp.Target = Math.Max(ent.Comp.Minimum, (int) MathF.Round(_players.PlayerCount * fraction));
    }

    private void OnSoulPercentAfterAssign(Entity<DevilSoulPercentObjectiveComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("count", ent.Comp.Target)), args.Meta);
        _metaData.SetEntityDescription(ent, Loc.GetString(ent.Comp.Description, ("count", ent.Comp.Target)), args.Meta);
    }

    private void OnSoulPercentGetProgress(Entity<DevilSoulPercentObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = ent.Comp.Target != 0 ? MathF.Min((float) ent.Comp.Souls / ent.Comp.Target, 1f) : 1f;
    }

    // Solar
    private void OnDepartmentGetProgress(EntityUid uid, DevilDepartmentContractsConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (comp.Departments.Count == 0)
        {
            args.Progress = 1f;
            return;
        }

        var done = 0;
        foreach (var department in comp.Departments)
        {
            if (comp.Signed.Contains(department))
                done++;
        }

        args.Progress = (float) done / comp.Departments.Count;
    }

    private void OnWeightGetProgress(EntityUid uid, MeetContractWeightConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        args.Progress = target != 0 ? MathF.Min((float)comp.ContractWeight / target, 1f) : 1f;
    }
}
