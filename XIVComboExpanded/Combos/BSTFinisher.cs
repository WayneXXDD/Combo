// Adapted from PunishXIV/WrathCombo (BSD-3-Clause). See licenses/WrathCombo-BSD-3-Clause.txt.
using System.Collections.Generic;

namespace XIVComboExpandedestPlugin.Combos;

// / <summary>Advances only when an expected action has actually been observed.</summary>
internal sealed class BeastmasterFinisher
{
    internal List<uint> Actions { get; } = new();
    private long? startedAt;

    internal void Update(long now, bool canStart, uint counterclockwiseAction)
    {
        if (this.startedAt is { } started && now - started > 10_000)
        {
            this.Reset();
            return;
        }

        if (this.Actions.Count == 0 && canStart && counterclockwiseAction != 0)
            this.Actions.AddRange(new[] { BST.Trick, counterclockwiseAction, BST.Rally, BST.RallyingCheer, BST.Trick });
    }

    internal void Observe(uint action, long now)
    {
        if (this.Actions.Count == 0 || this.Actions[0] != action)
            return;

        this.startedAt ??= now;
        this.Actions.RemoveAt(0);
        if (this.Actions.Count == 0)
            this.startedAt = null;
    }

    internal void Reset()
    {
        this.Actions.Clear();
        this.startedAt = null;
    }
}
