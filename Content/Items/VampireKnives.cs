using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

using VampireSummonRedux.Common.Players;
using VampireSummonRedux.Content.Projectiles;
using VampireSummonRedux.Content.Buffs; // <-- if your buff namespace differs, fix this line

namespace VampireSummonRedux.Content.Items
{
    public partial class VampireKnives : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Optional: staff-style hold (purely visual)
            Item.staff[Type] = true;

            ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            // This is what makes it “a weapon” and a summon weapon
            Item.damage = 20; // baseline; upgrades add bonus on the minion side
            Item.DamageType = DamageClass.Summon;

            Item.mana = 10;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;

            Item.noMelee = true;
            Item.knockBack = 2f;

            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.LightRed;

            // Summon wiring
            Item.buffType = ModContent.BuffType<VampireKnifeBuff>();      // <-- change if your buff class name differs
            Item.shoot = ModContent.ProjectileType<VampireKnifeMinion>(); // your minion projectile
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
                                   int type, int damage, float knockback)
        {
            // Apply buff so the minion persists
            player.AddBuff(Item.buffType, 2);

            // Spawn minion at cursor (standard summon behavior)
            position = Main.MouseWorld;

            Projectile.NewProjectile(
                source,
                position,
                Vector2.Zero,
                type,
                damage,
                knockback,
                player.whoAmI
            );

            // Prevent vanilla shooting behavior (we already spawned it)
            return false;
        }

        // -------------------- TOOLTIP (your existing code) --------------------

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

            const int dmgPerRank = 2;
            int bonusDmg = mp.DamageRank * dmgPerRank;

            int lsChance = mp.GetLifestealChancePercent();
            string lsFrac = ChanceAsFraction(lsChance);
            float lsAmtPct = mp.GetLifestealHealPercent() * 100f;

            int cd = mp.GetLocalHitCooldownTicks();
            int plateauPct = (int)(mp.GetSpeedPlateau01() * 100f);

            int insertIndex = tooltips.Count;

            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Header", "— Upgrades —"));

            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Damage",
                                                           bonusDmg > 0
                                                           ? $"Bonus minion damage: +{bonusDmg} ({mp.DamageRank} ranks)"
                                                           : "Bonus minion damage: +0"));

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

            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Immunity",
                                                           mp.ImmunityRank > 0
                                                           ? $"Hit cooldown: {cd} ticks (local NPC immunity) ({mp.ImmunityRank} ranks)"
                                                           : $"Hit cooldown: {cd} ticks (local NPC immunity)"));

            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Speed",
                                                           mp.SpeedRank > 0
                                                           ? $"Speed: {mp.SpeedRank}/50 ranks • plateau {plateauPct}%"
                                                           : "Speed: 0/50 ranks"));

            tooltips.Insert(insertIndex++, new TooltipLine(Mod, "VSR_Targeting",
                                                           $"Targeting mode: {mp.TargetMode}"));
        }
    }
}
