// Adapted from PunishXIV/WrathCombo (BSD-3-Clause). See licenses/WrathCombo-BSD-3-Clause.txt.
using System.Runtime.InteropServices;

namespace XIVComboExpandedestPlugin.Combos;

// The Instinct field at 0x10 requires at least 0x11 bytes; do not use upstream's 0x10 size.
[StructLayout(LayoutKind.Explicit, Size = 0x18)]
internal struct BSTGauge
{
    [FieldOffset(0x08)] public byte PlayerTP;
    [FieldOffset(0x09)] public byte BeastTP;
    [FieldOffset(0x0A)] public byte LastBeastTPSpent;
    [FieldOffset(0x0B)] public byte BattleHorn;
    [FieldOffset(0x0C)] public BST.InstinctualAffinity AffinityStatus;
    [FieldOffset(0x0D)] public BST.InstinctualAffinity ActiveAffinity;
    [FieldOffset(0x0E)] public byte Chain;
    [FieldOffset(0x0F)] public byte Kinship;
    [FieldOffset(0x10)] public byte Instinct;

    public byte KinshipBattlehorn => (byte)(this.Kinship & 0x0F);
    public int PetInstinct => this.Instinct & 0x3;
    public int MasterInstinct => (this.Instinct & 0xC) >> 2;
}
