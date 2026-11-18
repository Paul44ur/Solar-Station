using Content.Shared.Zombies;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Actions;
using Content.Goobstation.Shared.Devil.Components;
using Content.Goobstation.Shared.Religion;
using Content.Goobstation.Shared.Supermatter.Components;
using Content.Server.Actions;
using Content.Server.Antag.Components;
using Content.Server.Atmos.Components;
using Content.Server.Zombies;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Content.Shared.Mind;
using Content.Shared.Shuttles.Components;
using System.Linq;

namespace Content.Goobstation.Server.Devil.Systems;

public sealed partial class DevilTransformSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private DevilTitheSystem _tithe = default!; // Solar
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;

    private EntityQuery<ActionsComponent> _actionsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _actionsQuery = GetEntityQuery<ActionsComponent>();

        SubscribeLocalEvent<DevilComponent, BecomeLesserDevilEvent>(OnBecomeLesser);
        SubscribeLocalEvent<DevilComponent, BecomeArchdevilEvent>(OnBecomeArch);
    }

    private void OnBecomeLesser(EntityUid uid, DevilComponent comp, BecomeLesserDevilEvent args)
    {
        Transform(uid, args.Prototype, lesser: true);
    }

    private void OnBecomeArch(EntityUid uid, DevilComponent comp, BecomeArchdevilEvent args)
    {
        Transform(uid, args.Prototype, lesser: false);
    }

    private void Transform(EntityUid uid, string prototype, bool lesser)
    {
        if (!TryComp<DevilTransformComponent>(uid, out var oldComp))
            return;

        var coords = Transform(uid).Coordinates;
        var newEntity = Spawn(prototype, coords);

        CopyDevilComponents(uid, newEntity);

        if (!EntityManager.TryCopyComponent(uid, newEntity, ref oldComp, out var newComp))
        {
            Del(uid);
            return;
        }

        // Transfer soul/power state explicitly because DevilComponent values
        if (TryComp<DevilComponent>(uid, out var oldDevil) &&
            TryComp<DevilComponent>(newEntity, out var newDevil))
        {
            newDevil.Souls = oldDevil.Souls;
            newDevil.SoulSources = new(oldDevil.SoulSources); // Solar
            newDevil.SoulsWhileLesser = oldDevil.SoulsWhileLesser; // Solar
            newDevil.PowerLevel = oldDevil.PowerLevel;
        }

        // Solar: keep what the devil gained from tithes.
        if (TryComp<DevilTitheBonusComponent>(uid, out var oldBonus))
            _tithe.ReapplyBonus(newEntity, oldBonus);

        // Clear the old entity's action bar entries before transferring,
        if (_actionsQuery.TryComp(uid, out var oldActionsComp))
        {
            foreach (var actionId in oldActionsComp.Actions.ToArray())
            {
                _actions.RemoveAction((uid, oldActionsComp), actionId);
            }
        }

        if (TryComp<ActionsContainerComponent>(uid, out _) &&
            TryComp<ActionsContainerComponent>(newEntity, out _))
        {
            _actionContainer.TransferAllActionsWithNewAttached(uid, newEntity, newEntity);
        }

        // Transfer mind last, after the new entity is fully set up.
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, newEntity, mind: mind);

        EnsureComp<CombatModeComponent>(newEntity);

        if (lesser)
        {
            EnsureComp<DevilLesserFormComponent>(newEntity);

            // Lesser devils don't get jaunt or archfire.
            _actions.RemoveAction(newEntity, newComp.JauntActionEntity);
            _actions.RemoveAction(newEntity, newComp.ArchFireActionEntity);

            // Solar: lesser form gets Voice of Hell.
            _actions.AddAction(newEntity, "ActionDevilVoiceOfHell");
            _actions.AddAction(newEntity, "ActionDevilSummonImps");
        }
        else
        {
            RemComp<DevilLesserFormComponent>(newEntity);
            EnsureComp<ArchdevilComponent>(newEntity);

            var ev = new DevilAscendedEvent();
            RaiseLocalEvent(newEntity, new DevilAscendedEvent());

            // Archdevils lose hellfire, gain archfire.
            _actions.RemoveAction(newEntity, newComp.HellfireActionEntity);

            if (newComp.ArchFireActionEntity == null)
                _actions.AddAction(newEntity, ref newComp.ArchFireActionEntity, newComp.ArchFireAction);

            // Solar: infernal aura and its toggle.
            EnsureComp<InfernalAuraComponent>(newEntity);
            _actions.AddAction(newEntity, "ActionDevilHellsSevenRings");
            _actions.AddAction(newEntity, "ActionDevilSoulHarvest");
            _actions.AddAction(newEntity, "ActionDevilMassContract");
        }

        Del(uid);
    }

    private void CopyDevilComponents(EntityUid oldUid, EntityUid newUid) // This is really dirty but it works.
    {
        // Abilities
        CopyIfExists<DevilAuthorityComponent>(oldUid, newUid);
        CopyIfExists<DevilGripComponent>(oldUid, newUid);
        CopyIfExists<DevilHeresyComponent>(oldUid, newUid);
        CopyIfExists<DevilSummonPitchforkComponent>(oldUid, newUid);
        CopyIfExists<HellstepActionComponent>(oldUid, newUid);
        // Grip effects
        CopyIfExists<GripSidegradeRotComponent>(oldUid, newUid);
        CopyIfExists<GripSidegradeStunComponent>(oldUid, newUid);
        // Traits
        CopyIfExists<NoLimbForYouComponent>(oldUid, newUid);
        CopyIfExists<PreventBucklingComponent>(oldUid, newUid);
        CopyIfExists<UncontainableComponent>(oldUid, newUid);
        // Original traits
        CopyIfExists<ZombieImmuneComponent>(oldUid, newUid);
        CopyIfExists<BreathingImmunityComponent>(oldUid, newUid);
        CopyIfExists<PressureImmunityComponent>(oldUid, newUid);
        CopyIfExists<WeakToHolyComponent>(oldUid, newUid);
        CopyIfExists<AntagImmuneComponent>(oldUid, newUid);
        CopyIfExists<SupermatterImmuneComponent>(oldUid, newUid);
        CopyIfExists<FTLSmashImmuneComponent>(oldUid, newUid);
    }

    private void CopyIfExists<T>(EntityUid oldUid, EntityUid newUid) where T : IComponent, new()
    {
        if (TryComp<T>(oldUid, out var comp))
            CopyComp(oldUid, newUid, comp);
    }
}
