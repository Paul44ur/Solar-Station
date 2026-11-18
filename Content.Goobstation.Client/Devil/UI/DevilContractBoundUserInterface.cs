// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Devil.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Goobstation.Client.Devil.UI;

[UsedImplicitly]
public sealed class DevilContractBoundUserInterface : BoundUserInterface
{
    private DevilContractClauseMenu? _menu;

    public DevilContractBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        // Solar: the contract is spawned and its menu opened in the same tick, which can open this BUI twice.
        // Creating a second window crashes the client, so keep the one we have.
        if (_menu != null)
            return;

        _menu = this.CreateWindow<DevilContractClauseMenu>();
        _menu.Confirmed += (clauses, damned) =>
        {
            SendMessage(new DevilContractClausesChosenMessage(clauses, damned));
            Close();
        };

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is DevilContractMenuState menuState)
            _menu?.SetMass(menuState.Mass);
    }
}
