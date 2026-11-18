// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Goobstation.Shared.Devil.Components;

/// <summary>
/// Solar: the Archdevil's infernal aura. Burns living things near it, except devils and their minions.
/// Toggled with the Hell's Seven Rings action.
/// </summary>
[RegisterComponent]
public sealed partial class InfernalAuraComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public DamageSpecifier Damage = new() { DamageDict = { ["Heat"] = 2 } };

    [DataField]
    public float Range = 4f;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan NextTick;
}

/// <summary>
/// Solar: marks a devil's minion (such as an imp) so devil effects leave it alone.
/// </summary>
[RegisterComponent]
public sealed partial class DevilMinionComponent : Component;

/// <summary>
/// Solar: this body's soul was harvested by an Archdevil. Only an empty shell remains.
/// </summary>
[RegisterComponent]
public sealed partial class SoulHarvestedComponent : Component;
