using System.Runtime.InteropServices;
using XIVComboExpandedestPlugin.Combos;

var assertions = 0;
void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException(name);
    assertions++;
}

var ready = new HashSet<uint> { BST.SmashAxe, BST.AxebladeBite, BST.Shieldsplitter };
Check(BST.SelectBasicAction(0, 0, ready.Contains) == BST.SmashAxe, "Basic combo start");
Check(BST.SelectBasicAction(BST.SmashAxe, 10, ready.Contains) == BST.AxebladeBite, "Basic combo second hit");
Check(BST.SelectBasicAction(BST.AxebladeBite, 10, ready.Contains) == BST.Shieldsplitter, "Basic combo third hit");
Check(BST.SelectBasicAction(BST.AxebladeBite, 0, ready.Contains) == BST.SmashAxe, "Expired combo resets");
ready.Remove(BST.Shieldsplitter);
Check(BST.SelectBasicAction(BST.AxebladeBite, 10, ready.Contains) == BST.SmashAxe, "Level sync excludes locked finisher");
Check(BST.SelectBasicAction(BST.SmashAxe, 10, _ => false) == 0, "No usable action preserves caller fallback");

Check(BST.AxesForTrick(BST.TrickActions.CusithRake) == (BST.AvalancheAxe, BST.MistralAxe, BST.GaleAxe), "Rampant axe mapping");
Check(BST.AxesForTrick(BST.TrickActions.PugilScrewdriver) == (BST.MistralAxe, BST.SpinningAxe, BST.AvalancheAxe), "Durant axe mapping");
Check(BST.AxesForTrick(BST.TrickActions.DodoFowlStench) == (BST.SpinningAxe, BST.GaleAxe, BST.MistralAxe), "Eldritch axe mapping");
Check(BST.AxesForTrick(BST.TrickActions.WespeSharpSting) == (BST.GaleAxe, BST.AvalancheAxe, BST.SpinningAxe), "Volant axe mapping");
Check(BST.AxesForTrick(0) == default, "Unknown pet has no axe mapping");

var gauge = new BSTGauge { PlayerTP = 100, BeastTP = 100, Kinship = 0x82, Instinct = 0x09 };
Check(gauge.MasterInstinct == 2 && gauge.PetInstinct == 1 && gauge.KinshipBattlehorn == 2, "Gauge bitfield decoding");
Check(Marshal.OffsetOf<BSTGauge>(nameof(BSTGauge.Instinct)).ToInt32() == 0x10 && Marshal.SizeOf<BSTGauge>() >= 0x11, "Gauge layout contains Instinct");
Check(BST.MeetsTP(gauge, 100, false), "Both TP gauges meet threshold");
Check(!BST.MeetsTP(gauge, 200, false), "Higher configured TP threshold");
gauge.BeastTP = 99;
Check(!BST.MeetsTP(gauge, 100, false), "Beast TP alone blocks start");
Check(BST.MeetsTP(gauge, 200, true), "An ongoing combo bypasses start threshold");
Check(!BST.MeetsTP(gauge, -1, false), "Invalid TP setting clamps to minimum");
Check(BST.GetRallyFocus(gauge, true, true) == BST.RallyingType.RallyingCheer, "Focus pet stacks after two master stacks");
gauge.Instinct = 0x0F;
Check(BST.GetRallyFocus(gauge, true, true) == BST.RallyingType.None, "Both stacks capped");
gauge.Instinct = 0;
Check(BST.GetRallyFocus(gauge, true, false) == BST.RallyingType.Rally, "Early level Rally focus");

var finisher = new BeastmasterFinisher();
finisher.Update(0, true, 0);
Check(finisher.Actions.Count == 0, "Unknown pet cannot begin finisher");
finisher.Update(0, true, BST.GaleAxe);
Check(finisher.Actions.SequenceEqual(new[] { BST.Trick, BST.GaleAxe, BST.Rally, BST.RallyingCheer, BST.Trick }), "Finisher action order");
finisher.Observe(BST.SmashAxe, 10);
Check(finisher.Actions.Count == 5, "Unrelated action does not advance finisher");
finisher.Observe(BST.Trick, 20);
finisher.Observe(BST.Trick, 21);
Check(finisher.Actions.Count == 4 && finisher.Actions[0] == BST.GaleAxe, "Duplicate first action does not skip next step");
finisher.Update(10_021, false, BST.GaleAxe);
Check(finisher.Actions.Count == 0, "Finisher timeout clears stale sequence");
finisher.Update(20_000, true, BST.GaleAxe);
foreach (var action in new[] { BST.Trick, BST.GaleAxe, BST.Rally, BST.RallyingCheer, BST.Trick })
    finisher.Observe(action, 20_100);
Check(finisher.Actions.Count == 0, "Completed finisher clears sequence");
finisher.Update(30_000, true, BST.GaleAxe);
finisher.Reset();
Check(finisher.Actions.Count == 0, "Job or pet change can reset sequence");

Console.WriteLine($"Beastmaster: {assertions} assertions passed.");
