// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.CheatDeath;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Actions;
using Content.Goobstation.Shared.Devil.Components;
using Content.Goobstation.Shared.Devil.Condemned;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Devil.Systems;

/// <summary>
/// Solar: the Archdevil collects the souls it owns. The victim dies on the spot and can never be revived or cloned.
/// </summary>
public sealed partial class SoulHarvestSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private Contract.DevilContractSystem _contract = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    private static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> Burn = "Burn";
    private static readonly ProtoId<DamageTypePrototype> Cellular = "Cellular";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArchdevilComponent, DevilSoulHarvestEvent>(OnHarvest);
        SubscribeLocalEvent<SoulHarvestedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnHarvest(Entity<ArchdevilComponent> ent, ref DevilSoulHarvestEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (!TryComp<CondemnedComponent>(target, out var condemned)
            || condemned.SoulOwnedNotDevil
            || !OwnsSoul(ent, target)
            || !TryComp<MobStateComponent>(target, out var mobState)
            || mobState.CurrentState == MobState.Dead)
        {
            _popup.PopupEntity(Loc.GetString("devil-soul-harvest-no-soul"), ent, ent);
            return;
        }

        args.Handled = true;

        // The body stays behind as an empty shell instead of being dragged off by the usual condemnation.
        condemned.CondemnOnDeath = false;
        RemComp<CheatDeathComponent>(target);

        var unrevivable = EnsureComp<UnrevivableComponent>(target);
        unrevivable.ReasonMessage = "devil-soul-harvest-unrevivable";
        unrevivable.Cloneable = false;
        unrevivable.Analyzable = false;
        Dirty(target, unrevivable);

        EnsureComp<SoulHarvestedComponent>(target);
        _contract.MarkHarvested(target);

        _chat.TryEmoteWithChat(target, "Scream", ignoreActionBlocker: true, forceEmote: true);
        _chat.TrySendInGameICMessage(target, Loc.GetString("devil-soul-harvest-emote"), InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);

        _popup.PopupEntity(Loc.GetString("devil-soul-harvest-victim"), target, target, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("devil-soul-harvest-user", ("target", Identity.Entity(target, EntityManager))), ent, ent, PopupType.Medium);

        // Deal enough damage to actually be dead, so the next hit can't flip the body back to alive.
        if (_thresholds.TryGetThresholdForState(target, MobState.Dead, out var deadThreshold)
            && TryComp<DamageableComponent>(target, out var damageable))
        {
            var needed = deadThreshold.Value - damageable.TotalDamage + 1;
            if (needed > 0)
                _damageable.TryChangeDamage(target, new DamageSpecifier(_proto.Index(Cellular), needed), true, origin: ent);
        }

        if (mobState.CurrentState != MobState.Dead)
            _mobState.ChangeMobState(target, MobState.Dead, mobState, ent);

        // Feeding on the soul mends the Archdevil.
        var heal = new DamageSpecifier(_proto.Index(Brute), -150) + new DamageSpecifier(_proto.Index(Burn), -100);
        _damageable.TryChangeDamage(ent.Owner, heal, true, false);
    }

    /// <summary>
    /// The Archdevil can only harvest a soul it collected itself. Its old bodies are gone after changing form,
    /// so this checks the souls it remembers by mind rather than the condemned mark's owner.
    /// </summary>
    private bool OwnsSoul(EntityUid devil, EntityUid target)
    {
        if (!TryComp<DevilComponent>(devil, out var devilComp))
            return false;

        var source = _mind.TryGetMind(target, out var mindId, out _) ? mindId : target;
        return devilComp.SoulSources.Contains(source);
    }

    private void OnExamined(Entity<SoulHarvestedComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("devil-soul-harvest-examine"));
    }
}
