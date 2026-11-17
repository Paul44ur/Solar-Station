using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameStates;

namespace Content.Goobstation.Server.Bloodsuckers.GameTicking.Rules;

public sealed partial class BloodsuckerRuleSystem : GameRuleSystem<BloodsuckerRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private PvsOverrideSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        SubscribeLocalEvent<BloodsuckerRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    protected override void Started(EntityUid uid, BloodsuckerRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // Sol only happens while a day/night manager exists, and clients need it for the Sol timer.
        if (!EntityQueryEnumerator<BloodsuckerDayNightComponent>().MoveNext(out _))
            _pvs.AddGlobalOverride(Spawn(component.DayNightManager));
    }

    private void OnSelectAntag(Entity<BloodsuckerRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (HasComp<SiliconComponent>(args.EntityUid))
            return;

        EnsureComp<BloodsuckerComponent>(args.EntityUid);
        _antag.SendBriefing(args.EntityUid, Loc.GetString("bloodsucker-role-greeting"), Color.DarkRed, ent.Comp.BriefingSound);
    }

    private void OnGetBriefing(Entity<BloodsuckerRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("bloodsucker-role-briefing"));
    }
}
