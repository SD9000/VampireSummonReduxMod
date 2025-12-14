using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

using VampireSummonRedux.Common.Players;

namespace VampireSummonRedux.Content.Items
{
    public partial class VampireKnives : ModItem
    {
        private static int GCD(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { int t = a % b; a = b; b = t; }
            return a == 0 ? 1 : a;
        }

        private static string ChanceAsFraction(int percent)
        {
            if (percent <= 0) return "0";
            int g = GCD(percent, 100);
            return $"{percent / g}/{100 / g}";
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player p = Main.LocalPlayer;
            var mp = p.GetModPlayer<VampireSummonReduxPlayer>();

            // Damage upgrade info (your minion uses +2 per rank in UI; keep consistent here)
            const int dmgPerRank = 2;
            int bonusDmg = mp.DamageRank * dmgPerRank;

            // Lifesteal
            int lsChance = mp.GetLifestealChancePercent();
            string lsFrac = ChanceAsFraction(lsChance);
            float lsAmtPct = mp.GetLifestealHealPercent() * 100f;

            // Immunity (local hit cooldown ticks)
            int cd = mp.GetLocalHitCooldownTicks();

            // Speed plateau
            int plateauPct = (int)(mp.GetSpeedPlateau01() * 100f);

            // Insert near the end but before "Material" lines if present
            int insertIndex = tooltips.Count;

            // Add a header line for clarity
            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Header", "— Upgrades —"));

            // Bonus damage (note: base item damage line won't change, so show bonus explicitly)
            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Damage",
                                                           bonusDmg > 0
                                                           ? $"Bonus minion damage: +{bonusDmg} ({mp.DamageRank} ranks)"
                                                           : "Bonus minion damage: +0"));

            // Lifesteal summary
            if (lsChance > 0 || lsAmtPct > 0f)
            {
                tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Lifesteal",
                                                               $"Lifesteal: {lsChance}% ({lsFrac}) chance • heals {lsAmtPct:0.##}% of damage"));
            }
            else
            {
                tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Lifesteal",
                                                               "Lifesteal: 0% chance • heals 0% of damage"));
            }

            // Immunity summary
            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Immunity",
                                                           mp.ImmunityRank > 0
                                                           ? $"Hit cooldown: {cd} ticks (local NPC immunity) ({mp.ImmunityRank} ranks)"
                                                           : $"Hit cooldown: {cd} ticks (local NPC immunity)"));

            // Speed summary (doesn't promise exact value because actual speed is applied in minion AI)
            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Speed",
                                                           mp.SpeedRank > 0
                                                           ? $"Speed: {mp.SpeedRank}/50 ranks • plateau {plateauPct}%"
                                                           : "Speed: 0/50 ranks"));

            // Targeting note (since you’ve got player/minion targeting)
            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Targeting",
                                                           $"Targeting mode: {mp.TargetMode}"));
        }
    }
}
