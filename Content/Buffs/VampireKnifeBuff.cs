using Terraria;
using Terraria.ModLoader;

namespace VampireSummonRedux.Content.Buffs
{
    public class VampireKnifeBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Keep the buff alive as long as at least one knife exists.
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Content.Projectiles.VampireKnifeMinion>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
