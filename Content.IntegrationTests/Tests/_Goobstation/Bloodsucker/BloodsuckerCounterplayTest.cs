using System.Collections.Generic;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Actions;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Goobstation.Bloodsucker;

[TestFixture]
public sealed class BloodsuckerCounterplayTest
{
    [Test]
    public async Task HolyWaterAndWeapons()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var reactive = entMan.System<ReactiveSystem>();
        var stamina = entMan.System<SharedStaminaSystem>();
        var damageable = entMan.System<DamageableSystem>();

        EntityUid vamp = default;
        await server.WaitPost(() =>
        {
            vamp = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            entMan.EnsureComponent<BloodsuckerComponent>(vamp);
            reactive.ReactionEntity(vamp, ReactionMethod.Touch, new ReagentQuantity("Holywater", FixedPoint2.New(20)));
        });
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            var damage = entMan.GetComponent<DamageableComponent>(vamp).Damage;
            Assert.That(damage.DamageDict.GetValueOrDefault("Holy", FixedPoint2.Zero), Is.GreaterThan(FixedPoint2.Zero), "Holy water should burn a bloodsucker.");

            var weapon = entMan.SpawnEntity("CombatKnife", testMap.GridCoords);
            var attacked = new AttackedEvent(weapon, weapon, testMap.GridCoords);
            entMan.EventBus.RaiseLocalEvent(vamp, attacked);
            Assert.That(attacked.ModifiersList, Has.Count.EqualTo(1), "Unblessed weapons should barely hurt.");

            entMan.EnsureComponent<BlessedWeaponComponent>(weapon);
            var blessedAttack = new AttackedEvent(weapon, weapon, testMap.GridCoords);
            entMan.EventBus.RaiseLocalEvent(vamp, blessedAttack);
            Assert.That(blessedAttack.ModifiersList, Is.Empty, "Blessed weapons should hurt fully.");

            var bullet = entMan.SpawnEntity("BulletDisabler", testMap.GridCoords);
            var stam = entMan.GetComponent<StaminaComponent>(vamp);
            var before = stam.StaminaDamage;
            stamina.TakeStaminaDamage(vamp, 20f, stam, source: bullet, ignoreResist: true);
            Assert.That(stam.StaminaDamage - before, Is.EqualTo(6f).Within(0.01f), "Disablers should do 30% stamina damage.");

            var laser = entMan.SpawnEntity("WeaponLaserCarbine", testMap.GridCoords);
            damageable.TryChangeDamage(vamp, new DamageSpecifier { DamageDict = { ["Heat"] = 10 } }, out var dealt, origin: laser, canMiss: false);
            Assert.That(dealt.GetTotal(), Is.EqualTo(FixedPoint2.New(8)), "Lasers should do 80% damage.");

            var masquerade = entMan.EnsureComponent<BloodsuckerMasqueradeComponent>(vamp);
            entMan.EventBus.RaiseLocalEvent(vamp, new BloodsuckerMasqueradeEvent());
            Assert.That(masquerade.Active);
            damageable.TryChangeDamage(vamp, new DamageSpecifier { DamageDict = { ["Blunt"] = 35 } }, canMiss: false);
            Assert.That(masquerade.Active, Is.False, "Taking 30 damage should break the masquerade.");

            entMan.DeleteEntity(vamp);
            entMan.DeleteEntity(weapon);
            entMan.DeleteEntity(bullet);
            entMan.DeleteEntity(laser);
        });

        await pair.CleanReturnAsync();
    }
}
