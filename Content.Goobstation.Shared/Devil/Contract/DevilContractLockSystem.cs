// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Devil.Contract;

/// <summary>
/// Solar: devil contracts get their clauses from the summoning menu, so pens can't change them afterwards.
/// </summary>
public sealed partial class DevilContractLockSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> WriteTag = "Write";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevilContractComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(PaperSystem)]);
    }

    private void OnInteractUsing(Entity<DevilContractComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || !_tag.HasTag(args.Used, WriteTag)
            || !TryComp<PaperComponent>(ent, out var paper)
            || !paper.EditingDisabled)
            return;

        _popup.PopupClient(Loc.GetString("devil-contract-locked"), ent, args.User);
        args.Handled = true;
    }
}
