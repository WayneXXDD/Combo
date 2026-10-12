# Beastmaster combos

Open `/pcombo` and expand **Beastmaster**. All seven features are disabled by default.

| Feature | Hotbar action | Behavior |
| --- | --- | --- |
| Simple Mode | Smash Axe | Pet summoning and capture, axe/Trick combos, Rally setup and available cooldowns |
| Basic Combo | Axeblade Bite | Smash Axe → Axeblade Bite → Shieldsplitter |
| Instinctual Combo | Avalanche/Mistral/Spinning/Gale Axe | Choose the axe for the active pet; alternate Trick and axe according to Rally stacks |
| Intentional Combo | Trick | Clockwise/counterclockwise combos; optional Infinitive setup |
| Capture Helper | Capture | Block targets that are not capturable or are already unlocked |
| Battlehorn Lockout | First/Second/Third Battlehorn | Block horn replacement while a horn is active in combat |
| Borrow Feature | Borrow | Show the borrowed Beast Mode action |

Both combo TP thresholds default to 100 and can be set from 100 to 250.
Cycle Beasts, Infinitive setup for Intentional Combo, and Borrow's current-horn-only option default to off.
The optional settings appear below the corresponding enabled feature.

This plugin replaces hotbar actions; you still press the buttons yourself.
It does not import WrathCombo's automatic action execution, target switching, native pseudo-actions, or IPC integrations.
Blocked actions use an unavailable non-BST skill instead of WrathCombo's custom `Cease` pseudo-action.

The port uses WrathCombo's pending Beastmaster gauge layout and Lumina pet schemas.
It requires game data and Dalamud libraries that include BST. Compilation and offline logic tests cannot verify those layouts in a running game.
In-game checks should cover each pet affinity, level sync, TP thresholds, capturing an unlocked/locked beast, switching horns, the Infinitive sequence, and plugin reload/job changes.

Build the plugin using `dotnet build XIVComboExpanded/XIVComboExpanded.csproj -c Release` with .NET 10 and the Dalamud development libraries.
Run the offline checks using `dotnet run --project tests/Beastmaster.Tests/Beastmaster.Tests.csproj -c Release`.
GitHub Actions performs both steps and uploads `PluginRepoZip`, containing `latest.zip`.

## Attribution

Adapted from [PunishXIV/WrathCombo](https://github.com/PunishXIV/WrathCombo), commit `87545989a3996ec36bb52a9db44fd99fdb657554`,
including `WrathCombo/Combos/PvE/BST/` and the BST gauge/affinity definitions in `WrathCombo/Data/JobGaugeDebugging.cs`.
Copyright (c) 2024, Puni.sh. The original BSD 3-Clause license is retained in
`licenses/WrathCombo-BSD-3-Clause.txt` and included in the plugin ZIP.
