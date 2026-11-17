using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Actions;
using Content.Goobstation.Shared.Bloodsuckers.Components.Vassals;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Goobstation.Shared.Bloodsuckers.Systems;
using Content.Server.Antag;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Bloodsuckers.Systems;

/// <summary>
/// The Masquerade path perks: Beguile and Forget.
/// </summary>
public sealed partial class BloodsuckerHumanityPerksSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BloodsuckerHumanitySystem _humanity = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerComponent, BloodsuckerForgetEvent>(OnForget);
        SubscribeLocalEvent<BloodsuckerComponent, BloodsuckerBeguileEvent>(OnBeguile);
        SubscribeLocalEvent<BloodsuckerBeguileComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnForget(Entity<BloodsuckerComponent> ent, ref BloodsuckerForgetEvent args)
    {
        _antag.SendBriefing(args.Target, Loc.GetString("bloodsucker-forget-victim"), Color.MediumPurple, null);
    }

    private void OnBeguile(Entity<BloodsuckerComponent> ent, ref BloodsuckerBeguileEvent args)
    {
        if (!TryComp<BloodsuckerBeguileComponent>(ent, out var beguile))
            return;

        var target = args.Target;
        if (!_humanity.IsHighHumanity(ent))
        {
            _popup.PopupEntity(Loc.GetString("bloodsucker-beguile-low-humanity"), ent, ent, PopupType.SmallCaution);
            return;
        }

        if (!CanBeBeguiled(target, out var reason))
        {
            _popup.PopupEntity(Loc.GetString(reason, ("target", target)), ent, ent, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;
        beguile.Offers[target] = _timing.CurTime + beguile.OfferDuration;

        _popup.PopupEntity(Loc.GetString("bloodsucker-beguile-offered", ("target", target)), ent, ent);
        _antag.SendBriefing(target, Loc.GetString("bloodsucker-beguile-offer", ("user", ent.Owner)), Color.DarkRed, null);
    }

    private bool CanBeBeguiled(EntityUid target, out string reason)
    {
        reason = "bloodsucker-beguile-fail";
        if (!_mind.TryGetMind(target, out _, out _) || !_mobState.IsAlive(target))
            return false;

        if (HasComp<BloodsuckerComponent>(target) || HasComp<BloodsuckerVassalComponent>(target))
            return false;

        if (HasComp<BibleUserComponent>(target) || HasComp<MindShieldComponent>(target))
        {
            reason = "bloodsucker-beguile-fail-protected";
            return false;
        }

        return true;
    }

    private void OnGetVerbs(Entity<BloodsuckerBeguileComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (!args.CanInteract
            || !ent.Comp.Offers.TryGetValue(user, out var expires)
            || expires < _timing.CurTime)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("bloodsucker-beguile-accept-verb"),
            Act = () => Accept(ent, user),
            Priority = 2,
        });
    }

    private void Accept(Entity<BloodsuckerBeguileComponent> ent, EntityUid user)
    {
        if (!ent.Comp.Offers.Remove(user, out var expires)
            || expires < _timing.CurTime
            || !TryComp<BloodsuckerComponent>(ent, out var bloodsucker)
            || !CanBeBeguiled(user, out _))
            return;

        AddComp(user, new BloodsuckerVassalComponent { Master = ent.Owner });
        bloodsucker.VassalsMade++;

        _popup.PopupEntity(Loc.GetString("bloodsucker-beguile-accepted", ("target", user)), ent, ent, PopupType.Medium);
    }
}
