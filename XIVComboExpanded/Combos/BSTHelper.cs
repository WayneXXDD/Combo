// Adapted from PunishXIV/WrathCombo (BSD-3-Clause). See licenses/WrathCombo-BSD-3-Clause.txt.
#pragma warning disable PendingExcelSchema // BST pet columns currently use Lumina's pending schema, as in WrathCombo.
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using ActionRow = Lumina.Excel.Sheets.Action;
using BNpcBase = Lumina.Excel.Sheets.Experimental.BNpcBase;
using Pet = Lumina.Excel.Sheets.Experimental.Pet;
using PetMirage = Lumina.Excel.Sheets.Experimental.PetMirage;
using XBMPet = Lumina.Excel.Sheets.Experimental.XBMPet;

namespace XIVComboExpandedestPlugin.Combos;

internal static partial class BST
{
    private static readonly Dictionary<uint, uint> PetModels = new();
    private static readonly Dictionary<uint, uint> PetTricks = new();
    private static readonly Dictionary<uint, uint> PetReleases = new();

    public static bool FinisherReady => FinisherActions.Count > 0;
    public static List<uint> FinisherActions => Service.BeastmasterState.Finisher.Actions;
    public static bool JustUsed(uint action) => Service.BeastmasterState.JustUsed(action);
    public static bool InCombat() => Service.Condition[ConditionFlag.InCombat];

    public static unsafe BSTGauge JobGauge
    {
        get
        {
            var manager = JobGaugeManager.Instance();
            if (manager == null || Service.ObjectTable.LocalPlayer?.ClassJob.RowId != JobID || manager->ClassJobId != JobID)
                return default;

            return *(BSTGauge*)((byte*)manager + 0x08);
        }
    }

    public static unsafe uint CurrentPetId
    {
        get
        {
            var ui = UIState.Instance();
            return ui == null || ui->Buddy.PetInfo.Pet == null ? 0u : (uint)ui->Buddy.PetInfo.Pet->DataId;
        }
    }

    public static bool CurrentPetIsBMPet => CurrentPetId != 0 && Service.DataManager.GetExcelSheet<XBMPet>().Any(x => x.Pet.RowId == CurrentPetId);
    public static uint CurrentPetTrickAction => PetAbility(CurrentPetId, 0);
    public static uint CurrentPetReleaseAction => PetAbility(CurrentPetId, 1);
    public static uint InstinctualComboAxe => AxesForTrick(CurrentPetTrickAction).Instinctual;
    public static uint ClockwiseInstinctualAction => AxesForTrick(CurrentPetTrickAction).Clockwise;
    public static uint CounterClockwiseInstinctualAction => AxesForTrick(CurrentPetTrickAction).Counterclockwise;

    public static bool InInstinctualCombo =>
        !(JobGauge.BeastTP < 100 && JobGauge.PlayerTP < 100) &&
        (HasPlayerEffect(Buffs.RampantHeart) || HasPlayerEffect(Buffs.DurantHeart) || HasPlayerEffect(Buffs.EldritchHeart) || HasPlayerEffect(Buffs.VolantHeart));

    public static bool CanStartInstinctualCombo => JobGauge.PlayerTP >= 100 && JobGauge.BeastTP >= 100;
    public static bool AbleToIntentional => ActionLearned(ClockwiseInstinctualAction) || ActionLearned(CounterClockwiseInstinctualAction);
    public static bool RallyLearnt => ActionLearned(Rally);
    public static bool RallyingCheerLearnt => ActionLearned(RallyingCheer);
    public static bool OnLastHorn => !ActionReady(FirstBattlehorn) && !ActionReady(SecondBattlehorn) && !ActionReady(ThirdBattlehorn);
    public static bool IsOriginal(uint action) => Service.IconReplacer.OriginalHook(action) == action;

    public static RallyingType RallyStackFocus => GetRallyFocus(JobGauge, RallyLearnt, RallyingCheerLearnt);

    public static unsafe bool PetUnlocked(uint petId)
    {
        var manager = XBMManager.Instance();
        return manager != null && petId != 0 && manager->IsPetUnlocked(petId);
    }

    public static bool TargetIsBstPet(IGameObject? target) => GetPetIdFromModel(target) != 0;

