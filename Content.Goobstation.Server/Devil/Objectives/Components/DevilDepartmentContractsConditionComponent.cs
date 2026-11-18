// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Devil.Objectives.Components;

/// <summary>
/// Solar: the devil must get a fully signed contract from someone in each of these departments.
/// </summary>
[RegisterComponent]
public sealed partial class DevilDepartmentContractsConditionComponent : Component
{
    [DataField]
    public List<ProtoId<DepartmentPrototype>> Departments = new();

    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>> Signed = new();
}
