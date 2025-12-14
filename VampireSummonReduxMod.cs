using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using VampireSummonRedux.Common.Players;
using VampireSummonRedux.Common.Net;

namespace VampireSummonRedux
{
    public class VampireSummonReduxMod : Mod
    {
        public static ModKeybind OpenUpgradeMenu;

        public override void Load()
        {
            OpenUpgradeMenu = KeybindLoader.RegisterKeybind(this, "Open Upgrade Menu", "P");

            if (!Main.dedServ)
                Common.UI.VampireUpgradeUISystem.LoadUI();
        }

        public override void Unload()
        {
            OpenUpgradeMenu = null;

            if (!Main.dedServ)
                Common.UI.VampireUpgradeUISystem.UnloadUI();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            VampirePacketType type = (VampirePacketType)reader.ReadByte();

            switch (type)
            {

                case VampirePacketType.FullSyncRequest:
                {
                    int plr = reader.ReadByte();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        // Send this player's full state to everyone (or just requester if you want)
                        for (int i = 0; i < Main.maxPlayers; i++)
                            if (Main.player[i].active)
                                VampireSummonReduxNet.SendFullSync(plr, i);
                    }
                    break;
                }

                case VampirePacketType.FullSync:
                {
                    int plr = reader.ReadByte();
                    var mp = Main.player[plr].GetModPlayer<VampireSummonReduxPlayer>();

                    mp.Level = reader.ReadInt32();
                    mp.XP = reader.ReadInt64();
                    mp.UpgradePoints = reader.ReadInt32();

                    mp.DamageRank = reader.ReadInt32();
                    mp.SpeedRank = reader.ReadInt32();
                    mp.LifestealChanceRank = reader.ReadInt32();
                    mp.LifestealAmountRank = reader.ReadInt32();
                    mp.//FocusSameTargetRank = reader.ReadInt32();

                    mp.TargetMode = (TargetingMode)reader.ReadByte();
                    break;
                }

                case VampirePacketType.BuyUpgrade:
                {
                    int plr = reader.ReadByte();
                    UpgradeType up = (UpgradeType)reader.ReadByte();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        var mp = Main.player[plr].GetModPlayer<VampireSummonReduxPlayer>();
                        mp.TryBuyUpgrade(up);

                        for (int i = 0; i < Main.maxPlayers; i++)
                            if (Main.player[i].active)
                                VampireSummonReduxNet.SendFullSync(plr, i);
                    }
                    break;
                }

                case VampirePacketType.AddXP:
                {
                    int plr = reader.ReadByte();
                    int amount = reader.ReadInt32();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        var mp = Main.player[plr].GetModPlayer<VampireSummonReduxPlayer>();
                        mp.AddXP(amount);

                        for (int i = 0; i < Main.maxPlayers; i++)
                            if (Main.player[i].active)
                                VampireSummonReduxNet.SendFullSync(plr, i);
                    }
                    break;
                }

                case VampirePacketType.Refund:
                {
                    int plr = reader.ReadByte();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        if (plr != whoAmI)
                            plr = whoAmI;

                        var mp = Main.player[plr].GetModPlayer<VampireSummonReduxPlayer>();
                        mp.RefundAllUpgrades();

                        for (int i = 0; i < Main.maxPlayers; i++)
                            if (Main.player[i].active)
                                VampireSummonReduxNet.SendFullSync(plr, i);
                    }
                    break;
                }
            }
        }
    }
}
