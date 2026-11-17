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

        _menu = this.CreateWindow<DevilContractClauseMenu>();
        _menu.Confirmed += clauses =>
        {
            SendMessage(new DevilContractClausesChosenMessage(clauses));
            Close();
        };

        _menu.OpenCentered();
    }
}
