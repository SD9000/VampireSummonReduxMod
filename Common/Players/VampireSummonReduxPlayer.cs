using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.Input;
using Terraria.ID;
using VampireSummonRedux.Common.Config;
using VampireSummonRedux.Common.Players;

namespace VampireSummonRedux.Common.Players
{
    public class VampireSummonReduxPlayer : ModPlayer
    {
        // ===== Progression =====
        public int Level = 1;
        public int XP = 0;
        public int UpgradePoints = 0;

        // ===== Upgrade Ranks =====
        public int DamageRank = 0;

        // Speed: capped + diminishing returns
        public int SpeedRank = 0;

        // Lifesteal chance: caps at 100%
        public int LifestealChanceRank = 0;

        // Lifesteal amount: now PERCENT-based
        // (other files must use GetLifestealHealPercent() instead of flat healing)
        public int LifestealAmountRank = 0;

        // Immunity frames (local NPCHit cooldown reduction): -1 tick/rank, caps at 6 ticks
        public int ImmunityRank = 0;

        // ===== Targeting =====
        public TargetingMode TargetMode = TargetingMode.ClosestToPlayer;

        // ===== Focus removed (compat stubs) =====
        // Keep these so the project compiles until you remove focus from UI/minion/net.
        // They do nothing and can be deleted once other files are updated.
        //[Obsolete("Focus was removed. This exists only for temporary compile compatibility.")]
        //public int //FocusSameTargetRank = 0;

        // ===== Tuning constants (requested behavior) =====
        // Immunity cooldown model: start at 12, -1 per rank, min 6 => max rank 6
        public const int ImmunityBaseCooldown = 12;
        public const int ImmunityMinCooldown = 6;
        public const int ImmunityCooldownDownPerRank = 1;
        public const int ImmunityMaxRank = 6;

        // Lifesteal chance: +2% per rank, cap 100%
        public const int LifestealChancePerRankPercent = 2;
        public const int LifestealChanceMaxPercent = 100;
        public const int LifestealChanceMaxRank = LifestealChanceMaxPercent / LifestealChancePerRankPercent; // 50

        // Lifesteal amount percent: small start, ends "slightly OP but reasonable"
        // Suggested: +0.25% per rank, cap at 6% of damage dealt.
        // That is strong at high DPS but not totally insane like flat HP per hit.
        public const float LifestealPercentPerRank = 0.0025f; // 0.25% per rank
        public const float LifestealPercentCap = 0.06f;       // 6% cap
        public int LifestealAmountMaxRank => (int)Math.Ceiling(LifestealPercentCap / LifestealPercentPerRank);

        // Speed caps at 50 ranks
        public const int SpeedMaxRank = 50;

        // ===== Lifecycle =====
        public override void Initialize()
        {
            Level = 1;
            XP = 0;
            UpgradePoints = 0;

            DamageRank = 0;
            SpeedRank = 0;
            LifestealChanceRank = 0;
            LifestealAmountRank = 0;
            ImmunityRank = 0;

            TargetMode = TargetingMode.ClosestToPlayer;

            //FocusSameTargetRank = 0; // compat
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            // Safety: keybind may be null during unload/reload edges
            if (VampireSummonReduxMod.OpenUpgradeUIKeybind == null)
                return;

            if (!VampireSummonReduxMod.OpenUpgradeUIKeybind.JustPressed)
                return;

            // Only the local player should open UI
            if (Main.myPlayer != Player.whoAmI)
                return;

            // Only open when holding the summon weapon
            if (Player.HeldItem.type != ModContent.ItemType<VampireSummonRedux.Content.Items.VampireKnives>())
                return;

            // Toggle the UI
            VampireSummonRedux.Common.UI.VampireUpgradeUISystem.Toggle();
        }

