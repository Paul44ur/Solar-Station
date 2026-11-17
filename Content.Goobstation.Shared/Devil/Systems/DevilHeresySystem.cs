using Content.Goobstation.Shared.Devil.Actions;
using Content.Goobstation.Shared.Devil.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;

namespace Content.Goobstation.Shared.Devil.Systems;

/// <summary>
/// This system just replicates what Mansus grasp does when drawing a rune, except it is independent from the entity.
/// </summary>
public sealed partial class DevilHeresySystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DevilHeresyComponent, DevilHeresyEvent>(OnHeresyInstant);
        SubscribeLocalEvent<DevilHeresyComponent, DevilHeresyDoAfterEvent>(OnDrawComplete);
    }

    private void OnHeresyInstant(EntityUid uid, DevilHeresyComponent comp, DevilHeresyEvent args)
    {
        var coords = _transform.GetMapCoordinates(args.Performer);
        var animation = Spawn(comp.AnimationPrototype, coords);
        _transform.AttachToGridOrMap(animation);

        var doAfter = new DoAfterArgs(EntityManager, args.Performer, comp.DrawTime, new DevilHeresyDoAfterEvent(GetNetEntity(animation)), uid)
        {
            BreakOnMove = false,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnDrawComplete(EntityUid uid, DevilHeresyComponent comp, DevilHeresyDoAfterEvent args)
    {
        var animationEntity = GetEntity(args.AnimationEntity);

        if (args.Cancelled)
        {
            QueueDel(animationEntity);
            return;
        }

        var coords = _transform.GetMapCoordinates(animationEntity);
        var rune = Spawn(comp.RunePrototype, coords);
        _transform.AttachToGridOrMap(rune);

        QueueDel(animationEntity);
    }
}
