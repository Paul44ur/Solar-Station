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
public sealed class DevilContractClausesChosenMessage(List<string> clauses) : BoundUserInterfaceMessage
{
    public List<string> Clauses { get; } = clauses;
}
