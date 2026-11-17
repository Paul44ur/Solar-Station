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

    private static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> Burn = "Burn";

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

        _chat.TryEmoteWithChat(target, "Scream", ignoreActionBlocker: true, forceEmote: true);
        _chat.TrySendInGameICMessage(target, Loc.GetString("devil-soul-harvest-emote"), InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);

        _popup.PopupEntity(Loc.GetString("devil-soul-harvest-victim"), target, target, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("devil-soul-harvest-user", ("target", Identity.Entity(target, EntityManager))), ent, ent, PopupType.Medium);

        _mobState.ChangeMobState(target, MobState.Dead, mobState, ent);

        // Feeding on the soul mends the Archdevil.
        var heal = new DamageSpecifier(_proto.Index(Brute), -150) + new DamageSpecifier(_proto.Index(Burn), -100);
        _damageable.TryChangeDamage(ent.Owner, heal, true, false);
    }

    private void OnExamined(Entity<SoulHarvestedComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("devil-soul-harvest-examine"));
    }
}
