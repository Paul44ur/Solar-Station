using Content.Goobstation.Common.Bloodsuckers;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.IdentityManagement;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Goobstation.Bloodsucker;

[TestFixture]
public sealed class BloodsuckerForgetTest
{
    [Test]
    public async Task ForgetBlursAndHidesIdentity()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var urist = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            entMan.EnsureComponent<BloodsuckerComponent>(urist);
            var victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            var bystander = entMan.SpawnEntity("MobHuman", testMap.GridCoords);

            var ev = new BloodsuckerForgetEvent(victim);
            entMan.EventBus.RaiseLocalEvent(urist, ref ev);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<BlurryVisionComponent>(victim));
                Assert.That(entMan.GetComponent<BloodsuckerForgottenComponent>(victim).Target, Is.EqualTo(urist));
                Assert.That(Identity.Name(urist, entMan, victim), Is.EqualTo("Unknown"));
                Assert.That(Identity.Name(urist, entMan, bystander), Is.Not.EqualTo("Unknown"));
            });

            entMan.DeleteEntity(urist);
            entMan.DeleteEntity(victim);
            entMan.DeleteEntity(bystander);
        });

        await pair.CleanReturnAsync();
    }
}
