using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Bloodsuckers.Components;

/// <summary>
/// A melee weapon blessed by a chaplain. Deals holy damage until the blessing fades.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BlessedWeaponComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpireTime;

    [DataField]
    public DamageSpecifier Damage = new() { DamageDict = { ["Holy"] = 16 } };

    /// <summary>
    /// Whether the glow was added by the blessing, so it is only removed then.
    /// </summary>
    [DataField]
    public bool AddedLight;
}
