// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Devil.UI;

[Serializable, NetSerializable]
public enum DevilContractUiKey
{
    Key,
}

/// <summary>
/// Sent by the devil when they confirm which clauses go on a freshly summoned contract.
/// </summary>
[Serializable, NetSerializable]
public sealed class DevilContractClausesChosenMessage(List<string> clauses, string? damnedPrice = null) : BoundUserInterfaceMessage
{
    public List<string> Clauses { get; } = clauses;

    /// <summary>
    /// Solar: for a mass contract, the price taken instead of Soul Ownership from mortals whose soul is already owned.
    /// </summary>
    public string? DamnedPrice { get; } = damnedPrice;
}

/// <summary>
/// Solar: tells the clause menu whether it is setting up a mass contract.
/// </summary>
[Serializable, NetSerializable]
public sealed class DevilContractMenuState(bool mass) : BoundUserInterfaceState
{
    public bool Mass { get; } = mass;
}

[Serializable, NetSerializable]
public enum DevilMassContractOfferUiKey
{
    Key,
}

/// <summary>
/// Solar: a mortal's answer to a mass contract offer.
/// </summary>
[Serializable, NetSerializable]
public sealed class DevilMassContractOfferMessage(bool accepted) : BoundUserInterfaceMessage
{
    public bool Accepted { get; } = accepted;
}
