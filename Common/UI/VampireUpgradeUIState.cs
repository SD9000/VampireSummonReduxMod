using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using VampireSummonRedux.Common.Players;
using VampireSummonRedux.Common.Config;
using VampireSummonRedux.Content.Items;
using VampireSummonRedux.Common.Net;

namespace VampireSummonRedux.Common.UI
{
    public class VampireUpgradeUIState : UIState
    {
        private UIDraggablePanel panel;
        private UIText header;
        private UIText xpLine;
        private UIText pointsLine;

        private UITextButton dmgBtn, spdBtn, lscBtn, lsaBtn, focusBtn, targetBtn, closeBtn;

        public override void OnInitialize()
        {
            panel = new UIDraggablePanel();
            panel.SetPadding(12);

            float w = 460f;
            float h = 360f;
            panel.Width.Set(w, 0f);
            panel.Height.Set(h, 0f);

            // True center with pixel offsets (this is the fix)
            panel.Left.Set(-w / 2f, 0.5f);
            panel.Top.Set(-h / 2f, 0.5f);

            header = new UIText("Vampire Summon Upgrades");
            header.Top.Set(0, 0);
            panel.Append(header);

            xpLine = new UIText("");
            xpLine.Top.Set(36, 0);
            panel.Append(xpLine);

            pointsLine = new UIText("");
            pointsLine.Top.Set(60, 0);
            panel.Append(pointsLine);

            float y = 100;

            dmgBtn = MakeBtn(y, () => ClickBuy(UpgradeType.Damage)); y += 40;
            spdBtn = MakeBtn(y, () => ClickBuy(UpgradeType.Speed)); y += 40;
            lscBtn = MakeBtn(y, () => ClickBuy(UpgradeType.LifestealChance)); y += 40;
            lsaBtn = MakeBtn(y, () => ClickBuy(UpgradeType.LifestealAmount)); y += 40;
            focusBtn = MakeBtn(y, () => ClickBuy(UpgradeType.FocusSameTarget)); y += 40;

            targetBtn = MakeBtn(y + 10, () => ClickBuy(UpgradeType.ToggleTargetingMode));
            panel.Append(targetBtn);

            closeBtn = new UITextButton("Close");
            closeBtn.Width.Set(120, 0);
            closeBtn.Height.Set(34, 0);
            closeBtn.Left.Set(-130, 1f);
            closeBtn.Top.Set(-44, 1f);
            closeBtn.OnLeftClick += (_, __) => VampireUpgradeUISystem.Toggle();
            panel.Append(closeBtn);

            Append(panel);
        }

        private UITextButton MakeBtn(float top, System.Action onClick)
        {
            var b = new UITextButton("...");
            b.Width.Set(-20, 1f);
            b.Height.Set(34, 0);
            b.Left.Set(10, 0);
            b.Top.Set(top, 0);
            b.OnLeftClick += (_, __) => onClick();
            panel.Append(b);
            return b;
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);

            Player p = Main.LocalPlayer;

            // auto-close if not holding staff anymore
            if (!(p.HeldItem?.ModItem is VampireKnives))
            {
                VampireUpgradeUISystem.Toggle();
                return;
            }

            var mp = p.GetModPlayer<VampireSummonReduxPlayer>();
            var cfg = Terraria.ModLoader.ModContent.GetInstance<VampireSummonReduxConfig>();

            xpLine.SetText($"Level: {mp.Level}    XP: {mp.XP}/{mp.XPToNextLevel()}");
            pointsLine.SetText($"Upgrade Points: {mp.UpgradePoints}");

            dmgBtn.SetText(Label("Damage", mp.DamageRank, mp.UpgradeCost(mp.DamageRank),
                                 "Increases minion damage (bonus capped by progression)."));

            spdBtn.SetText(Label("Speed", mp.SpeedRank, mp.UpgradeCost(mp.SpeedRank),
                                 "Speeds up movement/attack responsiveness."));

            lscBtn.SetText(Label("Lifesteal Chance", mp.LifestealChanceRank, mp.UpgradeCost(mp.LifestealChanceRank),
                                 "Chance to heal you on hit."));

            lsaBtn.SetText(Label("Lifesteal Amount", mp.LifestealAmountRank, mp.UpgradeCost(mp.LifestealAmountRank),
                                 "Increases heal amount when lifesteal triggers."));

            focusBtn.SetText(Label("Focus Target (ticks)", mp.FocusSameTargetRank, mp.UpgradeCost(mp.FocusSameTargetRank),
                                   "Keeps the minion committed to the same target longer."));

            targetBtn.SetText($"Target Mode: {mp.TargetMode} (click to toggle)");
        }

        private string Label(string name, int rank, int cost, string desc)
        => $"{name}  |  Rank: {rank}  |  Cost: {cost}\n{desc}";

        private void ClickBuy(UpgradeType type)
        {
            // Singleplayer: apply directly.
            // Multiplayer: tell server to apply.
            if (Main.netMode == Terraria.ID.NetmodeID.SinglePlayer)
            {
                var mp = Main.LocalPlayer.GetModPlayer<VampireSummonReduxPlayer>();
                mp.TryBuyUpgrade(type);
            }
            else
            {
                VampireSummonReduxNet.SendBuyUpgrade(Main.myPlayer, type);
            }
        }
    }
}
