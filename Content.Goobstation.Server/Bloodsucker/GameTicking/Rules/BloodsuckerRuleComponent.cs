using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Bloodsuckers.GameTicking.Rules;

[RegisterComponent, Access(typeof(BloodsuckerRuleSystem))]
public sealed partial class BloodsuckerRuleComponent : Component
{
    [DataField]
    public SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Goobstation/Ambience/Antag/bloodcult_gain.ogg");

    [DataField]
    public EntProtoId DayNightManager = "BloodsuckerDayNightManager";
}
