// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Devil.Components;

/// <summary>
/// Solar: a mortal who paid a devil with their life force, speed or might.
/// Everything is given back when the devil who took it dies.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DevilTithedComponent : Component
{
    /// <summary>
    /// The mind of the devil who took the tithe, so it survives the devil changing forms.
    /// </summary>
    [DataField]
    public EntityUid? DevilMind;

    /// <summary>
    /// Max health taken (shown to medics as devilish burns).
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealthLost;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float MeleeMultiplier = 1f;
}

/// <summary>
/// Solar: what a devil has gained from tithes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DevilTitheBonusComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealthGained;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float MeleeMultiplier = 1f;
}
