// Adapted from PunishXIV/WrathCombo (BSD-3-Clause). See licenses/WrathCombo-BSD-3-Clause.txt.
using System;

namespace XIVComboExpandedestPlugin.Combos;

internal static partial class BST
{
    internal static (uint Instinctual, uint Clockwise, uint Counterclockwise) AxesForTrick(uint trick)
    {
        if (RampantTricks.Contains(trick))
            return (AvalancheAxe, MistralAxe, GaleAxe);
        if (DurantTricks.Contains(trick))
            return (MistralAxe, SpinningAxe, AvalancheAxe);
        if (EldritchTricks.Contains(trick))
            return (SpinningAxe, GaleAxe, MistralAxe);
        if (VolantTricks.Contains(trick))
            return (GaleAxe, AvalancheAxe, SpinningAxe);
        return default;
    }

    internal static RallyingType GetRallyFocus(BSTGauge gauge, bool rallyLearned, bool cheerLearned)
    {
        if (rallyLearned && !cheerLearned && gauge.MasterInstinct < 3)
            return RallyingType.Rally;
        if (rallyLearned && cheerLearned)
            return gauge.MasterInstinct == 3 && gauge.PetInstinct == 3 ? RallyingType.None :
                gauge.MasterInstinct < 2 || gauge.PetInstinct == 3 ? RallyingType.Rally : RallyingType.RallyingCheer;
        return RallyingType.None;
    }

    internal static bool MeetsTP(BSTGauge gauge, int threshold, bool inCombo) =>
        inCombo || (gauge.PlayerTP >= Math.Clamp(threshold, 100, 250) && gauge.BeastTP >= Math.Clamp(threshold, 100, 250));

    internal static uint SelectBasicAction(uint combo, float timer, Func<uint, bool> ready)
    {
        if (timer > 0 && combo == SmashAxe && ready(AxebladeBite))
            return AxebladeBite;
        if (timer > 0 && combo == AxebladeBite && ready(Shieldsplitter))
            return Shieldsplitter;
        return ready(SmashAxe) ? SmashAxe : 0;
    }
}
