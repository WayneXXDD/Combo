// Adapted from PunishXIV/WrathCombo (BSD-3-Clause). See licenses/WrathCombo-BSD-3-Clause.txt.
using System.Collections.Generic;

namespace XIVComboExpandedestPlugin.Combos;

internal static partial class BST
{
    public const byte JobID = 43;

    // A real action unavailable to BST replaces WrathCombo's custom "Cease" action.
    public const uint BlockedAction = 16230;
    public const uint
        SmashAxe = 44879,
        Capture = 44880,
        FirstBattlehorn = 44881,
        Gauge = 44882,
        AxebladeBite = 44883,
        AvalancheAxe = 44884,
        Shieldsplitter = 44885,
        BeastMode = 44886,
        MistralAxe = 44887,
        SpinningAxe = 44888,
        GaleAxe = 44889,
        TemperedRelease = 44890,
        PartingBlow = 44891,
        SecondBattlehorn = 44892,
        ShieldCharge = 44893,
        ThirdBattlehorn = 44894,
        Borrow = 44895,
        Beastskin = 44896,
        Vileskin = 44897,
        CloudSkim = 44898,
        Seedsower = 44899,
        QuellingWave = 44900,
        Scaleskin = 44901,
        SoulCrush = 44902,
        ScouringAsh = 44903,
        RallyingCheer = 44904,
        Rally = 44905,
        BrutalRage = 44930,
        HawkishTalons = 44931,
        RisenFall = 44932,
        Calamity = 44933,
        Trick = 47093,
        BorrowBeast = 47238,
        BorrowVile = 47239,
        BorrowCloud = 47240,
        BorrowSeed = 47241,
        BorrowWave = 47242,
        BorrowScale = 47243,
        BorrowSoul = 47244,
        BorrowAsh = 47245;

    public static class TrickActions
    {
        public const uint
            CusithRake = 44935,
            SquirrelSomersaultSlash = 44937,
            LambFleeceButt = 44939,
            PugilScrewdriver = 44941,
            OpoopoStoneThrow = 44943,
            DodoFowlStench = 44945,
            CoblynBestialThunder = 44947,
            DiremiteDeadlyThrust = 44949,
            MegalocrabDrenchingBlow = 44951,
            WespeSharpSting = 44953,
            VultureWingCutter = 44955,
            MandragoraBudbutt = 44957,
            GeshunpestDarkThunder = 44959,
            PukFireball = 44961,
            CrabBubbleShower = 44963,
            MantisStandingChine = 44965,
            SlimeDigest = 44967,
            DullahanIronJustice = 44969,
            BatBloodDrain = 44971,
            FlyingtrapSourSough = 44973,
            ZizIceBreath = 44975,
            SabotenderNaturalNeedles = 44977,
            GolemBoulderClap = 44979,
            ApkalluFlyingSardine = 44981,
            AdamantoiseBestialThunderII = 44983,
            BuffaloHeave = 44985,
            UragniteFrostBreath = 44987,
            WormSandBreath = 44989,
            SprigganRomp = 44991,
            GoobbueBeatdown = 44993,
            GigantoadBestialBlizzardII = 44995,
            ColibriLoop = 44997,
            CoeurlBlaster = 44999,
            RaptorFrostBreath = 45001,
            DrakeBurningCyclone = 45003,
            TreantAcornBomb = 45005,
            AntlingMandibleBite = 45007,
            ChimeratheLionsBreath = 45009,
            MorbolVineProbe = 45011,
            GhostFellGale = 45013,
            SalamanderBrackishRain = 45015,
            CobraDrippingFang = 45017,
            HydraMainTrap = 45019,
            DamselflyCursedSphere = 45021,
            RottinggoobbueDirtySneeze = 45023,
            ZuFlyingFrenzy = 45025,
            IcegolemIceGuillotine = 45027,
            KarlabosImpale = 45029,
            RafflesiaBloodyCaress = 45031,
            BehemothThunderbolt = 45033;
    }

