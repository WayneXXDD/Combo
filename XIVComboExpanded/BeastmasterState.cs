using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using XIVComboExpandedestPlugin.Combos;

namespace XIVComboExpandedestPlugin;

/// <summary>Tracks completed BST actions without sending actions or changing targets.</summary>
internal sealed unsafe class BeastmasterState : IDisposable
{
    private readonly Hook<ActionEffectHandler.Delegates.Receive> receiveHook;
    private readonly Dictionary<uint, long> timestamps = new();
    private uint petId;
    private byte horn;
    private uint lastSequence;
    private bool hasSequence;

    internal BeastmasterFinisher Finisher { get; } = new();

    internal BeastmasterState()
    {
        this.receiveHook = Service.GameInteropProvider.HookFromAddress<ActionEffectHandler.Delegates.Receive>(
            (nint)ActionEffectHandler.Addresses.Receive.Value, this.Receive);
        XIVComboExpandedestPlugin.Framework.Update += this.Update;
    }

    internal bool JustUsed(uint action) => this.timestamps.TryGetValue(action, out var when) && Environment.TickCount64 - when <= 3000;

    public void Dispose()
    {
        XIVComboExpandedestPlugin.Framework.Update -= this.Update;
        this.receiveHook.Dispose();
        this.Reset();
    }

    private void Reset()
    {
        this.Finisher.Reset();
        this.timestamps.Clear();
        this.hasSequence = false;
        this.petId = 0;
        this.horn = 0;
    }

    private void Update(IFramework framework)
    {
        try
        {
            var enabled = Service.ObjectTable.LocalPlayer?.ClassJob.RowId == BST.JobID &&
                (Service.Configuration.IsEnabled(CustomComboPreset.BeastmasterSimpleMode) ||
                (Service.Configuration.IsEnabled(CustomComboPreset.BeastmasterIntentionalCombo) && Service.Configuration.BeastmasterInfinitiveCombo));

            if (!enabled)
            {
                if (this.receiveHook.IsEnabled)
                    this.receiveHook.Disable();
                this.Reset();
                return;
            }

            if (!this.receiveHook.IsEnabled)
                this.receiveHook.Enable();

            var currentPet = BST.CurrentPetId;
            var gauge = BST.JobGauge;
            if (currentPet != this.petId || gauge.BattleHorn != this.horn)
            {
                this.Finisher.Reset();
                this.petId = currentPet;
                this.horn = gauge.BattleHorn;
            }

            if (!BST.InCombat())
            {
                this.Finisher.Reset();
                return;
            }

            this.Finisher.Update(Environment.TickCount64,
                gauge.MasterInstinct >= 2 && gauge.PetInstinct >= 1 && gauge.PlayerTP >= 100 && gauge.BeastTP >= 100 &&
                BST.ActionReady(BST.Rally) && BST.ActionReady(BST.RallyingCheer), BST.CounterClockwiseInstinctualAction);
        }
        catch (Exception ex)
        {
            this.Finisher.Reset();
            Service.PluginLog.Error(ex, "Unable to update Beastmaster combo state");
        }
    }

    private void Receive(uint casterEntityId, Character* caster, Vector3* targetPosition, ActionEffectHandler.Header* header,
        ActionEffectHandler.TargetEffects* effects, GameObjectId* targetIds)
    {
        this.receiveHook.Original(casterEntityId, caster, targetPosition, header, effects, targetIds);
        try
        {
            var player = Service.ObjectTable.LocalPlayer;
            if (header == null || player == null || player.ClassJob.RowId != BST.JobID ||
                casterEntityId != player.EntityId || (ActionType)header->ActionType != ActionType.Action ||
                (this.hasSequence && header->GlobalSequence == this.lastSequence))
                return;

            this.hasSequence = true;
            this.lastSequence = header->GlobalSequence;
            var action = header->ActionId;
            var now = Environment.TickCount64;
            this.timestamps[action] = now;
            // Some pet commands report their adjusted skill rather than the base Trick ID.
            var normalized = action == BST.CurrentPetTrickAction ? BST.Trick : action;
            this.Finisher.Observe(normalized, now);
        }
        catch (Exception ex)
        {
            this.Finisher.Reset();
            Service.PluginLog.Error(ex, "Unable to observe Beastmaster action");
        }
    }
}
