using Content.Goobstation.Server.Bloodsuckers.Systems;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Actions;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using System.Linq;
using Content.Server.Actions;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Goobstation.Bloodsucker;

[TestFixture]
public sealed class BloodsuckerRankTest
{
    [Test]
    public async Task PowersUnlockByRank()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<ActionsSystem>();
        var rank = entMan.System<BloodsuckerRankSystem>();

        await server.WaitAssertion(() =>
        {
            var urist = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            var comp = entMan.EnsureComponent<BloodsuckerComponent>(urist);
            var startActions = actions.GetActions(urist).Count();

            Assert.Multiple(() =>
            {
                Assert.That(comp.Rank, Is.EqualTo(1));
                Assert.That(entMan.HasComponent<BloodsuckerFeedComponent>(urist));
                Assert.That(entMan.HasComponent<BloodsuckerLungeComponent>(urist), Is.False);
                Assert.That(startActions, Is.GreaterThan(0));
            });

            rank.RankUp((urist, comp), string.Empty);
            rank.RankUp((urist, comp), string.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(comp.Rank, Is.EqualTo(3));
                Assert.That(entMan.HasComponent<BloodsuckerLungeComponent>(urist));
                Assert.That(entMan.HasComponent<BloodsuckerMesmerizeComponent>(urist), Is.False);
                Assert.That(actions.GetActions(urist).Count(), Is.GreaterThan(startActions));
            });

            var ev = new BloodsuckerDrankBloodEvent(comp.BloodPerRank);
            entMan.EventBus.RaiseLocalEvent(urist, ref ev);

            Assert.Multiple(() =>
            {
                Assert.That(comp.Rank, Is.EqualTo(4));
                Assert.That(comp.BloodDrunk, Is.EqualTo(comp.BloodPerRank));
                Assert.That(entMan.HasComponent<BloodsuckerMesmerizeComponent>(urist));
            });

            entMan.DeleteEntity(urist);
        });

        await pair.CleanReturnAsync();
    }
}
