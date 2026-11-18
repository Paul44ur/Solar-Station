// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Devil.Contract;
using Content.Goobstation.Server.Devil.Contract.Revival;
using Content.Goobstation.Server.Devil.Grip;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Actions;
using Content.Goobstation.Shared.Devil.Components;
using Content.Goobstation.Shared.Devil.Condemned;
using Content.Goobstation.Shared.Devil.Contract;
using Content.Server.Store.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Goobstation.Common.Religion;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;

namespace Content.Goobstation.Server.Devil;

public sealed partial class DevilSystem
{
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private void SubscribeAbilities()
    {
        SubscribeLocalEvent<DevilComponent, CreateContractEvent>(OnContractCreated);
        SubscribeLocalEvent<DevilComponent, CreateRevivalContractEvent>(OnRevivalContractCreated);
        SubscribeLocalEvent<DevilComponent, ShadowJauntEvent>(OnShadowJaunt);
        SubscribeLocalEvent<DevilComponent, DevilGripEvent>(OnDevilGrip);
        SubscribeLocalEvent<DevilComponent, DevilPossessionEvent>(OnPossess);
        SubscribeLocalEvent<DevilComponent, DevilVoiceOfHellEvent>(OnVoiceOfHell);
        SubscribeLocalEvent<DevilComponent, DevilSummonImpsEvent>(OnSummonImps);
    }

    private void OnContractCreated(Entity<DevilComponent> devil, ref CreateContractEvent args)
    {
        if (!TryUseAbility(args))
            return;

        var contract = Spawn(devil.Comp.ContractPrototype, Transform(devil).Coordinates);
        _hands.TryPickupAnyHand(devil, contract);

        if (!TryComp<DevilContractComponent>(contract, out var contractComponent))
            return;

        contractComponent.ContractOwner = args.Performer;
        _contract.OpenClauseMenu(contract, args.Performer); // Solar

        PlayFwooshSound(devil);
        DoContractFlavor(devil, Identity.Name(devil, EntityManager));
    }

    private void OnRevivalContractCreated(Entity<DevilComponent> devil, ref CreateRevivalContractEvent args)
    {
        if (!TryUseAbility(args))
            return;

        var contract = Spawn(devil.Comp.RevivalContractPrototype, Transform(devil).Coordinates);
        _hands.TryPickupAnyHand(devil, contract);

        if (!TryComp<RevivalContractComponent>(contract, out var contractComponent))
            return;

        contractComponent.ContractOwner = args.Performer;

        PlayFwooshSound(devil);
        DoContractFlavor(devil, Identity.Name(devil, EntityManager));
    }

    private void OnShadowJaunt(Entity<DevilComponent> devil, ref ShadowJauntEvent args)
    {
        if (!TryUseAbility(args))
            return;

        if (HasComp<DevilLesserFormComponent>(devil) || HasComp<ArchdevilComponent>(devil)) // Unable to jaunt while in ascended form.
        {
            var message = Loc.GetString("jaunt-ascended-fail");
            _popup.PopupEntity(message, devil, devil);
            return;
        }

        Spawn(devil.Comp.JauntAnimationProto, Transform(devil).Coordinates);
        Spawn(devil.Comp.PentagramEffectProto, Transform(devil).Coordinates);

        if (TryComp<CuffableComponent>(devil, out var cuffableComponent))
            _container.EmptyContainer(cuffableComponent.Container, true);

        _poly.PolymorphEntity(devil, devil.Comp.JauntEntityProto);
    }

    private void OnDevilGrip(Entity<DevilComponent> devil, ref DevilGripEvent args)
    {
        if (!TryUseAbility(args))
            return;

        if (devil.Comp.DevilGrip != null)
        {
            foreach (var item in _hands.EnumerateHeld(devil.Owner))
            {
                if (HasComp<DevilGripComponent>(item))
                    QueueDel(item);
            }
        }

        var grasp = Spawn(devil.Comp.GripPrototype, Transform(devil).Coordinates);

        if (!_hands.TryPickupAnyHand(devil, grasp))
            QueueDel(grasp);

        devil.Comp.DevilGrip = args.Action.Owner;
    }


    // Solar: lesser form calls imps up from below. Ghosts can take them over.
    private void OnSummonImps(Entity<DevilComponent> devil, ref DevilSummonImpsEvent args)
    {
        if (!TryUseAbility(args))
            return;

        var coords = Transform(devil).Coordinates;
        for (var i = 0; i < args.Amount; i++)
        {
            Spawn(devil.Comp.PentagramEffectProto, coords);
            Spawn(args.ImpPrototype, coords);
        }

        PlayFwooshSound(devil);
        _popup.PopupEntity(Loc.GetString("devil-summon-imps"), devil, PopupType.MediumCaution);
    }

    // Solar: Voice of Hell. Chaplains are protected by their faith.
    private void OnVoiceOfHell(Entity<DevilComponent> devil, ref DevilVoiceOfHellEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (HasComp<BibleUserComponent>(target) || HasComp<DevilComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("devil-voice-of-hell-resisted", ("target", Identity.Entity(target, EntityManager))), devil, devil);
            args.Handled = true;
            return;
        }

        if (!_statusEffects.TryAddStatusEffect<MutedComponent>(target, "Muted", args.Duration, true))
        {
            _popup.PopupEntity(Loc.GetString("devil-voice-of-hell-failed"), devil, devil);
            return;
        }

        args.Handled = true;
        PlayFwooshSound(devil);
        _popup.PopupEntity(Loc.GetString("devil-voice-of-hell-target"), target, target, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("devil-voice-of-hell-user", ("target", Identity.Entity(target, EntityManager))), devil, devil);
    }

    private void OnPossess(Entity<DevilComponent> devil, ref DevilPossessionEvent args)
    {
        if (!TryComp<CondemnedComponent>(args.Target, out var condemned) || condemned.SoulOwnedNotDevil)
        {
            var message = Loc.GetString("invalid-possession-target");
            _popup.PopupEntity(message, devil, devil);
            return;
        }

        if (!TryUseAbility(args))
            return;

        // Solar: scale a copy, so the stored base duration doesn't keep multiplying on every cast.
        var duration = devil.Comp.PossessionDuration;
        if (devil.Comp.PowerLevel != DevilPowerLevel.None)
            duration *= (int)devil.Comp.PowerLevel;

        // Only mark as handled if possession succeeds to avoid wasting the cooldown.
        if (!_possession.TryPossessTarget(args.Target, args.Performer, duration, true, polymorphPossessor: true))
            return;

        Spawn(devil.Comp.JauntAnimationProto, Transform(args.Target).Coordinates);
        Spawn(devil.Comp.PentagramEffectProto, Transform(args.Target).Coordinates);

        args.Handled = true;
    }
}
