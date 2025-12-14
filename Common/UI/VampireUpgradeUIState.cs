using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

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

        private UITextButton dmgBtn, spdBtn, lscBtn, lsaBtn, focusBtn, ifrBtn, targetBtn, refundBtn, closeBtn;

        private const float BtnTextScale = 0.60f;

        // --- Display constants (match current upgrade logic) ---
        private const int DamagePerRank = 2;
        private const int LifestealChancePerRankPercent = 2;  // your minion uses rank*2%
        private const int LifestealBaseHeal = 1;              // your minion uses 1 + rank
        private const int LifestealHealPerRank = 1;

        // Immunity frames = local NPC hit cooldown tuning (lower is stronger)
        private const int ImmunityBaseCooldown = 18;
        private const int ImmunityCooldownDownPerRank = 2;
        private const int ImmunityMinCooldown = 6;

        public override void OnInitialize()
        {
            panel = new UIDraggablePanel();
            panel.SetPadding(12);

            float w = 520f;
            float h = 460f;

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
            focusBtn = MakeBtn(y, rowH); y += rowH + gap;

            // NEW: Immunity frames upgrade button row
            ifrBtn = MakeBtn(y, rowH); y += rowH + gap;

            targetBtn = MakeBtn(y, rowH); y += rowH + gap;

            // Bottom row
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

            // Click behavior
            dmgBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.Damage);
            spdBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.Speed);
            lscBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.LifestealChance);
            lsaBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.LifestealAmount);
            focusBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.FocusSameTarget);

            // NEW: Immunity frames
            ifrBtn.OnLeftClick += (_, __) => TryBuy(UpgradeType.ImmunityFrames);

            targetBtn.OnLeftClick += (_, __) => ToggleTargetMode();

            // Hover descriptions (uses your UITextButton hover callback fields)
            HookDesc(dmgBtn, "Damage: +2 bonus damage per rank (bonus is capped by progression).");
            HookDesc(spdBtn, "Speed: improves dash/handling (match your minion AI tuning).");
            HookDesc(lscBtn, "Lifesteal chance: increases % chance to heal on hit.");
            HookDesc(lsaBtn, "Lifesteal amount: increases how much HP you heal when lifesteal triggers.");
            HookDesc(focusBtn, "Focus: keeps knives committed to the same target longer.");

            HookDesc(ifrBtn, "Immunity: lowers local NPC hit cooldown (hits connect more often).");

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
            if (descText != null)
                descText.SetText(text);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Player p = Main.LocalPlayer;

            // Close the UI if not holding the summon
            if (!(p.HeldItem?.ModItem is VampireKnives))
            {
                VampireUpgradeUISystem.Toggle();
                return;
            }

            UpdateTextAndButtons();
        }

        // --- helpers for more informative UI ---
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

        private int GetCost(VampireSummonReduxPlayer mp, UpgradeType type)
        {
            // Keep your existing economy callsite.
            // If your config field names differ, update these two field names accordingly.
            var cfg = ModContent.GetInstance<VampireSummonRedux.Common.Config.VampireSummonReduxConfig>();

            int rank =
            type == UpgradeType.Damage ? mp.DamageRank :
            type == UpgradeType.Speed ? mp.SpeedRank :
            type == UpgradeType.LifestealChance ? mp.LifestealChanceRank :
            type == UpgradeType.LifestealAmount ? mp.LifestealAmountRank :
            type == UpgradeType.FocusSameTarget ? mp.FocusSameTargetRank :
            type == UpgradeType.ImmunityFrames ? mp.ImmunityRank :
            0;

            return cfg.BaseUpgradeCost + cfg.UpgradeCostPerRank * rank;
        }

        private void UpdateTextAndButtons()
        {
            Player p = Main.LocalPlayer;
            var mp = p.GetModPlayer<VampireSummonReduxPlayer>();

            pointsLine.SetText($"Upgrade Points: {mp.UpgradePoints}");
            xpLine.SetText($"Level: {mp.Level} | XP: {mp.XP}");

            int dmgCost = GetCost(mp, UpgradeType.Damage);
            int spdCost = GetCost(mp, UpgradeType.Speed);
            int lscCost = GetCost(mp, UpgradeType.LifestealChance);
            int lsaCost = GetCost(mp, UpgradeType.LifestealAmount);
            int focCost = GetCost(mp, UpgradeType.FocusSameTarget);
            int ifrCost = GetCost(mp, UpgradeType.ImmunityFrames);

            // --- computed display values ---
            int dmgTotal = mp.DamageRank * DamagePerRank;

            int lscPercent = mp.LifestealChanceRank * LifestealChancePerRankPercent; // matches your minion: rank*2%
            string lscFrac = ChanceAsFraction(lscPercent);

            int healAmount = LifestealBaseHeal + mp.LifestealAmountRank * LifestealHealPerRank;

            int hitCd = Math.Max(ImmunityMinCooldown, ImmunityBaseCooldown - mp.ImmunityRank * ImmunityCooldownDownPerRank);

            // --- button labels (include exact per-rank + cumulative) ---
            dmgBtn.SetText($"Damage | R:{mp.DamageRank} | Cost:{dmgCost} | +{DamagePerRank}/r (Tot +{dmgTotal})");
            spdBtn.SetText($"Speed | R:{mp.SpeedRank} | Cost:{spdCost}");
            lscBtn.SetText($"Lifesteal % | R:{mp.LifestealChanceRank} | Cost:{lscCost} | {lscPercent}% ({lscFrac})");
            lsaBtn.SetText($"Lifesteal + | R:{mp.LifestealAmountRank} | Cost:{lsaCost} | Heal {healAmount} HP");
            focusBtn.SetText($"Focus | R:{mp.FocusSameTargetRank} | Cost:{focCost}");
            ifrBtn.SetText($"I-Frames | R:{mp.ImmunityRank} | Cost:{ifrCost} | CD {hitCd}t");

            targetBtn.SetText($"Targeting: {mp.TargetMode}");

            // Enable/disable based on points
            dmgBtn.SetEnabled(mp.UpgradePoints >= dmgCost);
            spdBtn.SetEnabled(mp.UpgradePoints >= spdCost);
            lscBtn.SetEnabled(mp.UpgradePoints >= lscCost);
            lsaBtn.SetEnabled(mp.UpgradePoints >= lsaCost);
            focusBtn.SetEnabled(mp.UpgradePoints >= focCost);
            ifrBtn.SetEnabled(mp.UpgradePoints >= ifrCost);

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
