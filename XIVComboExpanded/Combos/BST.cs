// Adapted from PunishXIV/WrathCombo (BSD-3-Clause). See licenses/WrathCombo-BSD-3-Clause.txt.
using FFXIVClientStructs.FFXIV.Client.Game;

namespace XIVComboExpandedestPlugin.Combos;

internal static partial class BST
{
    internal class BeastmasterSimpleMode : CustomCombo
    {
        protected override CustomComboPreset Preset => CustomComboPreset.BeastmasterSimpleMode;
        protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
        {
            if (actionID != SmashAxe)
                return actionID;

            if (!CurrentPetIsBMPet && InCombat() && !JustUsed(FirstBattlehorn) && !JustUsed(SecondBattlehorn) && !JustUsed(ThirdBattlehorn) && Service.ObjectTable.LocalPlayer is { IsCasting: false })
            {
                if (ActionReady(FirstBattlehorn))
                    return FirstBattlehorn;

                if (ActionReady(SecondBattlehorn))
                    return SecondBattlehorn;

                if (ActionReady(ThirdBattlehorn))
                    return ThirdBattlehorn;
            }

            if (TargetIsBstPet(CurrentTarget) && !PetUnlocked(GetPetIdFromModel(CurrentTarget)) && !HasTargetEffect(CurrentTarget, Debuffs.InterestCaptured))
            {
                if (ActionReady(Capture))
                    return Capture;
            }

            if (FinisherReady && FinisherActions.Count > 0)
            {
                if (FinisherActions[0] == CounterClockwiseInstinctualAction && !InInstinctualCombo)
                    return BlockedAction;

                if (FinisherActions[0] == Rally && JobGauge.MasterInstinct != 3)
                    return BlockedAction;

                return FinisherActions[0];
            }

            // Intentional > Instinctual
            if (AbleToIntentional)
            {
                if (JobGauge.ActiveAffinity is InstinctualAffinity.Moonstalker)
                {
                    if (ActionReady(RisenFall))
                        return RisenFall;
                }

                if (JobGauge.ActiveAffinity is InstinctualAffinity.Sunstrider)
                {
                    if (ActionReady(Calamity))
                        return Calamity;
                }

                if (CanStartInstinctualCombo || InInstinctualCombo)
                {
                    bool cantOmniDirection = !ActionLearned(ClockwiseInstinctualAction) || !ActionLearned(CounterClockwiseInstinctualAction);
                    if (RallyStackFocus is RallyingType.None or RallyingType.Rally || (cantOmniDirection && !ActionLearned(CounterClockwiseInstinctualAction))) // Prioritize our stacks
                    {
                        if (ActionReady(Trick))
                            return Trick;

                        if (ActionReady(ClockwiseInstinctualAction))
                            return ClockwiseInstinctualAction;
                    }
                    else
                    {
                        if (ActionReady(CounterClockwiseInstinctualAction))
                            return CounterClockwiseInstinctualAction;

                        if (ActionReady(Trick))
                            return Trick;
                    }
                }
            }
            else if (ActionLearned(InstinctualComboAxe))
            {
                // Fallback to instinctual
                if (RallyStackFocus is RallyingType.None or RallyingType.Rally) // Prioritize our stacks
                {
                    if (ActionReady(Trick))
                        return Trick;

                    if (ActionReady(InstinctualComboAxe))
                        return InstinctualComboAxe;
                }
                else
                {
                    if (ActionReady(InstinctualComboAxe))
                        return InstinctualComboAxe;

                    if (ActionReady(Trick))
                        return Trick;
                }

            }
            else if (InstinctualComboAxe == 0) // If somehow you're at level 4-7 with a pet that isn't Rampant
            {
                if (ActionReady(AvalancheAxe))
                    return AvalancheAxe;
            }

            if (ActionReady(TemperedRelease) && CanWeave() && CurrentPetReleaseAction != TemperedReleaseActions.WespeFinalSting)
                return TemperedRelease;

            if (Service.Configuration.BeastmasterCycleBeasts && TraitLevelChecked(Traits.WildHeartII) && !ActionReady(TemperedRelease) && ActionReady(PartingBlow) && !OnLastHorn && CanWeave())
                return PartingBlow;

            if (CanWeave() && InMeleeRange() && ActionReady(ShieldCharge) && GetRemainingCharges(ShieldCharge) > 1) // Save one for manual use
                return ShieldCharge;

            if (BasicCombo(out var basic))
                return basic;

            return OriginalHook(SmashAxe);
        }
    }

