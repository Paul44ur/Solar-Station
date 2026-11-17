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

    public event Action<List<string>, string?>? Confirmed;

    /// <summary>
    /// Solar: prices offered instead of Soul Ownership to mortals whose soul is already owned (mass contract only).
    /// </summary>
    private static readonly string[] DamnedPrices = ["lifeforce", "swiftness", "might", "ahand", "aleg", "anorgan"];

    private readonly BoxContainer _damnedRow;
    private readonly OptionButton _damnedPicker;

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

        // Mass contract only: what to take from those whose soul is already owned.
        _damnedRow = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, Visible = false };
        _damnedRow.AddChild(new Label { Text = Loc.GetString("devil-contract-menu-damned-price") });
        _damnedPicker = new OptionButton { HorizontalExpand = true };
        for (var i = 0; i < DamnedPrices.Length; i++)
            _damnedPicker.AddItem(DevilClauseNames.GetName(DamnedPrices[i]), i);
        _damnedPicker.OnItemSelected += args => _damnedPicker.SelectId(args.Id);
        _damnedRow.AddChild(_damnedPicker);
        root.AddChild(_damnedRow);

        _weightLabel = new Label();
        root.AddChild(_weightLabel);

        _confirmButton = new Button { Text = Loc.GetString("devil-contract-menu-confirm") };
        _confirmButton.OnPressed += _ =>
        {
            var damned = _damnedRow.Visible ? DamnedPrices[_damnedPicker.SelectedId] : null;
            Confirmed?.Invoke(_clauses.Where(c => c.Value.Box.Pressed).Select(c => c.Key).ToList(), damned);
        };
        root.AddChild(_confirmButton);

        Contents.AddChild(root);
        UpdateWeight();
    }

    /// <summary>
    /// Solar: switch the menu into mass contract mode.
    /// </summary>
    public void SetMass(bool mass)
    {
        _damnedRow.Visible = mass;
        Title = Loc.GetString(mass ? "devil-mass-contract-menu-title" : "devil-contract-menu-title");
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
