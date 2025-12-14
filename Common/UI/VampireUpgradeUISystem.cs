using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace VampireSummonRedux.Common.UI
{
    public class VampireUpgradeUISystem : ModSystem
    {
        internal static UserInterface UI;
        internal static VampireUpgradeUIState State;

        public static void LoadUI()
        {
            UI = new UserInterface();
            State = new VampireUpgradeUIState();
            State.Activate();
        }

        public static void UnloadUI()
        {
            UI = null;
            State = null;
        }

        public static void Toggle()
        {
            if (UI == null || State == null)
                return;

            if (UI.CurrentState == null)
                UI.SetState(State);
            else
                UI.SetState(null);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (UI?.CurrentState != null)
                UI.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "VampireSummonRedux: UpgradeMenu",
                    () =>
                    {
                        if (UI?.CurrentState != null)
                            UI.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }
    }
}
