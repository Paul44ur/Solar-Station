using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Bloodsuckers.Components;

/// <summary>
/// Tracks a bloodsucker's humanity score.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodsuckerHumanityComponent : Component
{
    /// <summary>
    /// Maximum possible humanity.
    /// </summary>
    [DataField]
    public float MaxHumanity = 100f;

    /// <summary>
    /// Current humanity value.  Clamped to 0.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentHumanity = 100f;

    /// <summary>
    /// Frenzy threshold (blood units) when humanity is at maximum.
    /// At max humanity frenzy triggers latest (lowest blood value).
    /// </summary>
    [DataField]
    public float FrenzyThresholdAtMaxHumanity = 75f;

    /// <summary>
    /// Frenzy threshold (blood units) when humanity is at zero.
    /// At zero humanity frenzy triggers earliest (highest blood value).
    /// </summary>
    [DataField]
    public float FrenzyThresholdAtZeroHumanity = 300f;

    /// <summary>
    /// Burn damage per second taken during frenzy at maximum humanity.
    /// </summary>
    [DataField]
    public float FrenzyBurnDamageAtMaxHumanity = 1f;

    /// <summary>
    /// Burn damage per second taken during frenzy at zero humanity.
    /// </summary>
    [DataField]
    public float FrenzyBurnDamageAtZeroHumanity = 8f;

    /// <summary>
    /// Minimum humanity required to use gated actions
    /// </summary>
    [DataField]
    public float GatedActionMinHumanity = 30f;

    /// <summary>
    /// At or above this humanity the Masquerade path perks work.
    /// </summary>
    [DataField]
    public float HighHumanity = 70f;

    /// <summary>
    /// Extra feeding and Brawn strength at zero humanity, scaled down linearly as humanity rises.
    /// </summary>
    [DataField]
    public float MonsterBonus = 0.5f;

    [DataField]
    public ProtoId<AlertPrototype> Alert = "BloodsuckerHumanity";
}
