// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Devil.Contract;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Devil.UI;

/// <summary>
/// Solar: lets the devil tick which clauses go on a contract they just summoned.
/// </summary>
public sealed partial class DevilContractClauseMenu : DefaultWindow
{
    [Dependency] private IPrototypeManager _proto = default!;

    public event Action<List<string>>? Confirmed;

    private readonly Dictionary<string, (CheckBox Box, int Weight)> _clauses = new();
    private readonly Label _weightLabel;
    private readonly Button _confirmButton;

    private static readonly HashSet<string> DefaultClauses = ["soulownership", "death"];

    public DevilContractClauseMenu()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("devil-contract-menu-title");
        MinSize = new(420, 520);

        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new(4) };

        root.AddChild(new Label
        {
            Text = Loc.GetString("devil-contract-menu-prompt"),
        });

        var list = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        var scroll = new ScrollContainer { VerticalExpand = true, HScrollEnabled = false };
        scroll.AddChild(list);
        root.AddChild(scroll);

        // Prices the mortal pays (positive weight) first, then the gifts they get (zero or negative weight).
        var clauses = _proto.EnumeratePrototypes<DevilClausePrototype>()
            .OrderByDescending(c => c.ClauseWeight)
            .ThenBy(c => c.ID)
            .ToList();

        AddSection(list, "devil-contract-menu-prices", clauses.Where(c => c.ClauseWeight > 0));
        AddSection(list, "devil-contract-menu-gifts", clauses.Where(c => c.ClauseWeight <= 0));

        _weightLabel = new Label();
        root.AddChild(_weightLabel);

        _confirmButton = new Button { Text = Loc.GetString("devil-contract-menu-confirm") };
        _confirmButton.OnPressed += _ =>
        {
            Confirmed?.Invoke(_clauses.Where(c => c.Value.Box.Pressed).Select(c => c.Key).ToList());
        };
        root.AddChild(_confirmButton);

        Contents.AddChild(root);
        UpdateWeight();
    }

    private void AddSection(BoxContainer list, string header, IEnumerable<DevilClausePrototype> clauses)
    {
        list.AddChild(new Label
        {
            Text = Loc.GetString(header),
            StyleClasses = { "LabelHeading" },
            Margin = new(0, 6, 0, 2),
        });

        foreach (var clause in clauses)
        {
            var box = new CheckBox
            {
                Text = Loc.GetString("devil-contract-menu-clause",
                    ("name", DevilClauseNames.GetName(clause.ID)),
                    ("weight", clause.ClauseWeight)),
                Pressed = DefaultClauses.Contains(clause.ID),
            };

            box.OnToggled += _ => UpdateWeight();
            _clauses[clause.ID] = (box, clause.ClauseWeight);
            list.AddChild(box);

            if (Loc.TryGetString($"devil-clause-desc-{clause.ID}", out var desc))
            {
                list.AddChild(new RichTextLabel
                {
                    Text = $"[color=#999999]{desc}[/color]",
                    Margin = new(24, 0, 4, 4),
                });
            }
        }
    }

    private void UpdateWeight()
    {
        var weight = _clauses.Values.Where(c => c.Box.Pressed).Sum(c => c.Weight);
        _weightLabel.Text = Loc.GetString("devil-contract-menu-weight", ("weight", weight));
        _confirmButton.Disabled = weight < 0;
    }
}
