using System.ComponentModel;
using Terraria.ModLoader.Config;
using VampireSummonRedux.Common.Players;

namespace VampireSummonRedux.Common.Config
{
    public class VampireSummonReduxConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("XP")]
        [DefaultValue(true)]
        public bool GainXPOnHit;

        [DefaultValue(true)]
        public bool GainXPOnKill;

        [DefaultValue(2)]
        [Range(0, 999)]
        public int XpPerHit;

        [DefaultValue(10)]
        [Range(0, 9999)]
        public int XpPerKill;

        [Header("Leveling")]
        [DefaultValue(100)]
        [Range(1, 999999)]
        public int BaseXpToLevel;

        [DefaultValue(40)]
        [Range(0, 999999)]
        public int XpToLevelPerLevel;

        [Header("Upgrades")]
        [DefaultValue(1)]
        [Range(1, 99)]
        public int PointsPerLevel;

        [DefaultValue(1)]
        [Range(0, 99)]
        public int BaseUpgradeCost;

        [DefaultValue(1)]
        [Range(0, 99)]
        public int UpgradeCostPerRank;

        [Header("DamageCaps")]
        [DefaultValue(60)]
        [Range(0, 9999)]
        public int PreHardmodeBonusDamageCap;

        [DefaultValue(250)]
        [Range(0, 9999)]
        public int PostMoonLordBonusDamageCap;

        [Header("Targeting")]
        [DefaultValue(TargetingMode.ClosestToPlayer)]
        public TargetingMode DefaultTargetingMode;
    }
}
