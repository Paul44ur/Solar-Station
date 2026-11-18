using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Bloodsuckers.Components.Vassals;

/// <summary>
/// Core component for the vassal.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodsuckerVassalComponent : Component
{
    /// <summary>
    /// The bloodsucker this vassal serves.
    /// </summary>
    [DataField]
    public EntityUid? Master;
}
