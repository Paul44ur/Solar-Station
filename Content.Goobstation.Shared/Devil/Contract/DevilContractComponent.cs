// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Goobstation.Shared.Devil.Contract;

[RegisterComponent]
public sealed partial class DevilContractComponent : Component
{
    /// <summary>
    /// The entity who signed the paper, AKA, the entity who has the effects applied.
    /// </summary>
    [DataField]
    public EntityUid? Signer;

    /// <summary>
    /// The entity who created the contract, AKA, the entity who gains the soul.
    /// </summary>
    [DataField]
    public EntityUid? ContractOwner;

    /// <summary>
    /// All current clauses.
    /// </summary>
    [DataField]
    public HashSet<DevilClausePrototype> CurrentClauses = [];

    /// <summary>
    /// Has the contract been signed by the signer?
    /// </summary>
    [DataField]
    public bool IsVictimSigned;

    /// <summary>
    /// Has the contract been signed by the devil?
    /// </summary>
    [DataField]
    public bool IsDevilSigned;

    /// <summary>
    /// Solar: has the devil picked this contract's clauses from the summoning menu yet?
    /// </summary>
    [DataField]
    public bool ClausesChosen;

    /// <summary>
    /// Solar: clauses a lawyer has voided through a loophole.
    /// </summary>
    [DataField]
    public HashSet<string> VoidedClauses = new();

    /// <summary>
    /// Has the contract been signed by both the devil and the victim?
    /// </summary>
    public bool IsContractFullySigned => IsVictimSigned && IsDevilSigned;

    public bool IsContractSignable => ContractWeight >= 0;

    public bool CanApplyEffects => IsContractFullySigned && IsContractSignable && Signer != null && ContractOwner != null;

    /// <summary>
    /// Does the contract weigh positively or negatively?
    /// </summary>
    /// <remarks>
    /// The higher it is, the more the cons.
    /// </remarks>
    [DataField]
    public int ContractWeight;
}

/// <summary>
/// Solar: a lawyer finished reading the contract and voids one clause.
/// </summary>
[System.Serializable, Robust.Shared.Serialization.NetSerializable]
public sealed partial class DevilLoopholeDoAfterEvent : Content.Shared.DoAfter.SimpleDoAfterEvent
{
    public string Clause = string.Empty;
}