    internal class BeastmasterBasicCombo : CustomCombo
    {
        protected override CustomComboPreset Preset => CustomComboPreset.BeastmasterBasicCombo;

        protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
        {
            if (actionID is not AxebladeBite)
                return actionID;

            if (BasicCombo(out var basic))
                return basic;

            return actionID;
        }
    }

    internal class BeastmasterInstinctualCombo : CustomCombo
    {
        protected override CustomComboPreset Preset => CustomComboPreset.BeastmasterInstinctualCombo;
        protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
        {
            if (actionID is not (AvalancheAxe or MistralAxe or SpinningAxe or GaleAxe))
                return actionID;

            if (!MeetsTP(JobGauge, Service.Configuration.BeastmasterInstinctualTP, InInstinctualCombo))
                return BlockedAction;

            if (RallyStackFocus is RallyingType.None or RallyingType.Rally) // Prioritize our stacks
            {
                if (ActionReady(Trick))
                    return Trick;

                if (ActionReady(InstinctualComboAxe))
                    return InstinctualComboAxe;
            }
            else
            {
                if (ActionReady(InstinctualComboAxe))
                    return InstinctualComboAxe;

                if (ActionReady(Trick))
                    return Trick;
            }

            return actionID;
        }
    }

    internal class BeastmasterIntentionalCombo : CustomCombo
    {
        protected override CustomComboPreset Preset => CustomComboPreset.BeastmasterIntentionalCombo;
        protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
        {
            if (actionID is not Trick)
                return actionID;

            if (Service.Configuration.BeastmasterInfinitiveCombo)
            {
                if (FinisherReady && FinisherActions.Count > 0)
                {
                    if (FinisherActions[0] == CounterClockwiseInstinctualAction && !InInstinctualCombo)
                        return BlockedAction;

                    return FinisherActions[0];
                }

                if (JobGauge.ActiveAffinity is InstinctualAffinity.Moonstalker)
                {
                    if (ActionReady(RisenFall))
                        return RisenFall;
                }

                if (JobGauge.ActiveAffinity is InstinctualAffinity.Sunstrider)
                {
                    if (ActionReady(Calamity))
                        return Calamity;
                }
            }

            if (!MeetsTP(JobGauge, Service.Configuration.BeastmasterIntentionalTP, InInstinctualCombo))
                return BlockedAction;

            if (RallyStackFocus is RallyingType.None or RallyingType.Rally) // Prioritize our stacks
            {
                if (ActionReady(Trick))
                    return Trick;

                if (ActionReady(ClockwiseInstinctualAction))
                    return ClockwiseInstinctualAction;
            }
            else
            {
                if (ActionReady(CounterClockwiseInstinctualAction))
                    return CounterClockwiseInstinctualAction;

                if (ActionReady(Trick))
                    return Trick;
            }

            return actionID;
        }
    }

    internal class BeastmasterCaptureHelper : CustomCombo
    {
        protected override CustomComboPreset Preset => CustomComboPreset.BeastmasterCaptureHelper;
        protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
        {
            if (actionID is not Capture)
                return actionID;

            var petId = GetPetIdFromModel(CurrentTarget);
            if (petId != 0)
            {
                if (!PetUnlocked(petId))
                    return Capture;
            }

            return BlockedAction;
        }
    }

    internal class BeastmasterBattlehornLockout : CustomCombo
    {
        protected override CustomComboPreset Preset => CustomComboPreset.BeastmasterBattlehornLockout;
        protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
        {
            if (actionID is not (FirstBattlehorn or SecondBattlehorn or ThirdBattlehorn))
                return actionID;

            if (InCombat() && JobGauge.BattleHorn != 0)
                return BlockedAction;

            return actionID;
        }
    }

    internal class BeastmasterBorrowFeature : CustomCombo
    {
        protected override CustomComboPreset Preset => CustomComboPreset.BeastmasterBorrowFeature;
        protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
        {
            if (actionID is not Borrow)
                return actionID;

            bool canSwitch = Service.Configuration.BeastmasterBorrowCurrentHornOnly && JobGauge.KinshipBattlehorn != JobGauge.BattleHorn;

            if (!IsOriginal(BeastMode) && !canSwitch)
                return OriginalHook(BeastMode);

            return actionID;
        }
    }
}
