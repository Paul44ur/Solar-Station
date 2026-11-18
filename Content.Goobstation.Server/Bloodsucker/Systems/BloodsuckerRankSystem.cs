using System.Linq;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Goobstation.Shared.Bloodsuckers.Prototypes;
using Content.Goobstation.Shared.Bloodsuckers.Systems;
using Content.Server.Antag;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Bloodsuckers.Systems;

/// <summary>
/// Ranks bloodsuckers up for sleeping through Sol in a coffin and for drinking blood, unlocking new powers.
/// </summary>
public sealed partial class BloodsuckerRankSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private BloodsuckerHumanitySystem _humanity = default!;

    private const float SolHumanityGain = 2f;

    private static readonly SoundSpecifier RankUpSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Ambience/Antag/bloodcult_eyes.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodsuckerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodsuckerComponent, BloodsuckerDrankBloodEvent>(OnDrankBlood);
        SubscribeLocalEvent<BloodsuckerNightStartedEvent>(OnNightStarted);
    }

    private void OnMapInit(Entity<BloodsuckerComponent> ent, ref MapInitEvent args)
    {
        UnlockPowers(ent, false);
    }

    private void OnDrankBlood(Entity<BloodsuckerComponent> ent, ref BloodsuckerDrankBloodEvent args)
    {
        ent.Comp.BloodDrunk += args.Amount;
        ent.Comp.BloodTowardsRank += args.Amount;

        while (ent.Comp.BloodPerRank > 0 && ent.Comp.BloodTowardsRank >= ent.Comp.BloodPerRank)
        {
            ent.Comp.BloodTowardsRank -= ent.Comp.BloodPerRank;
            RankUp(ent, Loc.GetString("bloodsucker-rank-up-blood"));
        }
    }

    private void OnNightStarted(ref BloodsuckerNightStartedEvent args)
    {
        var query = EntityQueryEnumerator<BloodsuckerComponent, InsideCoffinComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            // Patient bloodsuckers grow twice as fast from resting.
            var patient = _humanity.IsHighHumanity(uid);
            _humanity.ChangeHumanity(uid, SolHumanityGain);

            RankUp((uid, comp), Loc.GetString("bloodsucker-rank-up-sol"));
            if (patient)
                RankUp((uid, comp), Loc.GetString("bloodsucker-rank-up-patient"));
        }
    }

    public void RankUp(Entity<BloodsuckerComponent> ent, string reason)
    {
        ent.Comp.Rank++;
        Dirty(ent);

        var powers = UnlockPowers(ent, true);
        var message = powers.Count > 0
            ? Loc.GetString("bloodsucker-rank-up-powers", ("reason", reason), ("rank", ent.Comp.Rank), ("powers", string.Join(", ", powers)))
            : Loc.GetString("bloodsucker-rank-up", ("reason", reason), ("rank", ent.Comp.Rank));

        _antag.SendBriefing(ent, message, Color.DarkRed, RankUpSound);
    }

    private List<string> UnlockPowers(Entity<BloodsuckerComponent> ent, bool getNames)
    {
        var names = new List<string>();
        var ranks = _proto.EnumeratePrototypes<BloodsuckerRankPrototype>()
            .Where(r => r.Rank > ent.Comp.UnlockedRank && r.Rank <= ent.Comp.Rank)
            .OrderBy(r => r.Rank);

        foreach (var rank in ranks)
        {
            EntityManager.AddComponents(ent, rank.Components, false);

            foreach (var action in rank.Actions)
            {
                _actions.AddAction(ent, action);

                if (getNames && _proto.TryIndex(action, out var proto))
                    names.Add(proto.Name);
            }
        }

        ent.Comp.UnlockedRank = Math.Max(ent.Comp.UnlockedRank, ent.Comp.Rank);
        return names;
    }
}
