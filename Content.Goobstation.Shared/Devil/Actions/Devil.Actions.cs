// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Devil.Actions;

public sealed partial class CreateContractEvent : InstantActionEvent;

public sealed partial class CreateRevivalContractEvent : InstantActionEvent;

public sealed partial class ShadowJauntEvent : InstantActionEvent;

public sealed partial class DevilGripEvent : InstantActionEvent;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DevilGripCurseRotPurchasedEvent : EntityEventArgs { }

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DevilGripEnhancedPurchasedEvent : EntityEventArgs { }

public sealed partial class DevilSummonPitchforkEvent : InstantActionEvent;

public sealed partial class DevilHellstepEvent : InstantActionEvent;

public sealed partial class DevilHeresyEvent : InstantActionEvent;

public sealed partial class DevilAuthorityEvent : InstantActionEvent;

public sealed partial class DevilPossessionEvent : EntityTargetActionEvent;

// Solar: Archdevil toggle for its infernal aura (Hell's Seven Rings).
public sealed partial class DevilToggleInfernalAuraEvent : InstantActionEvent;

// Solar: Archdevil collects an owned soul on touch.
public sealed partial class DevilSoulHarvestEvent : EntityTargetActionEvent;

// Solar: Archdevil offers one contract to everyone around it at once.
public sealed partial class DevilMassContractEvent : InstantActionEvent;

// Solar: lesser form summons imps.
public sealed partial class DevilSummonImpsEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId ImpPrototype = "MobDevilImp";

    [DataField]
    public int Amount = 2;
}

// Solar: lesser form ability that mutes a target for a short time.
public sealed partial class DevilVoiceOfHellEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);
}

public sealed partial class DevilOpenStoreEvent : InstantActionEvent;

[Serializable]
public sealed partial class BecomeLesserDevilEvent : InstantActionEvent
{
    public EntProtoId Prototype = "MobLesserDevil";
}

[Serializable]
public sealed partial class BecomeArchdevilEvent : InstantActionEvent
{
    public EntProtoId Prototype = "MobArchDevil";
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DevilFireImmuneEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class DevilHeresyDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity AnimationEntity;

    public DevilHeresyDoAfterEvent(NetEntity animationEntity)
    {
        AnimationEntity = animationEntity;
    }
}
