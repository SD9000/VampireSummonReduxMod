using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace VampireSummonRedux.Content.Items
{
    public class VampireKnives : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Lets right-clicking on enemies (minion targeting) work nicely.
            ItemID.Sets.LockOnIgnoresCollision[Type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
        }

        public override void SetDefaults()
        {
            Item.damage = 15;
            Item.knockBack = 4f;
            Item.mana = 10;
            Item.width = 28;
            Item.height = 28;

            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.UseSound = SoundID.Item44;

            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 2);

            Item.shoot = ModContent.ProjectileType<Content.Projectiles.VampireKnifeMinion>();
            Item.buffType = ModContent.BuffType<Content.Buffs.VampireKnifeBuff>();
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Apply buff so the minion persists.
            player.AddBuff(Item.buffType, 2);

            // Spawn at the mouse cursor like most minion staves do.
            position = Main.MouseWorld;

            // velocity is irrelevant for aiStyle 169; just give a small nudge so it's not zero.
            if (velocity.LengthSquared() < 0.001f)
                velocity = Vector2.UnitY;

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.ShadowScale, 5)
                .AddIngredient(ItemID.HellstoneBar, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
