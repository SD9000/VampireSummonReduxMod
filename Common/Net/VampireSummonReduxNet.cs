using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using VampireSummonRedux.Common.Players;

namespace VampireSummonRedux.Common.Net
{
    public static class VampireSummonReduxNet
    {
        private enum Msg : byte
        {
            BuyUpgrade,
            Refund,
            FullSyncRequest,
            FullSyncData
        }

        private static Mod ModInstance => ModContent.GetInstance<VampireSummonReduxMod>();

        public static void SendBuyUpgrade(int whoAmI, UpgradeType up)
        {
            ModPacket p = ModInstance.GetPacket();
            p.Write((byte)Msg.BuyUpgrade);
            p.Write((byte)whoAmI);
            p.Write((byte)up);
            p.Send();
        }

        public static void SendRefund(int whoAmI)
        {
            ModPacket p = ModInstance.GetPacket();
            p.Write((byte)Msg.Refund);
            p.Write((byte)whoAmI);
            p.Send();
        }

        public static void SendFullSyncRequest(int whoAmI)
        {
            ModPacket p = ModInstance.GetPacket();
            p.Write((byte)Msg.FullSyncRequest);
            p.Write((byte)whoAmI);
            p.Send();
        }

        /// <summary>
        /// Call this from your Mod.HandlePacket.
        /// </summary>
        public static void HandlePacket(BinaryReader r, int whoAmI)
        {
            Msg msg = (Msg)r.ReadByte();

            switch (msg)
            {
                case Msg.BuyUpgrade:
                {
                    int plr = r.ReadByte();
                    UpgradeType up = (UpgradeType)r.ReadByte();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        var mp = Main.player[plr].GetModPlayer<VampireSummonReduxPlayer>();
                        mp.TryBuyUpgrade(up);

                        // Broadcast the authoritative state
                        SendFullSyncData(plr, toClient: -1);
                    }
                    break;
                }

                case Msg.Refund:
                {
                    int plr = r.ReadByte();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        var mp = Main.player[plr].GetModPlayer<VampireSummonReduxPlayer>();
                        mp.RefundAllUpgrades();

                        SendFullSyncData(plr, toClient: -1);
                    }
                    break;
                }

                case Msg.FullSyncRequest:
                {
                    int plr = r.ReadByte();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        // Send back only to requesting client
                        SendFullSyncData(plr, toClient: plr);
                    }
                    break;
                }

                case Msg.FullSyncData:
                {
                    // Client receives authoritative data
                    int plr = r.ReadByte();
                    var mp = Main.player[plr].GetModPlayer<VampireSummonReduxPlayer>();

                    mp.Level = r.ReadInt32();
                    mp.XP = r.ReadInt32();
                    mp.UpgradePoints = r.ReadInt32();

                    mp.DamageRank = r.ReadInt32();
                    mp.SpeedRank = r.ReadInt32();
                    mp.LifestealChanceRank = r.ReadInt32();
                    mp.LifestealAmountRank = r.ReadInt32();
                    mp.ImmunityRank = r.ReadInt32();

                    mp.TargetMode = (TargetingMode)r.ReadInt32();

                    break;
                }
            }
        }

        private static void SendFullSyncData(int plr, int toClient)
        {
            var mp = Main.player[plr].GetModPlayer<VampireSummonReduxPlayer>();

            ModPacket p = ModInstance.GetPacket();
            p.Write((byte)Msg.FullSyncData);
            p.Write((byte)plr);

            p.Write(mp.Level);
            p.Write(mp.XP);
            p.Write(mp.UpgradePoints);

            p.Write(mp.DamageRank);
            p.Write(mp.SpeedRank);
            p.Write(mp.LifestealChanceRank);
            p.Write(mp.LifestealAmountRank);
            p.Write(mp.ImmunityRank);

            p.Write((int)mp.TargetMode);

            // toClient:
            //  -1 => broadcast (server)
            //  >=0 => send only to that client (server)
            if (Main.netMode == NetmodeID.Server)
            {
                if (toClient >= 0)
                    p.Send(toClient);
                else
                    p.Send();
            }
            else
            {
                // client should never call this in normal flow, but keep safe
                p.Send();
            }
        }
    }
}
