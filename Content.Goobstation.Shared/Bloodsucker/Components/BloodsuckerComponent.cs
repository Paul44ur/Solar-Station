using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Bloodsuckers.Components;

/// <summary>
/// Core component for the Bloodsucker.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodsuckerComponent : Component
{
    /// <summary>
    /// Blood drained per second just from existing.
    /// </summary>
    [DataField]
    public float PassiveBloodDrainPerSecond = 0.1f;

    [DataField]
    public float CoffinHealPerSecond = 1f;

    [DataField]
    public ProtoId<AlertPrototype> BloodAlert = "BloodsuckerBlood";

    [DataField]
    public ProtoId<AlertPrototype> RankAlert = "BloodsuckerRank";

    [DataField]
    public ProtoId<AlertPrototype> SolAlert = "BloodsuckerSol";

    /// <summary>
    /// How much blood the vampire currently has, used to update the UI element.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentBlood;

    /// <summary>
    /// Current vampire rank/level.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Rank = 1;

    /// <summary>
    /// Highest rank whose powers have been granted.
    /// </summary>
    [DataField]
    public int UnlockedRank;

    /// <summary>
    /// Blood drunk from victims, in units, over the whole round.
    /// </summary>
    [DataField]
    public float BloodDrunk;

    /// <summary>
    /// Blood that has to be drunk to gain a rank.
    /// </summary>
    [DataField]
    public float BloodPerRank = 300f;

    [DataField]
    public float BloodTowardsRank;

    [DataField]
    public int VassalsMade;

    /// <summary>
    /// Blood volume (units) at or below which frenzy triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrenzyThreshold = 150f;

    /// <summary>Blood volume (units) required to exit frenzy.</summary>
    [DataField]
    public float FrenzyExitThreshold = 300f;

    /// <summary>Multiplier applied to flash stun duration against this entity.</summary>
    [DataField]
    public float FlashDurationMultiplier = 3f;

    /// <summary>Multiplier for stamina damage from projectiles like disablers.</summary>
    [DataField]
    public float ProjectileStaminaMultiplier = 0.3f;

    /// <summary>Multiplier for damage from hitscan weapons like lasers.</summary>
    [DataField]
    public float HitscanDamageMultiplier = 0.8f;

    /// <summary>Damage taken from melee weapons that are not blessed.</summary>
    [DataField]
    public DamageModifierSet UnblessedMeleeModifiers = new()
    {
        Coefficients =
        {
            ["Blunt"] = 0.25f,
            ["Slash"] = 0.25f,
            ["Piercing"] = 0.25f,
            ["Heat"] = 0.25f,
            ["Cold"] = 0.25f,
            ["Shock"] = 0.25f,
            ["Caustic"] = 0.25f,
        },
    };

    /// <summary>
    /// Entity UID of the coffin this bloodsucker has claimed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ClaimedCoffin;

    /// <summary>
    /// When true the entity passes medical scans as alive and loses
    /// vampire defensive buffs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsMasquerading;
}