        public override void SaveData(TagCompound tag)
        {
            tag["Level"] = Level;
            tag["XP"] = XP;
            tag["UpgradePoints"] = UpgradePoints;

            tag["DamageRank"] = DamageRank;
            tag["SpeedRank"] = SpeedRank;
            tag["LifestealChanceRank"] = LifestealChanceRank;
            tag["LifestealAmountRank"] = LifestealAmountRank;
            tag["ImmunityRank"] = ImmunityRank;

            tag["TargetMode"] = (int)TargetMode;

            // compat
            //tag["//FocusSameTargetRank"] = //FocusSameTargetRank;
        }

        public override void LoadData(TagCompound tag)
        {
            Level = tag.GetInt("Level");
            XP = tag.GetInt("XP");
            UpgradePoints = tag.GetInt("UpgradePoints");

            DamageRank = tag.GetInt("DamageRank");
            SpeedRank = tag.GetInt("SpeedRank");
            LifestealChanceRank = tag.GetInt("LifestealChanceRank");
            LifestealAmountRank = tag.GetInt("LifestealAmountRank");
            ImmunityRank = tag.GetInt("ImmunityRank");

            TargetMode = (TargetingMode)tag.GetInt("TargetMode");

            // compat (safe if missing)
            if (tag.ContainsKey("//FocusSameTargetRank"))
                //FocusSameTargetRank = tag.GetInt("//FocusSameTargetRank");

            // Safety clamps (in case configs changed)
            ClampAll();
        }

        private void ClampAll()
        {
            if (Level < 1) Level = 1;
            if (XP < 0) XP = 0;
            if (UpgradePoints < 0) UpgradePoints = 0;

            DamageRank = Math.Max(0, DamageRank);
            SpeedRank = Math.Clamp(SpeedRank, 0, SpeedMaxRank);
            LifestealChanceRank = Math.Clamp(LifestealChanceRank, 0, LifestealChanceMaxRank);
            LifestealAmountRank = Math.Clamp(LifestealAmountRank, 0, LifestealAmountMaxRank);
            ImmunityRank = Math.Clamp(ImmunityRank, 0, ImmunityMaxRank);

            //FocusSameTargetRank = 0; // compat disabled
        }

        // ===== XP / Leveling =====
        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            XP += amount;

            // You likely already have a level-up curve in your config.
            // This is a simple "XP to next level" loop that won't explode if XP jumps.
            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();
            while (XP >= GetXPToNextLevel(cfg))
            {
                XP -= GetXPToNextLevel(cfg);
                Level++;

                // Award points
                UpgradePoints += Math.Max(0, cfg.PointsPerLevel);
            }
        }

        private int GetXPToNextLevel(VampireSummonReduxConfig cfg)
        {
            const int XPToLevelBase = 50;
            const int XPToLevelPerLevel = 15;

            return XPToLevelBase + XPToLevelPerLevel * (Level - 1);
        }

        // ===== Upgrade purchasing =====
        public int GetUpgradeCost(UpgradeType up)
        {
            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();
            int baseCost = Math.Max(0, cfg.BaseUpgradeCost);
            int perRank = Math.Max(0, cfg.UpgradeCostPerRank);

            int rank = up switch
            {
                UpgradeType.Damage => DamageRank,
                UpgradeType.Speed => SpeedRank,
                UpgradeType.LifestealChance => LifestealChanceRank,
                UpgradeType.LifestealAmount => LifestealAmountRank,
                UpgradeType.ImmunityFrames => ImmunityRank,

                // Focus removed
                //UpgradeType.//FocusSameTarget => 0,

                _ => 0
            };

            return baseCost + perRank * rank;
        }

        public bool CanBuyUpgrade(UpgradeType up)
        {
            // Block purchases at caps
            if (up == UpgradeType.ImmunityFrames && ImmunityRank >= ImmunityMaxRank)
                return false;

            if (up == UpgradeType.LifestealChance && LifestealChanceRank >= LifestealChanceMaxRank)
                return false;

            if (up == UpgradeType.LifestealAmount && LifestealAmountRank >= LifestealAmountMaxRank)
                return false;

            if (up == UpgradeType.Speed && SpeedRank >= SpeedMaxRank)
                return false;

            // Focus removed
            //if (up == UpgradeType.//FocusSameTarget)
                //return false;

            int cost = GetUpgradeCost(up);
            return UpgradePoints >= cost;
        }

