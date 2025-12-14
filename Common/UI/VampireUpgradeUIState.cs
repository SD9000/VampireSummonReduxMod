using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;

using VampireSummonRedux.Common.Net;
using VampireSummonRedux.Common.Players;
using VampireSummonRedux.Content.Items;

namespace VampireSummonRedux.Common.UI
{
    public class VampireUpgradeUIState : UIState
    {
        private UIDraggablePanel panel;

        private UIText titleText;
        private UIText pointsLine;
        private UIText xpLine;
        private UIText descText;

        private UITextButton dmgBtn, spdBtn, lscBtn, lsaBtn, focusBtn, targetBtn, refundBtn, closeBtn;

        private const float TextScale = 0.5f;

        public override void OnInitialize()
        {
            panel = new UIDraggablePanel();
            panel.SetPadding(12);

            float w = 520f;
            float h = 440f;

            panel.Width.Set(w, 0f);
            panel.Height.Set(h, 0f);

            // Center it
            panel.Left.Set(-w / 2f, 0.5f);
            panel.Top.Set(-h / 2f, 0.5f);

            Append(panel);

            titleText = new UIText("Vampire Knives Upgrades", 0.7f);
            titleText.Left.Set(10, 0f);
            titleText.Top.Set(8, 0f);
            panel.Append(titleText);

            pointsLine = new UIText("", TextScale);
            pointsLine.Left.Set(10, 0f);
            pointsLine.Top.Set(40, 0f);
            panel.Append(pointsLine);

            xpLine = new UIText("", TextScale);
            xpLine.Left.Set(10, 0f);
            xpLine.Top.Set(60, 0f);
            panel.Append(xpLine);

            descText = new UIText("Hover an upgrade for details.", 0.6f);
            descText.Left.Set(10, 0f);
            descText.Top.Set(86, 0f);
            panel.Append(descText);

            // --- Buttons ---
            float y = 120f;
            float rowH = 34f;
            float gap = 6f;

            dmgBtn = MakeBtn(y, rowH); y += rowH + gap;
            spdBtn = MakeBtn(y, rowH); y += rowH + gap;
            lscBtn = MakeBtn(y, rowH); y += rowH + gap;
            lsaBtn = MakeBtn(y, rowH); y += rowH + gap;
            focusBtn = MakeBtn(y, rowH); y += rowH + gap;
            targetBtn = MakeBtn(y, rowH); y += rowH + gap;

            // bottom row
            refundBtn = new UITextButton("Refund Upgrades", TextScale);
            refundBtn.Width.Set(170, 0f);
            refundBtn.Height.Set(34, 0f);
            refundBtn.Left.Set(10, 0f);
            refundBtn.Top.Set(-44, 1f);
            refundBtn.OnLeftClick += (_, __) => ClickRefund();
            panel.Append(refundBtn);

            closeBtn = new UITextButton("Close", TextScale);
            closeBtn.Width.Set(120, 0f);
            closeBtn.Height.Set(34, 0f);
            closeBtn.Left.Set(-130, 1f);
            closeBtn.Top.Set(-44, 1f);
            closeBtn.OnLeftClick += (_, __) => VampireUpgradeUISystem.Toggle();
            panel.Append(closeBtn);

            // Wire click behavior
            dmgBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.Damage);
            spdBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.Speed);
            lscBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.LifestealChance);
            lsaBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.LifestealAmount);
            focusBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.FocusSameTarget);
            targetBtn.OnLeftClick += (_, __) => ToggleTargetMode();

            // Mouseover descriptions
            HookDesc(dmgBtn, "Increases minion damage (bonus capped by progression).");
            HookDesc(spdBtn, "Improves dash speed/handling and reduces attack cooldown.");
            HookDesc(lscBtn, "Chance to heal you when a knife hits an enemy.");
            HookDesc(lsaBtn, "How much you heal when lifesteal triggers.");
            HookDesc(focusBtn, "Keeps knives committed to the same target longer.");
            HookDesc(targetBtn, "Switch targeting: closest-to-player vs closest-to-minion.");
            HookDesc(refundBtn, "Refunds all spent upgrade points (keeps your level/XP).");
            HookDesc(closeBtn, "Closes this menu.");
        }

        private UITextButton MakeBtn(float topPx, float heightPx)
        {
            var b = new UITextButton("", TextScale);
            b.Width.Set(-24, 1f);
            b.Height.Set(heightPx, 0f);
            b.Left.Set(10, 0f);
            b.Top.Set(topPx, 0f);
            panel.Append(b);
            return b;
        }

        private void HookDesc(UIElement el, string desc)
        {
            el.OnMouseOver += (_, __) => SetDesc(desc);
            el.OnMouseOut += (_, __) => SetDesc("Hover an upgrade for details.");
        }

        private void SetDesc(string text)
        {
            descText?.SetText(text, 0.6f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var p = Main.LocalPlayer;

            // If player is no longer holding the summon, close the menu
            if (!(p.HeldItem?.ModItem is VampireKnives))
            {
                VampireUpgradeUISystem.Toggle();
                return;
            }

            UpdateTextAndButtons();
        }

        private void UpdateTextAndButtons()
        {
            var p = Main.LocalPlayer;
            var mp = p.GetModPlayer<VampireSummonReduxPlayer>();

            pointsLine.SetText($"Upgrade Points: {mp.UpgradePoints}", TextScale);

            // Adjust these if your XP/Level fields are named differently
            xpLine.SetText($"Level: {mp.Level} | XP: {mp.XP}", TextScale);

            // Costs: adjust if your cost function differs
            int dmgCost = mp.GetUpgradeCost(UpgradeType.Damage);
            int spdCost = mp.GetUpgradeCost(UpgradeType.Speed);
            int lscCost = mp.GetUpgradeCost(UpgradeType.LifestealChance);
            int lsaCost = mp.GetUpgradeCost(UpgradeType.LifestealAmount);
            int focCost = mp.GetUpgradeCost(UpgradeType.FocusSameTarget);

            dmgBtn.SetText(Label("Damage", mp.DamageRank, dmgCost), TextScale);
            spdBtn.SetText(Label("Speed", mp.SpeedRank, spdCost), TextScale);
            lscBtn.SetText(Label("Lifesteal %", mp.LifestealChanceRank, lscCost), TextScale);
            lsaBtn.SetText(Label("Lifesteal +", mp.LifestealAmountRank, lsaCost), TextScale);
            focusBtn.SetText(Label("Focus", mp.FocusSameTargetRank, focCost), TextScale);

            targetBtn.SetText($"Targeting: {mp.TargetMode}", TextScale);

            // Enable/disable buttons based on points
            dmgBtn.SetEnabled(mp.UpgradePoints >= dmgCost);
            spdBtn.SetEnabled(mp.UpgradePoints >= spdCost);
            lscBtn.SetEnabled(mp.UpgradePoints >= lscCost);
            lsaBtn.SetEnabled(mp.UpgradePoints >= lsaCost);
            focusBtn.SetEnabled(mp.UpgradePoints >= focCost);

            // Target toggle and refund always enabled
            targetBtn.SetEnabled(true);
            refundBtn.SetEnabled(true);
            closeBtn.SetEnabled(true);
        }

        private string Label(string name, int rank, int cost)
        => $"{name} | Rank: {rank} | Cost: {cost}";

        private void TryBuy(UpgradeType up)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.LocalPlayer.GetModPlayer<VampireSummonReduxPlayer>().TryBuyUpgrade(up);
            }
            else
            {
                VampireSummonReduxNet.SendBuyUpgrade(Main.myPlayer, up);
            }
        }

        private void ToggleTargetMode()
        {
            var mp = Main.LocalPlayer.GetModPlayer<VampireSummonReduxPlayer>();

            // Flip mode locally; server will be told by your existing sync logic
            mp.TargetMode = (mp.TargetMode == TargetingMode.ClosestToPlayer)
            ? TargetingMode.ClosestToMinion
            : TargetingMode.ClosestToPlayer;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                VampireSummonReduxNet.SendFullSyncRequest(Main.myPlayer);
        }

        private void ClickRefund()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.LocalPlayer.GetModPlayer<VampireSummonReduxPlayer>().RefundAllUpgrades();
            }
            else
            {
                VampireSummonReduxNet.SendRefund(Main.myPlayer);
            }
        }
    }
}
