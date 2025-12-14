using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using VampireSummonRedux.Common.Players;

namespace VampireSummonRedux.Common.Net
{
    public enum VampirePacketType : byte
    {
        FullSync,
        BuyUpgrade,
        AddXP,
        FullSyncRequest,
        Refund
    }

    public static class VampireSummonReduxNet
    {
        public static void SendFullSync(int playerWho, int toWho)
        {
            if (Main.netMode != NetmodeID.Server) return;

            var p = Main.player[playerWho];
            var mp = p.GetModPlayer<VampireSummonReduxPlayer>();

            ModPacket pkt = ModContent.GetInstance<VampireSummonRedux.VampireSummonReduxMod>().GetPacket();
            pkt.Write((byte)VampirePacketType.FullSync);
            pkt.Write((byte)playerWho);

            pkt.Write(mp.Level);
            pkt.Write(mp.XP);
            pkt.Write(mp.UpgradePoints);

            pkt.Write(mp.DamageRank);
            pkt.Write(mp.SpeedRank);
            pkt.Write(mp.LifestealChanceRank);
            pkt.Write(mp.LifestealAmountRank);
            //pkt.Write(mp.//FocusSameTargetRank);

            pkt.Write((byte)mp.TargetMode);
            pkt.Send(toWho);
        }

        public static void SendBuyUpgrade(int playerWho, UpgradeType type)
        {
            ModPacket pkt = ModContent.GetInstance<VampireSummonRedux.VampireSummonReduxMod>().GetPacket();
            pkt.Write((byte)VampirePacketType.BuyUpgrade);
            pkt.Write((byte)playerWho);
            pkt.Write((byte)type);
            pkt.Send();
        }

        public static void SendAddXp(int playerWho, int amount)
        {
            ModPacket pkt = ModContent.GetInstance<VampireSummonRedux.VampireSummonReduxMod>().GetPacket();
            pkt.Write((byte)VampirePacketType.AddXP);
            pkt.Write((byte)playerWho);
            pkt.Write(amount);
            pkt.Send();
        }

        public static void SendRefund(int playerWho)
        {
            ModPacket pkt = ModContent.GetInstance<VampireSummonRedux.VampireSummonReduxMod>().GetPacket();
            pkt.Write((byte)VampirePacketType.Refund);
            pkt.Write((byte)playerWho);
            pkt.Send();
        }

        public static void SendFullSyncRequest(int playerWho)
        {
            ModPacket pkt = ModContent.GetInstance<VampireSummonRedux.VampireSummonReduxMod>().GetPacket();
            pkt.Write((byte)VampirePacketType.FullSyncRequest);
            pkt.Write((byte)playerWho);
            pkt.Send();
        }
    }
}
