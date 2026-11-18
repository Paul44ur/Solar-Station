// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Server.Devil.Objectives.Components;

/// <summary>
/// Solar: collect souls equal to a random share of the players in the round.
/// </summary>
[RegisterComponent]
public sealed partial class DevilSoulPercentObjectiveComponent : Component
{
    [DataField]
    public float MinFraction = 0.35f;

    [DataField]
    public float MaxFraction = 0.6f;

    /// <summary>
    /// The lowest the target can go, for small rounds.
    /// </summary>
    [DataField]
    public int Minimum = 5;

    [DataField]
    public int Target;

    [DataField]
    public int Souls;

    [DataField]
    public LocId Title = "objective-condition-souls-percent-title";

    [DataField]
    public LocId Description = "objective-condition-souls-percent-description";
}
