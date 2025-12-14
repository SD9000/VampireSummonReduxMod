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
            VampireSummonRedux.Common.Net.VampireSummonReduxNet.HandlePacket(reader, whoAmI);
        }
    }
}
