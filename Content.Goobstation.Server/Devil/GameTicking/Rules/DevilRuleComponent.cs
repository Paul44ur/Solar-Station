// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.NPC.Prototypes;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Devil.GameTicking.Rules;

[RegisterComponent, Access(typeof(DevilRuleSystem), typeof(Contract.DevilContractSystem))] // Solar: the contract system writes the deal ledger
public sealed partial class DevilRuleComponent : Component
{
    [DataField]
    public SoundPathSpecifier BriefingSound = new("/Audio/_Goobstation/Ambience/Antag/devil_start.ogg");

    [DataField]
    public ProtoId<NpcFactionPrototype> DevilFaction = "DevilFaction";

    [DataField]
    public ProtoId<NpcFactionPrototype> NanotrasenFaction = "NanoTrasen";

    [DataField]
    public EntProtoId DevilMindRole = "DevilMindRole";

    [DataField]
    public ProtoId<StoreCategoryPrototype> StoreCategory = "DevilAbilities";

    [DataField]
    public bool AscensionAnnounced = false;

    [DataField]
    public SoundPathSpecifier AscensionSound = new("/Audio/_Goobstation/Devil/demonchant3.ogg");

    /// <summary>
    /// Solar: every deal closed this round, shown on the round end screen.
    /// </summary>
    [ViewVariables]
    public List<DevilDealRecord> Deals = new();
}

/// <summary>
/// Solar: one signed devil contract, for the round end deal ledger.
/// </summary>
public sealed class DevilDealRecord
{
    public EntityUid Contract;
    public string Devil = string.Empty;
    public string Signer = string.Empty;
    public EntityUid SignerEntity;
    public List<string> Clauses = new();
    public List<string> Voided = new();
    public bool Harvested;
}
