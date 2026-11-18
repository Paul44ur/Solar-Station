// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Devil.Systems;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Contract;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Goobstation.Server.Devil.Contract;

/// <summary>
/// Solar: the station Lawyer can read a signed devil contract and find a loophole, voiding one clause.
/// Each lawyer can do this once per round. Soul Ownership and clauses that can't be undone
/// (lost limbs, organs, items, transformations) can't be voided.
/// </summary>
public sealed partial class DevilContractSystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = null!;
    [Dependency] private DevilTitheSystem _tithe = null!;

    private const string LawyerJob = "Lawyer";
    private static readonly TimeSpan LoopholeTime = TimeSpan.FromSeconds(10);

    private void InitializeLoophole()
    {
        SubscribeLocalEvent<DevilContractComponent, GetVerbsEvent<Verb>>(OnGetLoopholeVerbs);
        SubscribeLocalEvent<DevilContractComponent, DevilLoopholeDoAfterEvent>(OnLoopholeDoAfter);
    }

    private void OnGetLoopholeVerbs(Entity<DevilContractComponent> contract, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || !contract.Comp.IsContractFullySigned
            || contract.Comp.Signer == null
            || HasComp<DevilLoopholeUsedComponent>(args.User)
            || !IsLawyer(args.User))
            return;

        var user = args.User;
        foreach (var clause in contract.Comp.CurrentClauses)
        {
            if (!CanVoidClause(clause) || contract.Comp.VoidedClauses.Contains(clause.ID))
                continue;

            var clauseId = clause.ID;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("devil-loophole-verb", ("clause", DevilClauseNames.GetName(clauseId))),
                Category = VerbCategory.Examine,
                Act = () => StartLoophole(contract, user, clauseId),
            });
        }
    }

    private void StartLoophole(Entity<DevilContractComponent> contract, EntityUid lawyer, string clauseId)
    {
        _popupSystem.PopupEntity(Loc.GetString("devil-loophole-start"), lawyer, lawyer);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, lawyer, LoopholeTime,
            new DevilLoopholeDoAfterEvent { Clause = clauseId }, contract, contract)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    private void OnLoopholeDoAfter(Entity<DevilContractComponent> contract, ref DevilLoopholeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var lawyer = args.User;
        if (contract.Comp.Signer is not { } signer
            || HasComp<DevilLoopholeUsedComponent>(lawyer)
            || contract.Comp.VoidedClauses.Contains(args.Clause)
            || !_prototypeManager.TryIndex(args.Clause, out DevilClausePrototype? clause)
            || !CanVoidClause(clause))
            return;

        // Undo what the clause did to the mortal.
        if (clause.AddedComponents != null)
            EntityManager.RemoveComponents(signer, clause.AddedComponents);

        if (clause.Event != null)
            _tithe.VoidTithe(signer, clause.Event);

        contract.Comp.VoidedClauses.Add(clause.ID);
        EnsureComp<DevilLoopholeUsedComponent>(lawyer);

        if (FindDeal(contract) is { } deal)
            deal.Voided.Add(DevilClauseNames.GetName(clause.ID));

        var name = DevilClauseNames.GetName(clause.ID);
        _popupSystem.PopupEntity(Loc.GetString("devil-loophole-success", ("clause", name), ("signer", Identity.Entity(signer, EntityManager))), lawyer, lawyer, PopupType.Medium);
        _popupSystem.PopupEntity(Loc.GetString("devil-loophole-freed", ("clause", name)), signer, signer, PopupType.Medium);
    }

    private bool IsLawyer(EntityUid uid)
    {
        return _mind.TryGetMind(uid, out var mindId, out _)
               && _jobs.MindTryGetJobId(mindId, out var job)
               && job == LawyerJob;
    }

    /// <summary>
    /// Only prices that can be cleanly undone: added traits, and the devil's tithes.
    /// </summary>
    private static bool CanVoidClause(DevilClausePrototype clause)
    {
        if (clause.ClauseWeight <= 0 || clause.ID == "soulownership")
            return false;

        if (clause.RemovedComponents != null
            || clause.Polymorph != null
            || clause.SpawnedItems != null
            || clause.Implants != null
            || clause.DamageModifierSet != null)
            return false;

        if (clause.Event != null)
            return clause.Event is DevilContractLifeForceEvent or DevilContractSwiftnessEvent or DevilContractMightEvent;

        return clause.AddedComponents != null;
    }
}

/// <summary>
/// Solar: this lawyer already used their one loophole this round.
/// </summary>
[RegisterComponent]
public sealed partial class DevilLoopholeUsedComponent : Component;