        public bool TryBuyUpgrade(UpgradeType up)
        {
            if (!CanBuyUpgrade(up))
                return false;

            int cost = GetUpgradeCost(up);
            if (UpgradePoints < cost)
                return false;

            // Spend points
            UpgradePoints -= cost;

            switch (up)
            {
                case UpgradeType.Damage:
                    DamageRank++;
                    break;

                case UpgradeType.Speed:
                    SpeedRank = Math.Min(SpeedMaxRank, SpeedRank + 1);
                    break;

                case UpgradeType.LifestealChance:
                    LifestealChanceRank = Math.Min(LifestealChanceMaxRank, LifestealChanceRank + 1);
                    break;

                case UpgradeType.LifestealAmount:
                    LifestealAmountRank = Math.Min(LifestealAmountMaxRank, LifestealAmountRank + 1);
                    break;

                case UpgradeType.ImmunityFrames:
                    ImmunityRank = Math.Min(ImmunityMaxRank, ImmunityRank + 1);
                    break;

                    // Focus removed
                //case UpgradeType.//FocusSameTarget:
                //default:
                    // If somehow called, refund the cost so no points are lost.
                    //UpgradePoints += cost;
                    //return false;
            }

            return true;
        }

        public void RefundAllUpgrades()
        {
            DamageRank = 0;
            SpeedRank = 0;
            LifestealChanceRank = 0;
            LifestealAmountRank = 0;
            ImmunityRank = 0;

            // Focus removed
            //FocusSameTargetRank = 0;

            // Rebuild points based on level
            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();
            UpgradePoints = Math.Max(0, (Level - 1) * Math.Max(0, cfg.PointsPerLevel));
        }

        // ===== Stat helpers used by other files (hooks) =====

        /// <summary>
        /// Current lifesteal chance (0..100).
        /// Other files should use this instead of rank*2 directly.
        /// </summary>
        public int GetLifestealChancePercent()
        {
            int pct = LifestealChanceRank * LifestealChancePerRankPercent;
            return Math.Clamp(pct, 0, LifestealChanceMaxPercent);
        }

        /// <summary>
        /// Lifesteal amount as a percentage of damage dealt (0..cap).
        /// Example: 0.01 = 1%.
        /// Other files must apply this to damageDone to compute heal amount.
        /// </summary>
        public float GetLifestealHealPercent()
        {
            float p = LifestealAmountRank * LifestealPercentPerRank;
            if (p > LifestealPercentCap) p = LifestealPercentCap;
            if (p < 0f) p = 0f;
            return p;
        }

        /// <summary>
        /// Local NPC hit cooldown in ticks for the minion projectile.
        /// Other files should set Projectile.localNPCHitCooldown to this.
        /// </summary>
        public int GetLocalHitCooldownTicks()
        {
            int cd = ImmunityBaseCooldown - ImmunityRank * ImmunityCooldownDownPerRank;
            return Math.Max(ImmunityMinCooldown, cd);
        }

        /// <summary>
        /// Diminishing returns speed scalar (0..1) based on SpeedRank (capped at 50).
        /// This gives early ranks noticeable gain and later ranks flatten out.
        /// </summary>
        public float GetSpeedPlateau01()
        {
            int r = Math.Clamp(SpeedRank, 0, SpeedMaxRank);
            // Smooth plateau curve: 1 - exp(-r/k)
            const float k = 14f;
            return 1f - (float)Math.Exp(-r / k);
        }

        /// <summary>
        /// Example: extraUpdates bonus from speed (0..1). Keep small; extraUpdates gets wild quickly.
        /// </summary>
        public int GetExtraUpdatesBonus()
        {
            // 0 at rank 0, approaches 1 by high ranks
            float p = GetSpeedPlateau01();
            return p >= 0.75f ? 1 : 0;
        }
    }

    // If your enum is elsewhere, keep using yours.
    // This is only here to make the file self-contained if needed.
    public enum TargetingMode
    {
        ClosestToPlayer,
        ClosestToMinion
    }
}
