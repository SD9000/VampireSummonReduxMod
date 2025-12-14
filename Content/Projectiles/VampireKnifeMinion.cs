using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using VampireSummonRedux.Common.Players;

namespace VampireSummonRedux.Content.Projectiles
{
    public class VampireKnifeMinion : ModProjectile
    {
        // --- States ---
        private const int StateIdle = 0;
        private const int StateAttack = 1;

        // --- Tunables (baseline intentionally a bit slower) ---
        private const float IdleLerp = 0.18f;
        private const float IdleMaxSpeed = 10f;

        private const float AttackApproachSpeed = 12f;   // pre-dash tighten
        private const float AttackDashSpeed = 18f;       // stab pass speed
        private const int AttackDashTicks = 14;          // length of each pass
        private const int AttackRetargetDelay = 10;      // small delay after attack finishes

        // Sprite alignment offsets
        private const float IdleRotation = 0f;           // you rotated PNG; idle should not rotate
        private const float AttackRotationOffset = 0f;   // keep 0 unless sprite alignment needs offset

        // Orbit rotation speed
        private const float OrbitSpeed = 0.035f;

        // Attack search range
        private const float TargetRange = 850f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;

            Projectile.DamageType = DamageClass.Summon;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12; // will be overwritten each tick by ImmunityRank

            Projectile.timeLeft = 2;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }

            var mp = owner.GetModPlayer<VampireSummonReduxPlayer>();

