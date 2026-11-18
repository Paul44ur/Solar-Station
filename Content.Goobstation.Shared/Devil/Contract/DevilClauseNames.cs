// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Devil.Contract;

/// <summary>
/// Solar: readable clause names. The contract parser lowercases a line and strips spaces to find the clause,
/// so a name is only used when it still maps back to the clause id.
/// </summary>
public static class DevilClauseNames
{
    public static string GetName(string clauseId)
    {
        if (!Loc.TryGetString($"devil-clause-name-{clauseId}", out var name))
            return clauseId;

        return name.Replace(" ", "").Equals(clauseId, StringComparison.OrdinalIgnoreCase) ? name : clauseId;
    }
}
