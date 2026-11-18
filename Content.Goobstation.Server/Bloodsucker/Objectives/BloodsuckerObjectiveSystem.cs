using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Goobstation.Server.Bloodsuckers.Objectives;

public sealed partial class BloodsuckerObjectiveSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerDrinkBloodConditionComponent, ObjectiveGetProgressEvent>(OnDrinkBloodProgress);
        SubscribeLocalEvent<BloodsuckerRankConditionComponent, ObjectiveGetProgressEvent>(OnRankProgress);
        SubscribeLocalEvent<BloodsuckerVassalConditionComponent, ObjectiveGetProgressEvent>(OnVassalProgress);
    }

    private void OnDrinkBloodProgress(EntityUid uid, BloodsuckerDrinkBloodConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(uid, args.Mind, b => b.BloodDrunk);
    }

    private void OnRankProgress(EntityUid uid, BloodsuckerRankConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(uid, args.Mind, b => b.Rank);
    }

    private void OnVassalProgress(EntityUid uid, BloodsuckerVassalConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(uid, args.Mind, b => b.VassalsMade);
    }

    private float GetProgress(EntityUid objective, MindComponent mind, Func<BloodsuckerComponent, float> getValue)
    {
        if (!TryComp<BloodsuckerComponent>(mind.OwnedEntity, out var bloodsucker))
            return 0f;

        var target = _number.GetTarget(objective);
        return target > 0 ? MathF.Min(getValue(bloodsucker) / target, 1f) : 1f;
    }
}
