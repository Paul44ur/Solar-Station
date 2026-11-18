using Robust.Shared.GameStates;

namespace Content.Goobstation.Common.Bloodsuckers;

/// <summary>
/// The owner was hit by Forget and sees <see cref="Target"/> as "Unknown" until <see cref="EndTime"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodsuckerForgottenComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField]
    public TimeSpan EndTime;
}