    public static uint GetPetIdFromModel(IGameObject? target)
    {
        if (target is not IBattleChara battle || !Service.DataManager.GetExcelSheet<BNpcBase>().TryGetRow(battle.BaseId, out var npc) || npc.Unknown10 != 5)
            return 0;

        if (PetModels.TryGetValue(battle.BaseId, out var cached))
            return cached;

        var model = npc.ModelChara.Value;
        var mirage = Service.DataManager.GetExcelSheet<PetMirage>().FirstOrDefault(x => x.ModelChara.Value.Model == model.Model && x.ModelChara.Value.Base == model.Base);
        if (mirage.RowId == 0)
            return PetModels[battle.BaseId] = 0;
        var pet = Service.DataManager.GetExcelSheet<Pet>().FirstOrDefault(x => x.AllowedPetMirage[0].RowId == mirage.RowId);
        return PetModels[battle.BaseId] = pet.RowId == 0 || pet.Unknown18 <= 0 ? 0u : (uint)pet.Unknown18;
    }

    public static bool HasTargetEffect(IGameObject? target, uint status) => target is IBattleChara battle && battle.StatusList.Any(x => x.StatusId == status);
    private static bool HasPlayerEffect(uint status) => HasTargetEffect(Service.ObjectTable.LocalPlayer, status);

    private static uint PetAbility(uint petId, int index)
    {
        var cache = index == 0 ? PetTricks : PetReleases;
        if (cache.TryGetValue(petId, out var cached))
            return cached;
        return cache[petId] = petId != 0 && Service.DataManager.GetExcelSheet<Pet>().TryGetRow(petId, out var pet) ? pet.Abilities[index].RowId : 0;
    }

    public static unsafe bool ActionLearned(uint action)
    {
        var manager = ActionManager.Instance();
        return action != 0 && manager != null &&
            Service.DataManager.GetExcelSheet<ActionRow>().TryGetRow(action, out var row) &&
            Service.ObjectTable.LocalPlayer?.Level >= row.ClassJobLevel &&
            manager->GetActionStatus(ActionType.Action, action, checkRecastActive: false, checkCastingActive: false) != 573;
    }

    public static unsafe bool ActionReady(uint action)
    {
        var manager = ActionManager.Instance();
        if (manager == null || !ActionLearned(action))
            return false;

        var status = manager->GetActionStatus(ActionType.Action, action, checkRecastActive: false, checkCastingActive: false);
        if (status is not (0 or 580 or 582))
            return false;

        var row = Service.DataManager.GetExcelSheet<ActionRow>().GetRow(action);
        return manager->GetCurrentCharges(action) > 0 ||
            (row.ActionCategory.RowId != 4 && RemainingCooldown(action) <= RemainingGCD + 0.5f);
    }

    public static unsafe uint GetRemainingCharges(uint action) => ActionManager.Instance() == null ? 0 : ActionManager.Instance()->GetCurrentCharges(action);

    public static unsafe float RemainingGCD
    {
        get
        {
            var manager = ActionManager.Instance();
            var detail = manager == null ? null : manager->GetRecastGroupDetail(57);
            return detail == null || !detail->IsActive ? 0 : Math.Max(0, detail->Total - detail->Elapsed);
        }
    }

    private static unsafe float RemainingCooldown(uint action)
    {
        var manager = ActionManager.Instance();
        return manager == null ? float.MaxValue : Math.Max(0, manager->GetRecastTime(ActionType.Action, action) - manager->GetRecastTimeElapsed(ActionType.Action, action));
    }

    public static unsafe bool CanWeave()
    {
        var player = Service.ObjectTable.LocalPlayer;
        var manager = ActionManager.Instance();
        if (player == null || manager == null)
            return false;
        var casting = Math.Max(0, player.TotalCastTime - player.CurrentCastTime);
        return manager->AnimationLock <= 0.6f && casting <= 0.5f && RemainingGCD > casting + 0.6f + manager->AnimationLock;
    }

    public static bool InMeleeRange()
    {
        var player = Service.ObjectTable.LocalPlayer;
        var target = Service.TargetManager.Target;
        return player != null && target != null && System.Numerics.Vector3.Distance(player.Position, target.Position) - player.HitboxRadius - target.HitboxRadius <= 3 + Service.Configuration.MeleeOffset;
    }

    public static bool TraitLevelChecked(uint trait) => Service.DataManager.GetExcelSheet<Trait>().TryGetRow(trait, out var row) && Service.ObjectTable.LocalPlayer?.Level >= row.Level;

    public static unsafe bool BasicCombo(out uint action)
    {
        var manager = ActionManager.Instance();
        action = manager == null ? 0 : SelectBasicAction(manager->Combo.Action, manager->Combo.Timer, ActionReady);
        return action != 0;
    }
}
