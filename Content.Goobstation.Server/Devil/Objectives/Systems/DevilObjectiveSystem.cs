// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Devil.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Goobstation.Server.Devil.Objectives.Systems;

public sealed partial class DevilObjectiveSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignContractConditionComponent, ObjectiveGetProgressEvent>(OnContractGetProgress);
        SubscribeLocalEvent<MeetContractWeightConditionComponent, ObjectiveGetProgressEvent>(OnWeightGetProgress);
        SubscribeLocalEvent<DevilDepartmentContractsConditionComponent, ObjectiveGetProgressEvent>(OnDepartmentGetProgress);
    }

    private void OnContractGetProgress(EntityUid uid, SignContractConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        args.Progress = target != 0 ? MathF.Min((float)comp.ContractsSigned / target, 1f) : 1f;
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
