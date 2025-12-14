using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using VampireSummonRedux.Common.Config;
using Terraria.GameInput;
using VampireSummonRedux.Common.UI;
using VampireSummonRedux.Content.Items;
using VampireSummonRedux.Common.Net;


namespace VampireSummonRedux.Common.Players
{
    public enum TargetingMode
    {
        ClosestToPlayer,
        ClosestToMinion
    }

    public enum UpgradeType : byte
    {
        Damage,
        Speed,
        LifestealChance,
        LifestealAmount,
        FocusSameTarget,
        ToggleTargetingMode
    }

    public class VampireSummonReduxPlayer : ModPlayer
    {
        public int Level = 1;
        public long XP = 0;
        public int UpgradePoints = 0;

        public int DamageRank = 0;
        public int SpeedRank = 0;
        public int LifestealChanceRank = 0;
        public int LifestealAmountRank = 0;
        public int FocusSameTargetRank = 0;

        public TargetingMode TargetMode = TargetingMode.ClosestToPlayer;

        public override void Initialize()
        {
            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();
            TargetMode = cfg.DefaultTargetingMode;
        }

        public long XPToNextLevel()
        {
            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();
            return cfg.BaseXpToLevel + (long)(Level - 1) * cfg.XpToLevelPerLevel;
        }

        public void AddXP(long amount)
        {
            if (amount <= 0) return;

            XP += amount;

            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();
            while (XP >= XPToNextLevel())
            {
                XP -= XPToNextLevel();
                Level++;
                UpgradePoints += cfg.PointsPerLevel;
            }
        }

        public int UpgradeCost(int currentRank)
        {
            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();
            return cfg.BaseUpgradeCost + currentRank * cfg.UpgradeCostPerRank;
        }

        public bool TryBuyUpgrade(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Damage:
                    return TrySpendAndInc(ref DamageRank);
                case UpgradeType.Speed:
                    return TrySpendAndInc(ref SpeedRank);
                case UpgradeType.LifestealChance:
                    return TrySpendAndInc(ref LifestealChanceRank);
                case UpgradeType.LifestealAmount:
                    return TrySpendAndInc(ref LifestealAmountRank);
                case UpgradeType.FocusSameTarget:
                    return TrySpendAndInc(ref FocusSameTargetRank);
                case UpgradeType.ToggleTargetingMode:
                    TargetMode = (TargetMode == TargetingMode.ClosestToPlayer) ? TargetingMode.ClosestToMinion : TargetingMode.ClosestToPlayer;
                    return true;
            }

            return false;
        }

        private bool TrySpendAndInc(ref int rankField)
        {
            int cost = UpgradeCost(rankField);
            if (UpgradePoints < cost)
                return false;

            UpgradePoints -= cost;
            rankField++;
            return true;
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
            tag["FocusSameTargetRank"] = FocusSameTargetRank;
            tag["TargetMode"] = (int)TargetMode;
        }

        public override void LoadData(TagCompound tag)
        {
            Level = tag.GetInt("Level");
            XP = tag.GetLong("XP");
            UpgradePoints = tag.GetInt("UpgradePoints");
            DamageRank = tag.GetInt("DamageRank");
            SpeedRank = tag.GetInt("SpeedRank");
            LifestealChanceRank = tag.GetInt("LifestealChanceRank");
            LifestealAmountRank = tag.GetInt("LifestealAmountRank");
            FocusSameTargetRank = tag.GetInt("FocusSameTargetRank");
            TargetMode = (TargetingMode)tag.GetInt("TargetMode");
        }

        // Minimal net sync for MP
        public override void CopyClientState(ModPlayer targetCopy)
        {
            var t = (VampireSummonReduxPlayer)targetCopy;
            t.Level = Level;
            t.XP = XP;
            t.UpgradePoints = UpgradePoints;
            t.DamageRank = DamageRank;
            t.SpeedRank = SpeedRank;
            t.LifestealChanceRank = LifestealChanceRank;
            t.LifestealAmountRank = LifestealAmountRank;
            t.FocusSameTargetRank = FocusSameTargetRank;
            t.TargetMode = TargetMode;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            // Only runs on the local client in MP. If something changed compared to the last
            // known server state, send a packet to update it.
            var old = (VampireSummonReduxPlayer)clientPlayer;

            bool changed =
            Level != old.Level ||
            XP != old.XP ||
            UpgradePoints != old.UpgradePoints ||
            DamageRank != old.DamageRank ||
            SpeedRank != old.SpeedRank ||
            LifestealChanceRank != old.LifestealChanceRank ||
            LifestealAmountRank != old.LifestealAmountRank ||
            FocusSameTargetRank != old.FocusSameTargetRank ||
            TargetMode != old.TargetMode;

            if (!changed)
                return;

            // Ask server to sync this player's state to everyone.
            // We already have FullSync logic server->clients, so in MP we just send a "BuyUpgrade/AddXP"
            // normally. But since UI toggles (target mode) can change without those packets,
            // this ensures it stays consistent.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                VampireSummonReduxNet.SendFullSyncRequest(Player.whoAmI);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            VampireSummonReduxNet.SendFullSync(Player.whoAmI, toWho);
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (VampireSummonReduxMod.OpenUpgradeMenu.JustPressed)
            {
                if (Player.HeldItem?.ModItem is VampireKnives)
                    VampireUpgradeUISystem.Toggle();
            }
        }
    }
}
