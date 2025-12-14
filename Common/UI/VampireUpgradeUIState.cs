using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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

        private UITextButton dmgBtn;
        private UITextButton spdBtn;
        private UITextButton lscBtn;
        private UITextButton lsaBtn;
        private UITextButton ifrBtn;
        private UITextButton targetBtn;

        private UITextButton refundBtn;
        private UITextButton closeBtn;

        private const float BtnTextScale = 0.60f;

        public override void OnInitialize()
        {
            panel = new UIDraggablePanel();
            panel.SetPadding(12);

            float w = 560f;
            float h = 485f;

            panel.Width.Set(w, 0f);
            panel.Height.Set(h, 0f);

            panel.Left.Set(-w / 2f, 0.5f);
            panel.Top.Set(-h / 2f, 0.5f);

            Append(panel);

            titleText = new UIText("Vampire Knives Upgrades", 0.70f);
            titleText.Left.Set(10, 0f);
            titleText.Top.Set(8, 0f);
            panel.Append(titleText);

            pointsLine = new UIText("", BtnTextScale);
            pointsLine.Left.Set(10, 0f);
            pointsLine.Top.Set(40, 0f);
            panel.Append(pointsLine);

            xpLine = new UIText("", BtnTextScale);
            xpLine.Left.Set(10, 0f);
            xpLine.Top.Set(60, 0f);
            panel.Append(xpLine);

            descText = new UIText("Hover an upgrade for details.", 0.60f);
            descText.Left.Set(10, 0f);
            descText.Top.Set(86, 0f);
            panel.Append(descText);

            float y = 120f;
            float rowH = 34f;
            float gap = 6f;

            dmgBtn = MakeBtn(y, rowH); y += rowH + gap;
            spdBtn = MakeBtn(y, rowH); y += rowH + gap;
            lscBtn = MakeBtn(y, rowH); y += rowH + gap;
            lsaBtn = MakeBtn(y, rowH); y += rowH + gap;
            ifrBtn = MakeBtn(y, rowH); y += rowH + gap;
            targetBtn = MakeBtn(y, rowH); y += rowH + gap;

            refundBtn = new UITextButton("Refund Upgrades", BtnTextScale);
            refundBtn.Width.Set(190, 0f);
            refundBtn.Height.Set(34, 0f);
            refundBtn.Left.Set(10, 0f);
            refundBtn.Top.Set(-44, 1f);
            refundBtn.OnLeftClick += (_, __) => ClickRefund();
            panel.Append(refundBtn);

            closeBtn = new UITextButton("Close", BtnTextScale);
            closeBtn.Width.Set(120, 0f);
            closeBtn.Height.Set(34, 0f);
            closeBtn.Left.Set(-130, 1f);
            closeBtn.Top.Set(-44, 1f);
            closeBtn.OnLeftClick += (_, __) => VampireUpgradeUISystem.Toggle();
            panel.Append(closeBtn);

            // Click actions
            dmgBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.Damage);
            spdBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.Speed);
            lscBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.LifestealChance);
            lsaBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.LifestealAmount);
            ifrBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.ImmunityFrames);

            targetBtn.OnLeftClick += (_, __) => ToggleTargetMode();

            // Hover descriptions (short, readable; the button text contains exact numbers)
            HookDesc(dmgBtn, "Damage: +2 bonus minion damage per rank (bonus is capped by progression).");
            HookDesc(spdBtn, "Speed: improves movement feel; caps at 50 ranks with diminishing returns.");
            HookDesc(lscBtn, "Lifesteal chance: chance to heal on hit (capped at 100%).");
            HookDesc(lsaBtn, "Lifesteal amount: heals a % of damage dealt (starts tiny, caps slightly OP).");
            HookDesc(ifrBtn, "Immunity: lowers local NPC hit cooldown; minimum is 6 ticks.");
            HookDesc(targetBtn, "Switch targeting: closest-to-player vs closest-to-minion.");

            HookDesc(refundBtn, "Refunds all spent upgrade points (keeps your level/XP).");
            HookDesc(closeBtn, "Closes this menu.");
        }

        private UITextButton MakeBtn(float topPx, float heightPx)
        {
            var b = new UITextButton("", BtnTextScale);
            b.Width.Set(-24, 1f);
            b.Height.Set(heightPx, 0f);
            b.Left.Set(10, 0f);
            b.Top.Set(topPx, 0f);
            panel.Append(b);
            return b;
        }

        private void HookDesc(UITextButton btn, string desc)
        {
            btn.HoverDescription = desc;
            btn.OnHoverDescription = SetDesc;
        }

        private void SetDesc(string text)
        {
            descText?.SetText(text);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Player p = Main.LocalPlayer;

            // Close if not holding the summon weapon
            if (!(p.HeldItem?.ModItem is VampireKnives))
            {
                VampireUpgradeUISystem.Toggle();
                return;
            }

            UpdateTextAndButtons();
        }

        private static int GCD(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                int t = a % b;
                a = b;
                b = t;
            }
            return a == 0 ? 1 : a;
        }

        private static string ChanceAsFraction(int percent)
        {
            if (percent <= 0) return "0";
            int g = GCD(percent, 100);
            return $"{percent / g}/{100 / g}";
        }

        private void UpdateTextAndButtons()
        {
            Player p = Main.LocalPlayer;
            var mp = p.GetModPlayer<VampireSummonReduxPlayer>();

            pointsLine.SetText($"Upgrade Points: {mp.UpgradePoints}");
            xpLine.SetText($"Level: {mp.Level} | XP: {mp.XP}");

            // Costs are computed by the player, so UI stays consistent with backend
            int dmgCost = mp.GetUpgradeCost(UpgradeType.Damage);
            int spdCost = mp.GetUpgradeCost(UpgradeType.Speed);
            int lscCost = mp.GetUpgradeCost(UpgradeType.LifestealChance);
            int lsaCost = mp.GetUpgradeCost(UpgradeType.LifestealAmount);
            int ifrCost = mp.GetUpgradeCost(UpgradeType.ImmunityFrames);

            // Display values
            const int dmgPerRank = 2;
            int dmgTotal = mp.DamageRank * dmgPerRank;

            int lscPct = mp.GetLifestealChancePercent();
            string lscFrac = ChanceAsFraction(lscPct);

            float lsaPct = mp.GetLifestealHealPercent() * 100f;

            int cd = mp.GetLocalHitCooldownTicks();

            int plateauPct = (int)(mp.GetSpeedPlateau01() * 100f);

            // Button labels (compact but detailed)
            dmgBtn.SetText($"Damage | R:{mp.DamageRank} | Cost:{dmgCost} | +{dmgPerRank}/r (Tot +{dmgTotal})");
            spdBtn.SetText($"Speed | R:{mp.SpeedRank}/50 | Cost:{spdCost} | Plateau {plateauPct}%");
            lscBtn.SetText($"Lifesteal % | R:{mp.LifestealChanceRank} | Cost:{lscCost} | {lscPct}% ({lscFrac})");
            lsaBtn.SetText($"Lifesteal Amt | R:{mp.LifestealAmountRank} | Cost:{lsaCost} | {lsaPct:0.##}% dmg");
            ifrBtn.SetText($"I-Frames | R:{mp.ImmunityRank} | Cost:{ifrCost} | CD {cd}t");

            targetBtn.SetText($"Targeting: {mp.TargetMode}");

            // Enable/disable using backend rules (caps + cost + points)
            dmgBtn.SetEnabled(mp.CanBuyUpgrade(UpgradeType.Damage));
            spdBtn.SetEnabled(mp.CanBuyUpgrade(UpgradeType.Speed));
            lscBtn.SetEnabled(mp.CanBuyUpgrade(UpgradeType.LifestealChance));
            lsaBtn.SetEnabled(mp.CanBuyUpgrade(UpgradeType.LifestealAmount));
            ifrBtn.SetEnabled(mp.CanBuyUpgrade(UpgradeType.ImmunityFrames));

            targetBtn.SetEnabled(true);
            refundBtn.SetEnabled(true);
            closeBtn.SetEnabled(true);
        }

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
