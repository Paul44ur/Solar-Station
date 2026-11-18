using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Bloodsuckers.Components.Actions;

/// <summary>
/// Lets a high-humanity bloodsucker offer a willing crew member the dark gift, making them a vassal without a rack.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodsuckerBeguileComponent : Component
{
    [DataField]
    public TimeSpan OfferDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Crew members with a pending offer and when it runs out.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan> Offers = new();
}
