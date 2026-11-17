// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Actions;
using Content.Goobstation.Shared.Devil.Components;
using Content.Goobstation.Shared.Devil.Condemned;
using Content.Goobstation.Shared.Devil.Contract;
using Content.Goobstation.Shared.Devil.UI;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Devil.Contract;

/// <summary>
/// Solar: the Archdevil's mass contract. One clause menu, then a contract in the hands of everyone nearby,
/// with an Accept/Decline window. Declining burns; waiting it out lets the contract crumble harmlessly.
/// </summary>
public sealed partial class DevilContractSystem
{
    [Dependency] private EntityLookupSystem _lookup = null!;
    [Dependency] private IGameTiming _timing = null!;

    private static readonly EntProtoId MassContractProto = "PaperDevilContract";
    private const float MassContractRange = 7f;
    private static readonly TimeSpan MassContractOfferTime = TimeSpan.FromSeconds(35);
    private const string DefaultDamnedPrice = "lifeforce";

    /// <summary>
    /// Prices the devil can take from mortals whose soul it already owns.
    /// </summary>
    public static readonly string[] DamnedPrices = ["lifeforce", "swiftness", "might", "ahand", "aleg", "anorgan"];

    private readonly HashSet<Entity<MobStateComponent>> _massTargets = new();

    private void InitializeMassContract()
    {
        SubscribeLocalEvent<ArchdevilComponent, DevilMassContractEvent>(OnMassContract);
        SubscribeLocalEvent<DevilMassContractOfferComponent, DevilMassContractOfferMessage>(OnOfferAnswered);
    }

    private void OnMassContract(Entity<ArchdevilComponent> ent, ref DevilMassContractEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var template = Spawn(MassContractProto, Transform(ent).Coordinates);
        EnsureComp<DevilMassContractTemplateComponent>(template);
        if (TryComp<DevilContractComponent>(template, out var contract))
            contract.ContractOwner = ent;

        _hands.TryPickupAnyHand(ent, template);
        _ui.OpenUi(template, DevilContractUiKey.Key, ent);
        _ui.SetUiState(template, DevilContractUiKey.Key, new DevilContractMenuState(true));
    }

    private void DistributeMassContract(EntityUid template, EntityUid devil, List<string> chosen, string? damnedPrice)
    {
        QueueDel(template);

        if (damnedPrice == null || !DamnedPrices.Contains(damnedPrice))
            damnedPrice = DefaultDamnedPrice;

        var offered = 0;
        _massTargets.Clear();
        _lookup.GetEntitiesInRange(Transform(devil).Coordinates, MassContractRange, _massTargets);
        foreach (var target in _massTargets)
        {
            if (!IsMassContractTarget(target, devil))
                continue;

            // Someone whose soul already belongs to a devil pays a different price instead.
            var clauses = chosen;
            if (TryComp<CondemnedComponent>(target, out var condemned) && !condemned.SoulOwnedNotDevil)
            {
                clauses = chosen.Select(id => id == "soulownership" ? damnedPrice : id).Distinct().ToList();
            }

            var offer = Spawn(MassContractProto, Transform(target).Coordinates);
            if (!TryComp<DevilContractComponent>(offer, out var offerContract)
                || !TryComp<PaperComponent>(offer, out var paper))
            {
                QueueDel(offer);
                continue;
            }

            offerContract.ContractOwner = devil;
            WriteClauses((offer, offerContract, paper), clauses);

            var pending = EnsureComp<DevilMassContractOfferComponent>(offer);
            pending.Target = target;
            pending.Expires = _timing.CurTime + MassContractOfferTime;

            _hands.TryPickupAnyHand(target, offer);
            _ui.OpenUi(offer, DevilMassContractOfferUiKey.Key, target);
            _popupSystem.PopupEntity(Loc.GetString("devil-mass-contract-received"), target, target, PopupType.LargeCaution);
            offered++;
        }

        _popupSystem.PopupEntity(Loc.GetString("devil-mass-contract-sent", ("count", offered)), devil, devil, PopupType.Medium);
    }

