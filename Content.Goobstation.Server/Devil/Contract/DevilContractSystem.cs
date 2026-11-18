// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Goobstation.Common.Paper;
using Content.Goobstation.Server.Devil.Objectives.Components;
using Content.Goobstation.Server.Possession;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Condemned;
using Content.Goobstation.Shared.Devil.Contract;
using Content.Goobstation.Shared.Devil.UI;
using Content.Shared._Imp.Drone; // Goob - Moved to shared
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Server.Implants;
using Content.Server.Mind;
using Content.Server.Polymorph.Systems;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Mindshield.Components;
using Content.Shared.Nutrition;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Goobstation.Shared.Slasher.Components;
using Content.Shared.Damage.Systems;

namespace Content.Goobstation.Server.Devil.Contract;

public sealed partial class DevilContractSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = null!;
    [Dependency] private DamageableSystem _damageable = null!;
    [Dependency] private HandsSystem _hands = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private IPrototypeManager _prototypeManager = null!;
    [Dependency] private BodySystem _bodySystem = null!;
    [Dependency] private WoundSystem _wounds = null!;
    [Dependency] private IRobustRandom _random = null!;
    [Dependency] private SubdermalImplantSystem _implant = null!;
    [Dependency] private PolymorphSystem _polymorph = null!;
    [Dependency] private ExplosionSystem _explosion = null!;
    [Dependency] private MindSystem _mind = null!;
    [Dependency] private SharedUserInterfaceSystem _ui = null!;
    [Dependency] private PaperSystem _paper = null!;
    [Dependency] private Content.Shared.Roles.Jobs.SharedJobSystem _jobs = null!;

    private ISawmill _sawmill = null!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeRegex();
        InitializeSpecialActions();
        InitializeMassContract(); // Solar
        InitializeLoophole(); // Solar

        SubscribeLocalEvent<DevilContractComponent, BeingSignedAttemptEvent>(OnContractSignAttempt);
        SubscribeLocalEvent<DevilContractComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DevilContractComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DevilContractComponent, SignSuccessfulEvent>(OnSignStep);
        SubscribeLocalEvent<DevilContractComponent, FullyEatenEvent>(OnEaten);
        SubscribeLocalEvent<DevilContractComponent, DevilContractClausesChosenMessage>(OnClausesChosen);

        _sawmill = Logger.GetSawmill("devil-contract");
    }

    private readonly Dictionary<LocId, Func<DevilContractComponent, EntityUid?>> _targetResolvers = new()
    {
        // The contractee is who is making the deal.
        ["devil-contract-contractee"] = comp => comp.Signer,
        // The contractor is the entity offering the deal.
        ["devil-contract-contractor"] = comp => comp.ContractOwner,
    };

    private Regex _clauseRegex = null!;

    private void InitializeRegex()
    {
        var escapedPatterns = _targetResolvers.Keys.Select(locId => Loc.GetString(locId)).ToList(); // malicious linq and regex
        var targetPattern = string.Join("|", escapedPatterns);

        _clauseRegex = new Regex($@"^\s*(?<target>{targetPattern})\s*:\s*(?<clause>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
    }
    private void OnGetVerbs(Entity<DevilContractComponent> contract, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract
        || !args.CanAccess
        || !TryComp<DevilComponent>(args.User, out var devilComp))
            return;

        var user = args.User;

        AlternativeVerb burnVerb = new()
        {
            Act = () => TryBurnContract(contract, (user, devilComp)),
            Text = Loc.GetString("burn-contract-prompt"),
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Effects/fire.rsi"), "fire"),
        };

        args.Verbs.Add(burnVerb);

        // Solar: reopen the clause menu if it was closed before choosing.
        if (contract.Comp.ClausesChosen
            || contract.Comp.IsVictimSigned
            || user != contract.Comp.ContractOwner)
            return;

        AlternativeVerb chooseVerb = new()
        {
            Act = () => _ui.OpenUi(contract.Owner, DevilContractUiKey.Key, user),
            Text = Loc.GetString("devil-contract-choose-clauses-verb"),
            Icon = new SpriteSpecifier.Rsi(new("_Goobstation/Actions/devil.rsi"), "summon-contract"),
            Priority = 2,
        };

        args.Verbs.Add(chooseVerb);
    }

    /// <summary>
    /// Solar: opens the clause menu for the devil who just summoned this contract.
    /// </summary>
    public void OpenClauseMenu(EntityUid contract, EntityUid devil)
    {
        _ui.OpenUi(contract, DevilContractUiKey.Key, devil);
    }

    private void OnClausesChosen(Entity<DevilContractComponent> contract, ref DevilContractClausesChosenMessage args)
    {
        var user = args.Actor;
        if (contract.Comp.ClausesChosen
            || contract.Comp.IsVictimSigned
            || user != contract.Comp.ContractOwner
            || !TryComp<PaperComponent>(contract, out var paper))
            return;

        var chosen = new List<string>();
        var weight = 0;
        foreach (var id in args.Clauses)
        {
            if (chosen.Contains(id) || !_prototypeManager.TryIndex(id, out DevilClausePrototype? clause))
                continue;

            chosen.Add(id);
            weight += clause.ClauseWeight;
        }

        if (weight < 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("contract-uneven-odds", ("number", Math.Abs(weight))), contract, user, PopupType.MediumCaution);
            return;
        }

        _ui.CloseUi(contract.Owner, DevilContractUiKey.Key);

        // Solar: a mass contract template is handed out to everyone nearby instead.
        if (HasComp<DevilMassContractTemplateComponent>(contract))
        {
            DistributeMassContract(contract, user, chosen, args.DamnedPrice);
            return;
        }

        WriteClauses((contract.Owner, contract.Comp, paper), chosen);
    }

    /// <summary>
    /// Solar: writes the clauses onto a contract, each with a line telling the mortal what it means.
    /// </summary>
    private void WriteClauses(Entity<DevilContractComponent, PaperComponent> contract, List<string> chosen)
    {
        var lines = chosen.Select(id => Loc.GetString("devil-contract-clause-line",
            ("name", DevilClauseNames.GetName(id)),
            ("desc", Loc.TryGetString($"devil-clause-contract-{id}", out var desc) ? desc : string.Empty)));

        _paper.SetContent((contract.Owner, contract.Comp2), Loc.GetString("devil-contract-content-chosen", ("clauses", string.Join("\n", lines))));
        contract.Comp1.ClausesChosen = true;
        UpdateContractWeight((contract.Owner, contract.Comp1));
    }

    private void TryBurnContract(Entity<DevilContractComponent> contract, Entity<DevilComponent> devil)
    {
        var coordinates = Transform(contract).Coordinates;

        if (contract.Comp is not { IsContractFullySigned: true})
        {
            Spawn(devil.Comp.FireEffectProto, coordinates);
            _audio.PlayPvs(devil.Comp.FwooshPath, coordinates, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f));
            QueueDel(contract);
        }
        else
            _popupSystem.PopupCoordinates(Loc.GetString("burn-contract-popup-fail"), coordinates, devil, PopupType.MediumCaution);
    }

    private void OnExamined(Entity<DevilContractComponent> contract, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        UpdateContractWeight(contract);
        args.PushMarkup(Loc.GetString("devil-contract-examined", ("weight", contract.Comp.ContractWeight)));
    }

    private void OnEaten(Entity<DevilContractComponent> contract, ref FullyEatenEvent args)
    {
        _explosion.QueueExplosion(
            args.User,
            typeId: "Default",
            totalIntensity: 1,
            slope: 1,
            maxTileIntensity: 1,
            maxTileBreak: 0,
            addLog: false);
    }

    #region Signing Steps

    private void OnContractSignAttempt(Entity<DevilContractComponent> contract, ref BeingSignedAttemptEvent args)
    {
        // Make sure that weight is set properly!
        UpdateContractWeight(contract);

        // Don't allow mortals to sign contracts for other people.
        if (contract.Comp.IsVictimSigned && args.Signer != contract.Comp.ContractOwner)
        {
            var invalidUserPopup = Loc.GetString("devil-sign-invalid-user");
            _popupSystem.PopupEntity(invalidUserPopup, contract, args.Signer);

            args.Cancelled = true;
            return;
        }

        // Only handle unsigned contracts.
        if (contract.Comp.IsVictimSigned || contract.Comp.IsDevilSigned)
            return;

        if (!IsUserValid(args.Signer, out var failReason))
        {
            _popupSystem.PopupEntity(failReason, contract, args.Signer, PopupType.MediumCaution);

            args.Cancelled = true;
            return;
        }

        // Check if the weight is too low
        if (!contract.Comp.IsContractSignable)
        {
            var difference = Math.Abs(contract.Comp.ContractWeight);

            var unevenOddsPopup = Loc.GetString("contract-uneven-odds", ("number", difference));
            _popupSystem.PopupEntity(unevenOddsPopup, contract, args.Signer, PopupType.MediumCaution);

            args.Cancelled = true;
            return;
        }

        // Check if devil is trying to sign first
        if (args.Signer == contract.Comp.ContractOwner)
        {
            var tooEarlyPopup = Loc.GetString("devil-contract-early-sign-failed");
            _popupSystem.PopupEntity(tooEarlyPopup, args.Signer, args.Signer, PopupType.MediumCaution);

            args.Cancelled = true;
        }

    }

    private void OnSignStep(Entity<DevilContractComponent> contract, ref SignSuccessfulEvent args)
    {
        // Determine signing phase
        if (!contract.Comp.IsVictimSigned)
            HandleVictimSign(contract, args.Paper, args.User);
        else if (!contract.Comp.IsDevilSigned)
            HandleDevilSign(contract, args.Paper, args.User);

        // Final activation check
        if (contract.Comp.IsContractFullySigned)
            HandleBothPartiesSigned(contract);
    }

    private void HandleVictimSign(Entity<DevilContractComponent> contract, EntityUid signed, EntityUid signer)
    {
        contract.Comp.Signer = signer;
        contract.Comp.IsVictimSigned = true;

        if (TryComp<PaperComponent>(contract, out var paper))
            paper.EditingDisabled = true;

        _popupSystem.PopupEntity(Loc.GetString("contract-victim-signed"), signed, signer);

        var signedEv = new DevilContractSignedEvent(contract);
        RaiseLocalEvent(signer, ref signedEv);
    }

    private void HandleDevilSign(Entity<DevilContractComponent> contract, EntityUid signed, EntityUid signer)
    {
        contract.Comp.IsDevilSigned = true;
        _popupSystem.PopupEntity(Loc.GetString("contract-devil-signed"), signed, signer);

        if (_mind.TryGetMind(signer, out var mindId, out var mind) &&
            _mind.TryGetObjectiveComp<MeetContractWeightConditionComponent>(mindId, out var objectiveComp, mind))
            objectiveComp.ContractWeight += contract.Comp.ContractWeight;

        // Solar: count the signer's department(s) toward "bargain with every department".
        if (_mind.TryGetMind(signer, out var devilMind, out var devilMindComp)
            && _mind.TryGetObjectiveComp<DevilDepartmentContractsConditionComponent>(devilMind, out var deptComp, devilMindComp)
            && contract.Comp.Signer is { } victim
            && _mind.TryGetMind(victim, out var victimMind, out _)
            && _jobs.MindTryGetJobId(victimMind, out var jobId)
            && jobId is { } job)
        {
            foreach (var department in _prototypeManager.EnumeratePrototypes<Content.Shared.Roles.DepartmentPrototype>())
            {
                if (department.Roles.Contains(job))
                    deptComp.Signed.Add(department.ID);
            }
        }
    }

    private void HandleBothPartiesSigned(Entity<DevilContractComponent> contract)
    {
        UpdateContractWeight(contract);
        DoContractEffects(contract);
        RecordDeal(contract); // Solar
    }

    /// <summary>
    /// Solar: write the deal into the round end ledger.
    /// </summary>
    private void RecordDeal(Entity<DevilContractComponent> contract)
    {
        if (contract.Comp.Signer is not { } signer || contract.Comp.ContractOwner is not { } owner)
            return;

        var devilName = Name(owner);
        if (TryComp<DevilComponent>(owner, out var devil) && !string.IsNullOrEmpty(devil.TrueName))
            devilName = Loc.GetString("devil-ledger-devil-name", ("name", Name(owner)), ("trueName", devil.TrueName));

        var record = new Content.Goobstation.Server.Devil.GameTicking.Rules.DevilDealRecord
        {
            Contract = contract.Owner,
            Devil = devilName,
            Signer = Name(signer),
            SignerEntity = signer,
            Clauses = contract.Comp.CurrentClauses.Select(c => DevilClauseNames.GetName(c.ID)).ToList(),
        };

        var rules = EntityQueryEnumerator<Content.Goobstation.Server.Devil.GameTicking.Rules.DevilRuleComponent>();
        if (rules.MoveNext(out var rule))
            rule.Deals.Add(record);
    }

    /// <summary>
    /// Solar: look up a deal in the ledger by its contract.
    /// </summary>
    public Content.Goobstation.Server.Devil.GameTicking.Rules.DevilDealRecord? FindDeal(EntityUid contract)
    {
        var rules = EntityQueryEnumerator<Content.Goobstation.Server.Devil.GameTicking.Rules.DevilRuleComponent>();
        while (rules.MoveNext(out var rule))
        {
            foreach (var deal in rule.Deals)
            {
                if (deal.Contract == contract)
                    return deal;
            }
        }

        return null;
    }

    /// <summary>
    /// Solar: mark every deal this mortal signed as harvested.
    /// </summary>
    public void MarkHarvested(EntityUid signer)
    {
        var rules = EntityQueryEnumerator<Content.Goobstation.Server.Devil.GameTicking.Rules.DevilRuleComponent>();
        while (rules.MoveNext(out var rule))
        {
            foreach (var deal in rule.Deals)
            {
                if (deal.SignerEntity == signer)
                    deal.Harvested = true;
            }
        }
    }

    #endregion

    #region Helper Events

    /// <summary>
    /// Solar: things that have no soul to sell at all, regardless of any earlier deal.
    /// </summary>
    public bool HasNoSoul(EntityUid user)
    {
        return HasComp<SiliconComponent>(user)
               || HasComp<DroneComponent>(user)
               || HasComp<ChangelingComponent>(user)
               || HasComp<BorgChassisComponent>(user)
               || HasComp<SoullessComponent>(user)
               || HasComp<PossessedComponent>(user);
    }

    public bool IsUserValid(EntityUid user, out string failReason)
    {
        if (HasComp<CondemnedComponent>(user)
            || HasComp<SiliconComponent>(user)
            || HasComp<DroneComponent>(user)
            || HasComp<ChangelingComponent>(user)
            || HasComp<BorgChassisComponent>(user)
            || HasComp<SoullessComponent>(user))
        {
            failReason = Loc.GetString("devil-contract-no-soul-sign-failed");
            return false;
        }

        // Solar: a mindshield doesn't protect you from a deal with hell itself, so mindshielded crew can sign.

        if (HasComp<PossessedComponent>(user))
        {
            failReason = Loc.GetString("devil-contract-early-sign-failed");
            return false;
        }

        failReason = string.Empty;
        return true;
    }
    public bool TryTransferSouls(EntityUid devil, EntityUid contractee, int added)
    {
        if (TerminatingOrDeleted(contractee))
            return false;

        // Can't sell what doesn't exist.
        if (HasComp<CondemnedComponent>(contractee)
            || HasComp<SoullessComponent>(contractee)
            || devil == contractee)
            return false;

        // Solar: one soul per person, no matter how many deals or handshakes they make.
        if (added > 0 && TryComp<DevilComponent>(devil, out var devilComp))
        {
            var source = _mind.TryGetMind(contractee, out var mindId, out _) ? mindId : contractee;
            if (!devilComp.SoulSources.Add(source))
                return false;
        }

        var ev = new SoulAmountChangedEvent(devil, contractee, added);
        RaiseLocalEvent(devil, ref ev);

        var condemned = EnsureComp<CondemnedComponent>(contractee);
        condemned.SoulOwner = devil;
        condemned.CondemnOnDeath = true;

        return true;
    }

    private void UpdateContractWeight(Entity<DevilContractComponent> contract, PaperComponent? paper = null)
    {
        if (!Resolve(contract, ref paper))
            return;

        contract.Comp.CurrentClauses.Clear();
        var newWeight = 0;

        var matches = _clauseRegex.Matches(paper.Content);
        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;

            var targetKey = match.Groups["target"].Value.Trim().ToLowerInvariant().Replace(" ", "");
            var clauseKey = match.Groups["clause"].Value.Trim().ToLowerInvariant().Replace(" ", "");

            var locId = _targetResolvers.Keys.FirstOrDefault(id =>
                Loc.GetString(id).Equals(targetKey, StringComparison.OrdinalIgnoreCase));

            if (locId == default)
                continue;

            var target = _targetResolvers[locId](contract.Comp);
            if (target == contract.Comp.ContractOwner)
                continue;

            if (!_prototypeManager.TryIndex(clauseKey, out DevilClausePrototype? clauseProto)
                || !contract.Comp.CurrentClauses.Add(clauseProto))
                continue;

            newWeight += clauseProto.ClauseWeight;
        }

        contract.Comp.ContractWeight = newWeight;
    }

    private void DoContractEffects(Entity<DevilContractComponent> contract, PaperComponent? paper = null)
    {
        if (!Resolve(contract, ref paper))
            return;

        UpdateContractWeight(contract);

        var matches = _clauseRegex.Matches(paper.Content);
        var processedClauses = new HashSet<string>();

        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;

            var targetKey = match.Groups["target"].Value.Trim().ToLowerInvariant().Replace(" ", "");
            var clauseKey = match.Groups["clause"].Value.Trim().ToLowerInvariant().Replace(" ", "");

            var locId = _targetResolvers.Keys.FirstOrDefault(id =>
                Loc.GetString(id).Equals(targetKey, StringComparison.OrdinalIgnoreCase));

            if (locId == default)
                continue;

            var targetEntity = _targetResolvers[locId](contract.Comp);

            if (targetEntity == contract.Comp.ContractOwner)
            {
                _sawmill.Warning($"Skipping clause {clauseKey}: targets contract owner.");
                if (contract.Comp.Signer is { } signer)
                    _popupSystem.PopupEntity(Loc.GetString("devil-contract-clause-targets-contractor", ("clause", clauseKey)), contract, signer, PopupType.MediumCaution);
                continue;
            }

            if (!_prototypeManager.TryIndex(clauseKey, out DevilClausePrototype? clause))
            {
                _sawmill.Warning($"Unknown contract clause: {clauseKey}");
                continue;
            }

            // no duplicates
            if (!processedClauses.Add(clauseKey))
            {
                _sawmill.Warning($"Attempted to apply duplicate clause: {clauseKey} on contract {ToPrettyString(contract)}");
                continue;
            }

            if (targetEntity is not null)
                ApplyEffectToTarget(targetEntity.Value, clause, contract);
            else
                _sawmill.Warning($"Invalid target entity for clause {clauseKey} in contract {ToPrettyString(contract)}");
        }
    }

    private void ApplyEffectToTarget(EntityUid target, DevilClausePrototype clause, Entity<DevilContractComponent>? contract)
    {
        //_sawmill.Debug($"Applying {clause.ID} effect to {ToPrettyString(target)}");

        if (TerminatingOrDeleted(target))
            return;

        DoPolymorphs(target, clause);

        RemoveComponents(target, clause);

        AddComponents(target, clause);

        ChangeDamageModifier(target, clause);

        AddImplants(target, clause);

        SpawnItems(target, clause);

        DoSpecialActions(target, contract, clause);
    }

    private void ChangeDamageModifier(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.DamageModifierSet == null)
            return;

        _damageable.SetDamageModifierSetId(target, clause.DamageModifierSet);
        // _sawmill.Debug($"Changed {ToPrettyString(target)} modifier set to {clause.DamageModifierSet}");
    }

    private void RemoveComponents(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.RemovedComponents == null)
            return;

        EntityManager.RemoveComponents(target, clause.RemovedComponents);

        //foreach (var component in clause.RemovedComponents)
        //    _sawmill.Debug($"Removed {component.Value.Component} from {ToPrettyString(target)}");
    }

    private void AddImplants(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.Implants == null)
            return;

        _implant.AddImplants(target, clause.Implants);

        //foreach (var implant in clause.Implants)
        //    _sawmill.Debug($"Added {implant} to {ToPrettyString(target)}");
    }

    private void AddComponents(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.AddedComponents == null)
            return;

        EntityManager.AddComponents(target, clause.AddedComponents, false);

        //foreach (var (name, data) in clause.AddedComponents)
        //    _sawmill.Debug($"Added {data.Component} to {ToPrettyString(target)}");
    }

    private void SpawnItems(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.SpawnedItems == null)
            return;

        foreach (var item in clause.SpawnedItems)
        {
            if (!_prototypeManager.TryIndex(item, out _))
                continue;

            var spawnedItem = SpawnNextToOrDrop(item, target);
            _hands.TryPickupAnyHand(target, spawnedItem, false, false, false);

            //_sawmill.Debug($"Spawned {item} for {ToPrettyString(target)}");
        }
    }

    private void DoPolymorphs(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.Polymorph == null)
            return;

        _polymorph.PolymorphEntity(target, clause.Polymorph.Value);
        //_sawmill.Debug($"Polymorphed {ToPrettyString(target)} to {clause.Polymorph} ");
    }

    private void DoSpecialActions(EntityUid target, Entity<DevilContractComponent>? contract, DevilClausePrototype clause)
    {
        if (clause.Event == null)
            return;

        var ev = clause.Event;
        ev.Target = target;

        if (contract is not null)
            ev.Contract = contract;

        // you gotta cast this shit to object, don't ask me vro idk either
        RaiseLocalEvent(target, (object)ev, true);
        //_sawmill.Debug($"Raising event: {(object)ev} on {ToPrettyString(target)}. ");
    }

    public void AddRandomNegativeClause(EntityUid target)
    {
        var negativeClauses = _prototypeManager.EnumeratePrototypes<DevilClausePrototype>()
            .Where(c => c.ClauseWeight >= 0)
            .ToList();

        if (negativeClauses.Count == 0)
            return;

        var selectedClause = _random.Pick(negativeClauses);
        ApplyEffectToTarget(target, selectedClause, null);

        _sawmill.Debug($"Selected {selectedClause.ID} effect for {ToPrettyString(target)}");
    }

    public void AddRandomNegativeClauseSlasher(EntityUid target)
    {
        var negativeClauses = _prototypeManager.EnumeratePrototypes<DevilClausePrototype>()
            .Where(c => c.ClauseWeight >= 0 && c.ID != "humanity")
            .ToList();

        if (negativeClauses.Count == 0)
            return;

        var selectedClause = _random.Pick(negativeClauses);
        ApplyEffectToTarget(target, selectedClause, null);

        _sawmill.Debug($"Selected {selectedClause.ID} effect for {ToPrettyString(target)}");
    }

    public void AddRandomPositiveClause(EntityUid target)
    {
        var positiveClauses = _prototypeManager.EnumeratePrototypes<DevilClausePrototype>()
            .Where(c => c.ClauseWeight <= 0)
            .ToList();

        if (positiveClauses.Count == 0)
            return;

        var selectedClause = _random.Pick(positiveClauses);
        ApplyEffectToTarget(target, selectedClause, null);

        _sawmill.Debug($"Selected {selectedClause.ID} effect for {ToPrettyString(target)}");
    }

    public void AddRandomClause(EntityUid target)
    {
        var clauses = _prototypeManager.EnumeratePrototypes<DevilClausePrototype>().ToList();

        if (clauses.Count == 0)
            return;

        var selectedClause = _random.Pick(clauses);
        ApplyEffectToTarget(target, selectedClause, null);

        _sawmill.Debug($"Selected {selectedClause.ID} effect for {ToPrettyString(target)}");
    }

    #endregion
}
