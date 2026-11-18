using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Actions;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Goobstation.Bloodsucker;

[TestFixture]
public sealed class BloodsuckerOlfactionTest
{
    [Test]
    public async Task TracksBloodOwner()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var puddles = entMan.System<SharedPuddleSystem>();
        var solutions = entMan.System<SharedSolutionContainerSystem>();

        EntityUid urist = default;
        EntityUid victim = default;
        EntityUid puddle = default;

        await server.WaitAssertion(() =>
        {
            urist = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            entMan.EnsureComponent<BloodsuckerComponent>(urist);
            entMan.EnsureComponent<BloodsuckerOlfactionComponent>(urist);
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords.Offset(new(5, 0)));
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            var bs = entMan.GetComponent<BloodstreamComponent>(victim);
            Assert.That(solutions.ResolveSolution(victim, bs.BloodSolutionName, ref bs.BloodSolution));
            var blood = solutions.SplitSolution(bs.BloodSolution!.Value, 20);
            Assert.That(puddles.TrySpillAt(testMap.GridCoords, blood, out puddle, false));

            var ev = new BloodsuckerOlfactionEvent { Performer = urist, Target = testMap.GridCoords.Offset(new(1, 0)) };
            entMan.EventBus.RaiseLocalEvent(urist, ev);

            var olf = entMan.GetComponent<BloodsuckerOlfactionComponent>(urist);
            Assert.That(olf.IsTracking);
            Assert.That(entMan.GetEntity(olf.TrackedTarget), Is.EqualTo(victim));
        });

        await pair.CleanReturnAsync();
    }
}
