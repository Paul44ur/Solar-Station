using System.Linq;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Actions;
using Content.Goobstation.Shared.Bloodsuckers.Components.Vassals;
using Content.Server.Objectives.Components;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Goobstation.Bloodsucker;

[TestFixture]
[Category("GameRuleTests")]
public sealed class BloodsuckerRuleTest
{
    [Test]
    public async Task PlayerBecomesBloodsucker()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });
        var server = pair.Server;
        var entMan = server.EntMan;
        var ticker = server.System<GameTicker>();
        var mindSys = server.System<MindSystem>();
        var roleSys = server.System<RoleSystem>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        var minPlayers = 1;
        await server.WaitAssertion(() =>
        {
            var proto = server.ProtoMan.Index<EntityPrototype>("Bloodsucker");
            Assert.That(proto.TryGetComponent<GameRuleComponent>(out var rule, compFact));
            minPlayers = rule!.MinPlayers;
        });

        var dummies = await pair.Server.AddDummySessions(minPlayers);
        await pair.RunTicksSync(5);
        await pair.SetAntagPreference("Bloodsucker", true);

        await server.WaitPost(() =>
        {
            var rule = ticker.AddGameRule("Bloodsucker");
            ticker.ToggleReadyAll(true);
            ticker.StartRound();
            ticker.StartGameRule(rule);
        });
        await pair.RunTicksSync(10);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        var player = pair.Player!.AttachedEntity!.Value;
        var mind = mindSys.GetMind(player)!.Value;

        Assert.Multiple(() =>
        {
            Assert.That(entMan.HasComponent<BloodsuckerComponent>(player));
            Assert.That(entMan.HasComponent<BloodsuckerFeedComponent>(player));
            Assert.That(entMan.HasComponent<BloodsuckerGoHomeComponent>(player), Is.False);
            Assert.That(roleSys.MindHasRole<BloodsuckerRoleComponent>(mind));
            Assert.That(roleSys.MindIsAntagonist(mind));
            Assert.That(entMan.GetComponent<MindComponent>(mind).Objectives, Has.Count.EqualTo(4));
            Assert.That(entMan.EntityQuery<BloodsuckerDayNightComponent>().Count(), Is.EqualTo(1));
        });

        var thrall = dummies[0].AttachedEntity!.Value;
        await server.WaitPost(() => entMan.AddComponent(thrall, new BloodsuckerVassalComponent { Master = player }));
        await pair.RunTicksSync(5);

        var thrallMind = entMan.GetComponent<MindComponent>(mindSys.GetMind(thrall)!.Value);
        Assert.Multiple(() =>
        {
            Assert.That(roleSys.MindHasRole<BloodsuckerVassalRoleComponent>(mindSys.GetMind(thrall)!.Value));
            Assert.That(thrallMind.Objectives.Any(o =>
                entMan.TryGetComponent<TargetObjectiveComponent>(o, out var target) && target.Target == mind));
        });

        await pair.CleanReturnAsync();
    }
}