    private bool IsMassContractTarget(Entity<MobStateComponent> target, EntityUid devil)
    {
        return target.Owner != devil
               && target.Comp.CurrentState == MobState.Alive
               && HasComp<HumanoidAppearanceComponent>(target)
               && TryComp<MindContainerComponent>(target, out var mind) && mind.HasMind
               && !HasComp<DevilComponent>(target)
               && !HasComp<DevilMinionComponent>(target)
               && !HasComp<SoulHarvestedComponent>(target);
    }

    private void OnOfferAnswered(Entity<DevilMassContractOfferComponent> ent, ref DevilMassContractOfferMessage args)
    {
        var target = ent.Comp.Target;
        if (args.Actor != target
            || !TryComp<DevilContractComponent>(ent, out var contract)
            || contract.ContractOwner is not { } devil)
            return;

        _ui.CloseUi(ent.Owner, DevilMassContractOfferUiKey.Key);
        RemComp<DevilMassContractOfferComponent>(ent);

        if (!args.Accepted)
        {
            DeclineMassContract(ent, target, devil);
            return;
        }

        // A chaplain who signs gives up their faith, and with it their holy powers.
        if (HasComp<BibleUserComponent>(target))
        {
            RemComp<BibleUserComponent>(target);
            RemComp<Content.Goobstation.Shared.Enchanting.Components.CanEnchantComponent>(target);
            if (TryComp<CondemnedComponent>(target, out var pledged) && pledged.SoulOwnedNotDevil)
                RemComp(target, pledged);

            _popupSystem.PopupEntity(Loc.GetString("devil-mass-contract-chaplain-fallen"), target, target, PopupType.LargeCaution);
        }

        var contractEnt = (ent.Owner, contract);
        HandleVictimSign(contractEnt, ent, target);
        HandleDevilSign(contractEnt, ent, devil);
        HandleBothPartiesSigned(contractEnt);
    }

    private void DeclineMassContract(EntityUid contract, EntityUid target, EntityUid devil)
    {
        var coords = Transform(contract).Coordinates;
        if (TryComp<DevilComponent>(devil, out var devilComp))
        {
            Spawn(devilComp.FireEffectProto, coords);
            _audio.PlayPvs(devilComp.FwooshPath, coords, AudioParams.Default.WithVolume(-2f));
        }

        QueueDel(contract);

        // A chaplain's faith shields them from the flames.
        if (HasComp<BibleUserComponent>(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("devil-mass-contract-declined-chaplain"), target, target, PopupType.Medium);
            return;
        }

        var burn = new DamageSpecifier(_prototypeManager.Index<Content.Shared.Damage.Prototypes.DamageTypePrototype>("Heat"), 15);
        _damageable.TryChangeDamage(target, burn, true);
        _popupSystem.PopupEntity(Loc.GetString("devil-mass-contract-declined", ("target", Identity.Entity(target, EntityManager))), target, PopupType.MediumCaution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DevilMassContractOfferComponent>();
        while (query.MoveNext(out var uid, out var offer))
        {
            if (now < offer.Expires)
                continue;

            _ui.CloseUi(uid, DevilMassContractOfferUiKey.Key);
            _popupSystem.PopupEntity(Loc.GetString("devil-mass-contract-crumbles"), offer.Target, offer.Target, PopupType.Medium);
            QueueDel(uid);
        }
    }
}

/// <summary>
/// Solar: the contract the Archdevil fills in before copies are handed out.
/// </summary>
[RegisterComponent]
public sealed partial class DevilMassContractTemplateComponent : Component;

/// <summary>
/// Solar: a mass contract waiting for its mortal's answer.
/// </summary>
[RegisterComponent]
public sealed partial class DevilMassContractOfferComponent : Component
{
    [DataField]
    public EntityUid Target;

    [DataField]
    public TimeSpan Expires;
}
