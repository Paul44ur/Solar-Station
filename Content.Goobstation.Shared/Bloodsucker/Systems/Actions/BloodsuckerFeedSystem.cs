using Content.Goobstation.Common.Grab;
using Content.Goobstation.Common.Religion;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.Bloodsuckers.Components;
using Content.Goobstation.Shared.Bloodsuckers.Components.Actions;
using Content.Goobstation.Shared.Bloodsuckers.Components.Vassals;
using Content.Goobstation.Shared.Bloodsuckers.Events;
using Content.Goobstation.Shared.GrabIntent;
using Content.Shared._Starlight.VentCrawling;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Bloodsuckers.Systems;

/// <summary>
/// Drains blood from a target.
/// </summary>
public sealed partial class BloodsuckerFeedSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private BloodsuckerHumanitySystem _humanity = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    // TO DO: BloodsuckerMasqueradeSystem

    private EntityQuery<PullerComponent> _pullerQuery;

    private static readonly EntProtoId ForcedSleepEffect = "StatusEffectForcedSleeping";

    public override void Initialize()
    {
        base.Initialize();

        _pullerQuery = GetEntityQuery<PullerComponent>();
        SubscribeLocalEvent<BloodsuckerComponent, BloodsuckerFeedEvent>(OnFeedStart);
        SubscribeLocalEvent<BloodsuckerComponent, BloodsuckerFeedDoAfterEvent>(OnFeedDoAfter);
    }

    private void OnFeedStart(Entity<BloodsuckerComponent> ent, ref BloodsuckerFeedEvent args)
    {
        if (!TryComp(ent, out BloodsuckerFeedComponent? comp))
            return;

        var target = args.Target;

        if (target == EntityUid.Invalid || target == ent.Owner)
            return;

        if (!InRange(ent.Owner, target))
            return;

        // eating rats and stuff makes the vamp recoil and kills the little fella instantly.
        if (IsSmallAnimal(target))
        {
            _popup.PopupPredicted(Loc.GetString("bloodsucker-feed-mouse"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            _bloodstream.TryModifyBloodLevel(
                new Entity<BloodstreamComponent?>(ent.Owner, null),
                FixedPoint2.New(25));
            QueueDel(target);
            return;
        }

        if (!TryUseCosts(ent, comp))
            return;

        var feeding = EnsureComp<BloodsuckerFeedingComponent>(ent);
        feeding.Target = target;

        // visible only to vamp
        _popup.PopupPredicted(
            Loc.GetString("bloodsucker-feed-starting", ("target", target)),
            ent.Owner, ent.Owner, PopupType.Small);

        StartDoAfter(ent, target, comp.StartDelay);
    }

    private void OnFeedDoAfter(Entity<BloodsuckerComponent> ent, ref BloodsuckerFeedDoAfterEvent args)
    {
        if (!TryComp(ent, out BloodsuckerFeedingComponent? feeding))
            return;

        var target = feeding.Target;
        if (target == EntityUid.Invalid || !Exists(target))
            return;

        if (!TryComp(ent, out BloodsuckerFeedComponent? comp))
            return;

        // Interrupted
        if (args.Cancelled || args.Handled)
        {
            if (!feeding.Silent)
            {
                // Visible to everyone nearby
                _popup.PopupPredicted(
                    Loc.GetString("bloodsucker-feed-interrupt-others",
                        ("user", ent.Owner),
                        ("target", target),
                        ("target_their", Loc.GetString("their", ("ent", target)))),
                    Loc.GetString("bloodsucker-feed-interrupt-user",
                        ("target", target),
                        ("target_their", Loc.GetString("their", ("ent", target)))),
                    ent.Owner, ent.Owner, PopupType.LargeCaution);

                if (TryComp(target, out BloodstreamComponent? targetBloodstream))
                {
                    var spillAmount = comp.BloodDrainAmount;

                    _bloodstream.TryModifyBloodLevel(
                        new Entity<BloodstreamComponent?>(target, targetBloodstream),
                        -spillAmount);
                }
            }

            CleanupFeeding(ent);
            return;
        }

        if (!InRange(ent.Owner, target))
        {
            // Fell out of range without a hard cancel which gets treated as interrupt.
            if (!feeding.Silent)
            {
                _popup.PopupPredicted(
                    Loc.GetString("bloodsucker-feed-interrupt-others",
                        ("user", ent.Owner), ("target", target)),
                    Loc.GetString("bloodsucker-feed-interrupt-user",
                        ("target", target)),
                    ent.Owner, ent.Owner, PopupType.LargeCaution);
            }

            CleanupFeeding(ent);
            return;
        }

        // play the bite animation as well as message and check for witnesses
        if (!feeding.HasBitten)
        {
            feeding.HasBitten = true;
            PlayBiteMessage(ent, target, feeding, comp);
            CheckMasquerade(ent, target);
        }

        PerformDrain(ent, target, comp);

        CheckVictimBloodWarnings(ent, target, comp, feeding);

        StartDoAfter(ent, target, comp.SipDelay);
    }

    private void PlayBiteMessage(
        Entity<BloodsuckerComponent> ent,
        EntityUid target,
        BloodsuckerFeedingComponent feeding,
        BloodsuckerFeedComponent comp)
    {
        var isAggressiveGrab = _pullerQuery.TryComp(ent.Owner, out var puller)
                               && puller.Pulling == target
                               && TryComp(target, out GrabbableComponent? grabbable)
                               && grabbable.GrabStage >= GrabStage.Hard;

        if (isAggressiveGrab)
        {
            // aggressive grab puts target to sleep.
            feeding.Silent = false;

            _popup.PopupPredicted(
                 Loc.GetString("bloodsucker-feed-start-neck-others",
                     ("user", ent.Owner),
                     ("target", target),
                     ("user_their", Loc.GetString("their", ("ent", ent.Owner)))),
                 Loc.GetString("bloodsucker-feed-start-neck-user",
                     ("target", target)),
                 ent.Owner, ent.Owner, PopupType.LargeCaution);

            _status.TryAddStatusEffect(
                target, ForcedSleepEffect, out _,
                TimeSpan.FromSeconds(comp.SleepDuration));
        }
        else
        {
            feeding.Silent = true;

            var dazedSuffix = IsAlive(target)
                ? " " + Loc.GetString("bloodsucker-feed-dazed", ("target", target))
                : string.Empty;

            // pop up to any looky loos
            foreach (var watcher in GetNearbyLiving(ent.Owner, comp.FeedNoticeRange))
            {
                if (watcher == ent.Owner || watcher == target)
                    continue;
                _popup.PopupPredicted(
                    Loc.GetString("bloodsucker-feed-start-wrist-others",
                        ("user", ent.Owner),
                        ("target", target),
                        ("user_their", Loc.GetString("their", ("ent", ent.Owner)))),
                    watcher, watcher, PopupType.Small);
            }

            // vamp only
            _popup.PopupPredicted(
                Loc.GetString("bloodsucker-feed-start-wrist-user",
                    ("target", target)) + dazedSuffix,
                ent.Owner, ent.Owner, PopupType.Small);
        }
    }

    private void CheckMasquerade(Entity<BloodsuckerComponent> ent, EntityUid target)
    {
        if (!TryComp(ent, out BloodsuckerFeedComponent? comp))
            return;

        foreach (var watcher in GetNearbyLiving(ent.Owner, comp.FeedNoticeRange))
        {
            if (watcher == ent.Owner || watcher == target)
                continue;

            // Skip other bloodsuckers, vassals, chaplains, and blind mobs.
            if (HasComp<BloodsuckerComponent>(watcher))
                continue;
            if (HasComp<BibleUserComponent>(watcher))
                continue;
            if (HasComp<PermanentBlindnessComponent>(watcher))
                continue;
            if (HasComp<BloodsuckerVassalComponent>(watcher))
                continue;

            _popup.PopupPredicted(
                Loc.GetString("bloodsucker-feed-noticed"),
                ent.Owner, ent.Owner, PopupType.SmallCaution);

            // TO DO: _masquerade.GiveInfraction(ent.Owner);
            break; // one infraction per feed
        }
    }

    private void CheckVictimBloodWarnings(
        Entity<BloodsuckerComponent> ent,
        EntityUid target,
        BloodsuckerFeedComponent comp,
        BloodsuckerFeedingComponent feeding)
    {
        if (!TryComp(target, out BloodstreamComponent? bs))
            return;

        if (bs.BloodSolution is not { } sol)
            return;

        var current = (float) sol.Comp.Solution.Volume;
        var max = (float) sol.Comp.Solution.MaxVolume;
        if (max <= 0f)
            return;

        var fraction = current / max;

        // Warn only when we cross into a new (lower) threshold band,
        if (fraction <= comp.BloodWarningFatal && feeding.LastWarnedBloodFraction > comp.BloodWarningFatal)
        {
            _popup.PopupPredicted(
                Loc.GetString("bloodsucker-feed-warning-fatal"),
                ent.Owner, ent.Owner, PopupType.LargeCaution);
        }
        else if (fraction <= comp.BloodWarningDanger && feeding.LastWarnedBloodFraction > comp.BloodWarningDanger)
        {
            _popup.PopupPredicted(
                Loc.GetString("bloodsucker-feed-warning-danger"),
                ent.Owner, ent.Owner, PopupType.MediumCaution);
        }
        else if (fraction <= comp.BloodWarningSafe && feeding.LastWarnedBloodFraction > comp.BloodWarningSafe)
        {
            _popup.PopupPredicted(
                Loc.GetString("bloodsucker-feed-warning-safe"),
                ent.Owner, ent.Owner, PopupType.SmallCaution);
        }

        feeding.LastWarnedBloodFraction = fraction;
    }

    private void StopFeeding(Entity<BloodsuckerComponent> ent, EntityUid target)
    {
        _popup.PopupPredicted(
            Loc.GetString("bloodsucker-feed-stop", ("target", target)),
            ent.Owner, ent.Owner, PopupType.Small);

        CleanupFeeding(ent);
    }

    private void CleanupFeeding(Entity<BloodsuckerComponent> ent)
    {
        RemCompDeferred<BloodsuckerFeedingComponent>(ent);
    }

    private void PerformDrain(Entity<BloodsuckerComponent> ent, EntityUid target, BloodsuckerFeedComponent comp)
    {
        float amount = comp.BloodDrainAmount;
        if (TryComp(ent, out BloodsuckerFrenzyComponent? frenzy))
            amount *= frenzy.FeedMultiplier;

        if (!TryComp(target, out BloodstreamComponent? targetBloodstream))
            return;

        if (!TryComp(ent.Owner, out BloodstreamComponent? vampireBloodstream))
            return;

        // Resolve the target's blood solution
        if (!_bloodstream.SolutionContainer.ResolveSolution(target, targetBloodstream.BloodSolutionName,
                ref targetBloodstream.BloodSolution, out var targetBloodSolution))
            return;

        // Clamp to what's actually available
        var actualAmount = FixedPoint2.New(MathF.Min(amount, (float) targetBloodSolution.Volume));
        if (actualAmount <= FixedPoint2.Zero)
            return;

        // Pull the blood out (shit method name btw)
        var stolen = _bloodstream.SolutionContainer.SplitSolution(
            targetBloodstream.BloodSolution!.Value,
            actualAmount);

        // Resolve vampire's blood solution and expand max volume if needed
        if (!_bloodstream.SolutionContainer.ResolveSolution(ent.Owner, vampireBloodstream.BloodSolutionName,
                ref vampireBloodstream.BloodSolution, out var vampBloodSolution))
            return;

        if (vampBloodSolution.Volume + actualAmount > vampBloodSolution.MaxVolume)
        {
            vampBloodSolution.MaxVolume = vampBloodSolution.Volume + actualAmount;
            _bloodstream.SolutionContainer.UpdateChemicals(vampireBloodstream.BloodSolution!.Value);
        }

        // Solar: the victim's blood carries their DNA, so the vampire's metabolism doesn't treat it as its own
        // blood and digests it like food (UncookedAnimalProteins -> vomiting). Swap it for the vampire's own blood
        // reagents and keep any other chemicals the victim had in their bloodstream.
        var bloodTaken = FixedPoint2.Zero;
        foreach (var quantity in stolen.Contents.ToArray())
        {
            if (!IsBloodReagent(quantity.Reagent.Prototype, targetBloodstream)
                && !IsBloodReagent(quantity.Reagent.Prototype, vampireBloodstream))
                continue;

            stolen.RemoveReagent(quantity.Reagent, quantity.Quantity);
            bloodTaken += quantity.Quantity;
        }

        var referenceVolume = vampireBloodstream.BloodReferenceSolution.Volume;
        if (bloodTaken > FixedPoint2.Zero && referenceVolume > FixedPoint2.Zero)
        {
            foreach (var (reagent, quantity) in vampireBloodstream.BloodReferenceSolution)
                stolen.AddReagent(reagent, bloodTaken * quantity / referenceVolume);
        }

        // Pour stolen blood into vampire
        _bloodstream.SolutionContainer.TryAddSolution(vampireBloodstream.BloodSolution!.Value, stolen);

        if (bloodTaken > FixedPoint2.Zero)
        {
            var drankEv = new BloodsuckerDrankBloodEvent(bloodTaken.Float());
            RaiseLocalEvent(ent, ref drankEv);
        }

        _audio.PlayPredicted(comp.DrinkSound, ent, ent);
    }

    private void StartDoAfter(Entity<BloodsuckerComponent> ent, EntityUid target, float delay)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            ent.Owner,
            delay,
            new BloodsuckerFeedDoAfterEvent(),
            ent.Owner,
            target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private bool TryUseCosts(Entity<BloodsuckerComponent> ent, BloodsuckerFeedComponent comp)
    {
        if (comp.DisabledInFrenzy && HasComp<BloodsuckerFrenzyComponent>(ent))
            return false;

        if (comp.BloodCost > 0f)
        {
            if (!TryComp(ent.Owner, out BloodstreamComponent? bloodstream))
                return false;

            var currentVolume = bloodstream.BloodSolution is { } sol
                ? (float) sol.Comp.Solution.Volume
                : 0f;
            if (currentVolume < comp.BloodCost)
                return false;

            _bloodstream.TryModifyBloodLevel(
                new Entity<BloodstreamComponent?>(ent.Owner, bloodstream),
                FixedPoint2.New(-comp.BloodCost));
        }

        if (comp.HumanityCost != 0f && TryComp(ent, out BloodsuckerHumanityComponent? humanity))
            _humanity.ChangeHumanity(
                new Entity<BloodsuckerHumanityComponent>(ent.Owner, humanity),
                -comp.HumanityCost);

        return true;
    }

    #region Helpers
    private static bool IsBloodReagent(string prototype, BloodstreamComponent bloodstream)
    {
        foreach (var (reagent, _) in bloodstream.BloodReferenceSolution)
        {
            if (reagent.Prototype == prototype)
                return true;
        }

        return false;
    }

    private bool InRange(EntityUid a, EntityUid b)
    {
        if (!TryComp(a, out TransformComponent? ta) ||
            !TryComp(b, out TransformComponent? tb))
            return false;

        if (ta.MapID != tb.MapID)
            return false;

        var delta = ta.WorldPosition - tb.WorldPosition;
        return MathF.Abs(delta.X) <= 1.5f && MathF.Abs(delta.Y) <= 1.5f;
    }

    // Returns all living mobs within range.
    private IEnumerable<EntityUid> GetNearbyLiving(EntityUid origin, float range)
    {
        var coords = Transform(origin).Coordinates;
        foreach (var nearby in _lookup.GetEntitiesInRange<MobStateComponent>(coords, range))
        {
            if (nearby.Comp.CurrentState == MobState.Dead)
                continue;
            yield return nearby.Owner;
        }
    }

    // If its small creatures rather than humans and etc the vampire finds it nasty.
    private bool IsSmallAnimal(EntityUid uid)
    {
        // TO DO: Change this so that you can actually make a list of animals that taste bad. Bandaid solution.
        return HasComp<VentCrawlerComponent>(uid);
    }

    // Returns true if the target is alive. Self explanatory.
    private bool IsAlive(EntityUid uid)
    {
        return TryComp(uid, out MobStateComponent? state)
               && state.CurrentState != MobState.Dead;
    }

    #endregion
}
