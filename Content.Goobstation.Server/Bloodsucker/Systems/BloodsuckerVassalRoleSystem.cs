using Content.Goobstation.Shared.Bloodsuckers.Components.Vassals;
using Content.Server.Antag;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Bloodsuckers.Systems;

/// <summary>
/// Gives vassals their antag mind role and briefing, and takes it away when they stop being a vassal.
/// </summary>
public sealed partial class BloodsuckerVassalRoleSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    private static readonly EntProtoId VassalRole = "MindRoleBloodsuckerVassal";
    private static readonly SoundSpecifier BriefingSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerVassalComponent, ComponentStartup>(OnVassalStartup);
        SubscribeLocalEvent<BloodsuckerVassalComponent, ComponentShutdown>(OnVassalShutdown);
        SubscribeLocalEvent<BloodsuckerVassalComponent, MindAddedMessage>(OnVassalMindAdded);
        SubscribeLocalEvent<BloodsuckerVassalRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnVassalStartup(Entity<BloodsuckerVassalComponent> ent, ref ComponentStartup args)
    {
        TryAddVassalRole(ent);
    }

    // A vassal made from a body with no player gets the role once someone takes it over.
    private void OnVassalMindAdded(Entity<BloodsuckerVassalComponent> ent, ref MindAddedMessage args)
    {
        TryAddVassalRole(ent);
    }

    private void TryAddVassalRole(EntityUid ent)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mind)
            || _role.MindHasRole<BloodsuckerVassalRoleComponent>(mindId))
            return;

        _role.MindAddRole(mindId, VassalRole, mind, silent: true);
        _antag.SendBriefing(ent, Loc.GetString("bloodsucker-vassal-greeting"), Color.DarkRed, BriefingSound);
    }

    private void OnVassalShutdown(Entity<BloodsuckerVassalComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || !_mind.TryGetMind(ent, out var mindId, out _))
            return;

        _role.MindRemoveRole<BloodsuckerVassalRoleComponent>(mindId);
    }

    private void OnGetBriefing(Entity<BloodsuckerVassalRoleComponent> ent, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("bloodsucker-vassal-briefing"));
    }
}
