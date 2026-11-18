using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Bible;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Bloodsuckers.Systems;

/// <summary>
/// Lets chaplains bless melee weapons with their bible so they deal holy damage for a while.
/// </summary>
public sealed partial class BlessedWeaponSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PointLightSystem _light = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;

    private static readonly TimeSpan BlessingDuration = TimeSpan.FromMinutes(10);
    private static readonly Color GlowColor = Color.FromHex("#FFE8A3");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeWeaponComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BlessedWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<BlessedWeaponComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInteractUsing(Entity<MeleeWeaponComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || !TryComp<BibleComponent>(args.Used, out var bible)
            || !HasComp<BibleUserComponent>(args.User)
            || !HasComp<ItemComponent>(ent)
            || HasComp<MobStateComponent>(ent)
            || HasComp<GunComponent>(ent))
            return;

        args.Handled = true;

        var blessed = EnsureComp<BlessedWeaponComponent>(ent);
        blessed.ExpireTime = _timing.CurTime + BlessingDuration;

        if (!HasComp<PointLightComponent>(ent))
        {
            var light = _light.EnsureLight(ent);
            _light.SetColor(ent, GlowColor, light);
            _light.SetRadius(ent, 1.5f, light);
            _light.SetEnergy(ent, 1.5f, light);
            blessed.AddedLight = true;
        }

        Dirty(ent, blessed);
        _audio.PlayPvs(bible.HealSoundPath, ent);
        _popup.PopupEntity(Loc.GetString("blessed-weapon-blessed", ("weapon", ent.Owner)), ent, args.User);
    }

    private void OnMeleeHit(EntityUid uid, BlessedWeaponComponent comp, MeleeHitEvent args)
    {
        if (args.IsHit && args.HitEntities.Count > 0)
            args.BonusDamage += comp.Damage;
    }

    private void OnExamined(Entity<BlessedWeaponComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("blessed-weapon-examine"));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BlessedWeaponComponent>();
        while (query.MoveNext(out var uid, out var blessed))
        {
            if (blessed.ExpireTime > _timing.CurTime)
                continue;

            if (blessed.AddedLight)
                RemComp<PointLightComponent>(uid);

            RemCompDeferred<BlessedWeaponComponent>(uid);
            _popup.PopupEntity(Loc.GetString("blessed-weapon-faded", ("weapon", uid)), uid);
        }
    }
}
