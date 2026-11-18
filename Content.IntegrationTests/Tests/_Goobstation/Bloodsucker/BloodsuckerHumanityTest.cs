using Content.Goobstation.Server.Bloodsuckers.Systems;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Actions;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Goobstation.Shared.Bloodsuckers.Systems;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Goobstation.Bloodsucker;

[TestFixture]
public sealed class BloodsuckerHumanityTest
{
    [Test]
    public async Task PatientGrowthAndBeguile()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var rank = entMan.System<BloodsuckerRankSystem>();
        var humanity = entMan.System<BloodsuckerHumanitySystem>();
        var minds = entMan.System<SharedMindSystem>();

        await server.WaitAssertion(() =>
        {
            var urist = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            var comp = entMan.EnsureComponent<BloodsuckerComponent>(urist);
            var hum = entMan.GetComponent<BloodsuckerHumanityComponent>(urist);
            entMan.AddComponent<InsideCoffinComponent>(urist);

            var night = new BloodsuckerNightStartedEvent();
            entMan.EventBus.RaiseEvent(EventSource.Local, ref night);
            Assert.That(comp.Rank, Is.EqualTo(3), "High humanity should give 2 ranks per Sol");

            humanity.ChangeHumanity(urist, -hum.MaxHumanity);
            Assert.That(humanity.IsLowHumanity(urist));
            entMan.EventBus.RaiseEvent(EventSource.Local, ref night);
            Assert.That(comp.Rank, Is.EqualTo(4), "Low humanity should give 1 rank per Sol");

            var victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            var mind = minds.CreateMind(null);
            minds.TransferTo(mind, victim, mind: mind);

            var beguile = entMan.GetComponent<BloodsuckerBeguileComponent>(urist);
            var ev = new BloodsuckerBeguileEvent { Performer = urist, Target = victim };
            entMan.EventBus.RaiseLocalEvent(urist, ev);
            Assert.That(beguile.Offers, Does.Not.ContainKey(victim), "Beguile needs high humanity");

            humanity.ChangeHumanity(urist, hum.MaxHumanity);
            ev = new BloodsuckerBeguileEvent { Performer = urist, Target = victim };
            entMan.EventBus.RaiseLocalEvent(urist, ev);
            Assert.That(beguile.Offers, Does.ContainKey(victim));

            entMan.DeleteEntity(urist);
            entMan.DeleteEntity(victim);
        });

        await pair.CleanReturnAsync();
    }
}
