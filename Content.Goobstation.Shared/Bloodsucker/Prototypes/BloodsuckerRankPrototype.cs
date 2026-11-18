using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Bloodsuckers.Prototypes;

/// <summary>
/// Powers a bloodsucker unlocks on reaching a rank.
/// </summary>
[Prototype("bloodsuckerRank")]
public sealed partial class BloodsuckerRankPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public int Rank;

    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public List<EntProtoId> Actions = new();
}
