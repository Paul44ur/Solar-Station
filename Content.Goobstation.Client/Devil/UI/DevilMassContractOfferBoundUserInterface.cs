// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Devil.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Goobstation.Client.Devil.UI;

/// <summary>
/// Solar: the Accept/Decline window a mortal gets with a mass contract.
/// </summary>
[UsedImplicitly]
public sealed class DevilMassContractOfferBoundUserInterface : BoundUserInterface
{
    private DefaultWindow? _window;

    public DevilMassContractOfferBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        // Solar: the offer is spawned and opened in the same tick, which can open this BUI twice.
        if (_window != null)
            return;

        _window = this.CreateWindow<DefaultWindow>();
        _window.Title = Loc.GetString("devil-mass-contract-offer-title");
        _window.MinWidth = 340;

        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new(4) };
        root.AddChild(new RichTextLabel { Text = Loc.GetString("devil-mass-contract-offer-prompt") });

        var accept = new Button { Text = Loc.GetString("devil-mass-contract-offer-accept") };
        accept.OnPressed += _ => Answer(true);
        root.AddChild(accept);

        var decline = new Button { Text = Loc.GetString("devil-mass-contract-offer-decline") };
        decline.OnPressed += _ => Answer(false);
        root.AddChild(decline);

        _window.Contents.AddChild(root);
        _window.OpenCentered();
    }

    private void Answer(bool accepted)
    {
        SendMessage(new DevilMassContractOfferMessage(accepted));
        Close();
    }
}