    public static class TemperedReleaseActions
    {
        public const uint
            CusithRelentlessRake = 44936,
            SquirrelScamper = 44938,
            LambLullaby = 44940,
            PugilWaterWall = 44942,
            OpoopoPinsandNails = 44944,
            DodoStrut = 44946,
            CoblynVulcanize = 44948,
            DiremiteSilkscreen = 44950,
            MegalocrabBubbleShower = 44952,
            WespeFinalSting = 44954,
            VultureBloodcurdlingCaw = 44956,
            MandragoraHeirloomScream = 44958,
            GeshunpestOdium = 44960,
            PukTailChase = 44962,
            CrabHundredFists = 44964,
            MantisEerieSoundwave = 44966,
            SlimeSyrup = 44968,
            DullahanKingsWill = 44970,
            BatUltrasonics = 44972,
            FlyingtrapNecroticNectar = 44974,
            ZizPetribreath = 44976,
            SabotenderHypodermicHustle = 44978,
            GolemRockslide = 44980,
            ApkalluRegurgitate = 44982,
            AdamantoiseHardenShell = 44984,
            BuffaloWarCry = 44986,
            UragniteGasShell = 44988,
            WormBottomlessDesert = 44990,
            SprigganFreneticFlurry = 44992,
            GoobbueMoldySneeze = 44994,
            GigantoadStickyTongue = 44996,
            ColibriPeckingFlurry = 44998,
            CoeurlChargedWhisker = 45000,
            RaptorFoulBreath = 45002,
            DrakeSmolderingScales = 45004,
            TreantArborealStorm = 45006,
            AntlingFormicPheromones = 45008,
            ChimeratheRamsVoice = 45010,
            MorbolBadBreath = 45012,
            GhostCurse = 45014,
            SalamanderPeculiarLight = 45016,
            CobraStoneGaze = 45018,
            HydraWhiteBreath = 45020,
            DamselflyVenom = 45022,
            RottinggoobbueInhale = 45024,
            ZuBreathWing = 45026,
            IcegolemFrozenHeart = 45028,
            KarlabosTailScrew = 45030,
            RafflesiaBlightedBouquet = 45032,
            BehemothMeteor = 45034;

    }

    public static class Buffs
    {
        public const uint

            VolantHeart = 4595,
            RampantHeart = 4596,
            DurantHeart = 4597,
            EldritchHeart = 4598;
    }

    public static class Debuffs
    {
        public const uint
            InterestCaptured = 4626;
    }

    public static class Traits
    {
        public const uint
            WildHeart = 690,
            WildHeartII = 691,
            BattlehornMastery = 692,
            WildHeartIV = 693,
            WildHeartIII = 694,
            TemperedReleaseMastery = 749,
            EnhancedBorrow = 750,
            EnhancedShieldCharge = 751,
            Beastmastery = 752,
            BattlehornMasteryII = 754,
            BattlehornMasteryIII = 755,
            EnhancedRally = 756,
            EnhancedRallyingCheer = 757,
            InstinctualMastery = 758;

    }

    private static List<uint> RampantTricks =
    [
         TrickActions.CusithRake,
         TrickActions.SquirrelSomersaultSlash,
         TrickActions.LambFleeceButt,
         TrickActions.OpoopoStoneThrow,
         TrickActions.DiremiteDeadlyThrust,
         TrickActions.MandragoraBudbutt,
         TrickActions.PukFireball,
         TrickActions.SabotenderNaturalNeedles,
         TrickActions.BuffaloHeave,
         TrickActions.SprigganRomp,
         TrickActions.GoobbueBeatdown,
         TrickActions.DrakeBurningCyclone,
         TrickActions.AntlingMandibleBite,
         TrickActions.ChimeratheLionsBreath,
         TrickActions.MorbolVineProbe,
    ];

    private static List<uint> EldritchTricks =
    [
         TrickActions.DodoFowlStench,
         TrickActions.CoblynBestialThunder,
         TrickActions.GeshunpestDarkThunder,
         TrickActions.SlimeDigest,
         TrickActions.GolemBoulderClap,
         TrickActions.AdamantoiseBestialThunderII,
         TrickActions.WormSandBreath,
         TrickActions.GigantoadBestialBlizzardII,
         TrickActions.CoeurlBlaster,
         TrickActions.TreantAcornBomb,
         TrickActions.RottinggoobbueDirtySneeze,
         TrickActions.RafflesiaBloodyCaress,
         TrickActions.BehemothThunderbolt,
    ];

    private static List<uint> DurantTricks =
    [
         TrickActions.PugilScrewdriver,
         TrickActions.MegalocrabDrenchingBlow,
         TrickActions.CrabBubbleShower,
         TrickActions.MantisStandingChine,
         TrickActions.DullahanIronJustice,
         TrickActions.ZizIceBreath,
         TrickActions.ApkalluFlyingSardine,
         TrickActions.UragniteFrostBreath,
         TrickActions.RaptorFrostBreath,
         TrickActions.SalamanderBrackishRain,
         TrickActions.CobraDrippingFang,
         TrickActions.HydraMainTrap,
         TrickActions.IcegolemIceGuillotine,
         TrickActions.KarlabosImpale,

    ];

    private static List<uint> VolantTricks =
    [
         TrickActions.WespeSharpSting,
         TrickActions.VultureWingCutter,
         TrickActions.BatBloodDrain,
         TrickActions.FlyingtrapSourSough,
         TrickActions.ColibriLoop,
         TrickActions.GhostFellGale,
         TrickActions.DamselflyCursedSphere,
         TrickActions.ZuFlyingFrenzy,
    ];

    public enum InstinctualAffinity : byte
    {
        None = 0, Volant = 1, Rampant = 2, Durant = 3, Eldritch = 4,
        Sunstrider = 5, Moonstalker = 6, WaveringHeart = 7,
    }

    public enum TrickTypes { Durant, Eldritch, Volant, Rampant, None }
    public enum RallyingType { None, Rally, RallyingCheer }
}
