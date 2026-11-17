using System.Text;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Vassals;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameStates;

namespace Content.Goobstation.Server.Bloodsuckers.GameTicking.Rules;

public sealed partial class BloodsuckerRuleSystem : GameRuleSystem<BloodsuckerRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private PvsOverrideSystem _pvs = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        SubscribeLocalEvent<BloodsuckerRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<BloodsuckerRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
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

    private void OnTextPrepend(Entity<BloodsuckerRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();
        var query = EntityQueryEnumerator<BloodsuckerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_mind.TryGetMind(uid, out var mindId, out var mind))
                continue;

            var vassals = 0;
            var vassalQuery = EntityQueryEnumerator<BloodsuckerVassalComponent>();
            while (vassalQuery.MoveNext(out _, out var vassal))
            {
                if (vassal.Master == uid)
                    vassals++;
            }

            sb.AppendLine(Loc.GetString("bloodsucker-roundend-stats",
                ("name", _objectives.GetTitle((mindId, mind), Name(uid))),
                ("rank", comp.Rank),
                ("blood", (int) comp.BloodDrunk),
                ("made", comp.VassalsMade),
                ("vassals", vassals)));
        }

        args.Text += sb.ToString();
    }
}