            // keepalive via buff (adjust buff name if yours differs)
            // If you handle keepalive elsewhere, this is still safe.
            if (owner.dead)
            {
                // If you want: owner.ClearBuff(ModContent.BuffType<Buffs.VampireKnifeBuff>());
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            // Apply immunity upgrade each tick
            Projectile.localNPCHitCooldown = mp.GetLocalHitCooldownTicks();

            // Apply speed plateau (gentle scaling, avoids "too fast to read")
            float spd01 = mp.GetSpeedPlateau01(); // 0..~1
            float idleSpeed = IdleMaxSpeed * (1f + 0.20f * spd01);
            float approachSpeed = AttackApproachSpeed * (1f + 0.25f * spd01);
            float dashSpeed = AttackDashSpeed * (1f + 0.25f * spd01);

            // Find target
            NPC target = AcquireTarget(owner, mp);

            // State machine
            int state = (int)Projectile.ai[0];
            int timer = (int)Projectile.ai[1];

            if (state == StateIdle)
            {
                // idle visuals
                Projectile.rotation = IdleRotation;

                Vector2 idlePos = GetIdlePosition(owner, GetMinionIndex(owner));
                MoveTowards(idlePos, idleSpeed, IdleLerp);

                // if we have a target and we're allowed to attack, enter attack
                if (target != null && timer <= 0)
                {
                    Projectile.ai[0] = StateAttack;
                    Projectile.ai[1] = 0;

                    // localAI[0] = phase (0 or 1), localAI[1] = dash direction sign
                    Projectile.localAI[0] = 0f;
                    Projectile.localAI[1] = 1f;
                    Projectile.netUpdate = true;
                }
                else
                {
                    if (timer > 0) Projectile.ai[1] = timer - 1;
                }
            }
            else // StateAttack
            {
                if (target == null || !target.active || target.friendly || target.lifeMax <= 5 || target.type == NPCID.TargetDummy)
                {
                    // fallback to idle if target vanished
                    Projectile.ai[0] = StateIdle;
                    Projectile.ai[1] = AttackRetargetDelay;
                    Projectile.netUpdate = true;
                    return;
                }

                // Tighten to target before dashing, so they "stick" like Blade Staff
                Vector2 toTarget = target.Center - Projectile.Center;
                float dist = toTarget.Length();
                if (dist > 220f)
                {
                    Vector2 desired = target.Center - Vector2.Normalize(toTarget) * 180f;
                    MoveTowards(desired, approachSpeed, 0.25f);
                }

                // Dash pass logic: two passes (forward/back), flipping rotation 180 each pass
                int dashT = timer;
                int phase = (int)Projectile.localAI[0]; // 0 or 1
                float sign = Projectile.localAI[1];     // 1 or -1

                // Compute dash direction each tick so it stays tight on fast-moving enemies
                Vector2 dir = target.Center - Projectile.Center;
                if (dir.LengthSquared() < 0.001f)
                    dir = Vector2.UnitX;

                dir.Normalize();

                // Face target but flipped 180 (your request)
                Projectile.rotation = dir.ToRotation() + MathHelper.Pi + AttackRotationOffset;

                // Move through target along facing direction
                Projectile.velocity = dir * (dashSpeed * sign);

                dashT++;

                // At end of a dash pass:
                if (dashT >= AttackDashTicks)
                {
                    dashT = 0;
                    phase++;

                    if (phase >= 2)
                    {
                        // finish attack, go idle with small delay
                        Projectile.velocity *= 0.2f;
                        Projectile.ai[0] = StateIdle;
                        Projectile.ai[1] = AttackRetargetDelay;

                        Projectile.localAI[0] = 0f;
                        Projectile.localAI[1] = 1f;
                        Projectile.netUpdate = true;
                        return;
                    }

                    // flip sign (stab back the other way)
                    sign *= -1f;
                    Projectile.localAI[0] = phase;
                    Projectile.localAI[1] = sign;
                }

                Projectile.ai[1] = dashT;
            }

            // small damping to prevent jitter at very low speeds
            if (Projectile.velocity.LengthSquared() < 0.05f)
                Projectile.velocity *= 0.85f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            var mp = owner.GetModPlayer<VampireSummonReduxPlayer>();

            // Ignore dummies entirely for lifesteal etc.
            if (target.type == NPCID.TargetDummy)
                return;

            // Lifesteal chance (0..100 capped)
            int chance = mp.GetLifestealChancePercent();
            if (chance <= 0)
                return;

            if (Main.rand.Next(100) >= chance)
                return;

            // Lifesteal amount = percent of damage
            float pct = mp.GetLifestealHealPercent(); // e.g. 0.06 = 6%
            if (pct <= 0f)
                return;

            int heal = (int)Math.Ceiling(damageDone * pct);
            if (heal <= 0)
                return;

            owner.statLife = Utils.Clamp(owner.statLife + heal, 0, owner.statLifeMax2);
            owner.HealEffect(heal, broadcast: true);

            // Vanilla Vampire Knives heal visual
            if (Main.myPlayer == owner.whoAmI)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                                         owner.Center,
                                         Vector2.Zero,
                                         ProjectileID.VampireHeal,
                                         0,
                                         0f,
                                         owner.whoAmI,
                                         owner.whoAmI,
                                         heal
                );
            }
        }

        // ---------- Targeting ----------
        private NPC AcquireTarget(Player owner, VampireSummonReduxPlayer mp)
        {
            // Player-designated minion target (right click / summon targeting)
            int forced = owner.MinionAttackTargetNPC;
            if (forced >= 0 && forced < Main.maxNPCs)
            {
                NPC n = Main.npc[forced];
                if (IsValidTarget(n))
                    return n;
            }

            float best = TargetRange * TargetRange;
            NPC bestNpc = null;

            Vector2 origin = (mp.TargetMode == TargetingMode.ClosestToPlayer)
            ? owner.Center
            : Projectile.Center;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!IsValidTarget(n))
                    continue;

                float d = Vector2.DistanceSquared(origin, n.Center);
                if (d < best)
                {
                    best = d;
                    bestNpc = n;
                }
            }

            return bestNpc;
        }

        private static bool IsValidTarget(NPC n)
        {
            if (n == null || !n.active) return false;
            if (n.friendly) return false;
            if (n.dontTakeDamage) return false;
            if (n.lifeMax <= 5) return false;
            if (n.type == NPCID.TargetDummy) return false;
            return true;
        }

        // ---------- Idle Orbit ----------
        private Vector2 GetIdlePosition(Player owner, int index)
        {
            int count = GetMinionCount(owner);
            if (count <= 0) count = 1;

            // Stable anchor point above the player's head (no slope wobble)
            Vector2 center = owner.Center + new Vector2(0f, -48f);

            // Orbit phase
            float t = Main.GameUpdateCount * OrbitSpeed;
            float angle = t + MathHelper.TwoPi * (index / (float)count);

            // Depth illusion: -1..1 (front/back)
            float depth = (float)Math.Sin(angle);

            // Radius scaling by minion count (packs tighter, but clamps to avoid huge halo)
            float baseRx = 22f;
            float addPerKnife = 2.5f;

            float rx = baseRx + (count - 1) * addPerKnife;
            rx = MathHelper.Clamp(rx, 18f, 36f);

            float ry = MathHelper.Clamp(rx * 0.28f, 6f, 12f);

            Vector2 offset = new Vector2((float)Math.Cos(angle) * rx, depth * ry);

            // Visual depth: front = bigger + less transparent
            float depth01 = (depth + 1f) * 0.5f; // 0..1
            Projectile.scale = 0.85f + 0.30f * depth01;
            Projectile.alpha = (int)(120f - 90f * depth01);

            return center + offset;
        }

        private int GetMinionCount(Player owner)
        {
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == owner.whoAmI && p.type == Type)
                    count++;
            }
            return count;
        }

        private int GetMinionIndex(Player owner)
        {
            // Deterministic ordering by whoAmI
            int idx = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.owner != owner.whoAmI || p.type != Type)
                    continue;

                if (p.whoAmI == Projectile.whoAmI)
                    return idx;

                idx++;
            }
            return 0;
        }

        // ---------- Movement helper ----------
        private void MoveTowards(Vector2 destination, float maxSpeed, float lerp)
        {
            Vector2 to = destination - Projectile.Center;
            float dist = to.Length();

            if (dist < 4f)
            {
                Projectile.velocity *= 0.85f;
                return;
            }

            Vector2 desired = to / dist * Math.Min(maxSpeed, dist);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, lerp);
        }
    }
}
