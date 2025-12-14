using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using VampireSummonRedux.Common.Players;
using VampireSummonRedux.Common.Config;
using VampireSummonRedux.Common.Net;

namespace VampireSummonRedux.Content.Projectiles
{
    public class VampireKnifeMinion : ModProjectile
    {
        // --- AI tuning knobs ---
        private const float IdleInertia = 14f;          // higher = smoother/slower turns
        private const float ReturnSpeed = 14f;          // when too far from owner
        private const float IdleHoverRadius = 72f;      // spacing around player
        private const float TargetSearchRange = 900f;

        // Attack pattern (Blade Staff-ish): reposition near target, then dash through
        private const float EngageDistance = 360f;
        private const float StabStartDistance = 220f;
        private const float StabEndDistance = 40f;

        // Base dash / accel; speed upgrades add to these
        private const float BaseDashSpeed = 18f;
        private const float BaseAccel = 0.55f;

        // Cooldown between stabs; speed upgrades reduce it
        private const int BaseAttackCooldown = 32;

        // Focus target duration base; focus upgrades extend it
        private const int BaseFocusTicks = 25;
        private const int FocusTicksPerRank = 15;

        // States
        private const int StateIdle = 0;
        private const int StateDashForward = 1;
        private const int StateDashBack = 2;

        // ai[0] = state
        // ai[1] = attackTimer (counts down; when 0, can attack)
        // localAI[0] = focusedTargetWhoAmI + 1  (0 means none)
        // localAI[1] = focusTicksRemaining
        private int FocusedTarget
        {
            get => (int)Projectile.localAI[0] - 1;
            set => Projectile.localAI[0] = value + 1;
        }

        private int FocusTicks
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private float DashDot
        {
            get => Projectile.localAI[2];
            set => Projectile.localAI[2] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        public override string Texture => "VampireSummonRedux/Content/Projectiles/VampireKnife";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 14;

            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;

            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;

            // Custom AI
            Projectile.aiStyle = 0;

            // Keep-alive handled by buff
            Projectile.timeLeft = 2;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;

            // Let speed upgrades affect “feel”; start at 1 for smoothness like you had
            Projectile.extraUpdates = 1;
        }

        public override bool? CanCutTiles() => false;

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

        // -----------------------------
        // XP + lifesteal (unchanged)
        // -----------------------------
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
            Player owner = Main.player[Projectile.owner];
            var mp = owner.GetModPlayer<VampireSummonReduxPlayer>();
            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();

            if (cfg.GainXPOnHit && cfg.XpPerHit > 0)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    VampireSummonReduxNet.SendAddXp(owner.whoAmI, cfg.XpPerHit);
                else
                    mp.AddXP(cfg.XpPerHit);
            }

            bool killed = target.life <= 0;
            if (killed && cfg.GainXPOnKill && cfg.XpPerKill > 0)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    VampireSummonReduxNet.SendAddXp(owner.whoAmI, cfg.XpPerKill);
                else
                    mp.AddXP(cfg.XpPerKill);
            }

            int chancePercent = 2 + mp.LifestealChanceRank * 2; // base 2%
            int healAmount = 1 + mp.LifestealAmountRank;        // base 1

            if (chancePercent > 0 && Main.rand.Next(100) < chancePercent)
            {
                owner.statLife += healAmount;
                owner.statLife = Utils.Clamp(owner.statLife, 0, owner.statLifeMax2);
                owner.HealEffect(healAmount, broadcast: true);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            var mp = owner.GetModPlayer<VampireSummonReduxPlayer>();
            var cfg = ModContent.GetInstance<VampireSummonReduxConfig>();

            int bonus = mp.DamageRank * 2;

            bool postML = NPC.downedMoonlord;
            int cap = postML ? cfg.PostMoonLordBonusDamageCap : cfg.PreHardmodeBonusDamageCap;
            bonus = Utils.Clamp(bonus, 0, cap);

            modifiers.SourceDamage += bonus;
        }

        // -----------------------------
        // Custom Blade-Staff-like AI
        // -----------------------------
        public override void AI()
        {

            Player owner = Main.player[Projectile.owner];

            if (owner.dead || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            int buffType = ModContent.BuffType<Buffs.VampireKnifeBuff>();
            if (!owner.HasBuff(buffType))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.rotation = 0f;

            var mp = owner.GetModPlayer<VampireSummonReduxPlayer>();

            // Speed upgrade affects responsiveness + dash/cooldown.
            // Keep extraUpdates modest; it can get expensive in MP.
            Projectile.extraUpdates = 1 + (mp.SpeedRank >= 3 ? 1 : 0); // 1 normally, 2 after rank 3

            float dashSpeed = BaseDashSpeed + mp.SpeedRank * 1.2f;
            float accel = BaseAccel + mp.SpeedRank * 0.06f;

            int attackCooldown = BaseAttackCooldown - mp.SpeedRank * 2;
            if (attackCooldown < 14) attackCooldown = 14;

            int focusMax = BaseFocusTicks + mp.FocusSameTargetRank * FocusTicksPerRank;

            // Keep minionPos stable for nicer “slotting”
            Projectile.minionPos = GetMinionIndex(owner);

            // Reduce attack timer
            if (Projectile.ai[1] > 0f)
                Projectile.ai[1]--;

            // If too far from player, hard return (prevents getting lost offscreen)
            Vector2 idlePos = GetIdlePosition(owner, Projectile.minionPos);
            float distToOwnerIdle = Vector2.Distance(Projectile.Center, idlePos);

            if (distToOwnerIdle > 1600f)
            {
                Projectile.Center = idlePos;
                Projectile.velocity *= 0.1f;
                Projectile.netUpdate = true;
            }

            // Target selection (manual target > focused target > mode target)
            int targetWho = AcquireTarget(owner, mp, focusMax, out bool hasTarget);

            // State machine
            if (!hasTarget)
            {
                Projectile.ai[0] = StateIdle;
                FocusedTarget = -1;
                FocusTicks = 0;

                DoIdle(idlePos, accel);
                return;
            }

            NPC target = Main.npc[targetWho];

            // Maintain/refresh focus timer if we’re focusing something
            if (FocusedTarget == targetWho)
            {
                if (FocusTicks > 0) FocusTicks--;
            }
            else
            {
                // new focus
                FocusedTarget = targetWho;
                FocusTicks = focusMax;
                Projectile.netUpdate = true;
            }

            // Decide whether to dash or reposition
            float distToTarget = Vector2.Distance(Projectile.Center, target.Center);

            if (Projectile.ai[0] != StateDashForward && Projectile.ai[0] != StateDashBack)
            {
                // Reposition near the target before dashing
                Vector2 hoverNearTarget = target.Center + new Vector2(0, -60f);
                Vector2 toHover = hoverNearTarget - Projectile.Center;

                // If close enough and cooldown is ready, stab
                if (distToTarget <= StabStartDistance && Projectile.ai[1] <= 0f)
                {
                    Projectile.ai[0] = StateDashForward;
                    Projectile.ai[1] = attackCooldown;// reset cooldown now
                    Projectile.netUpdate = true;
                }
                else
                {
                    // Approach hover point / target area
                    float desiredSpeed = distToTarget > EngageDistance ? dashSpeed : (dashSpeed * 0.75f);
                    Vector2 desiredVel = toHover.SafeNormalize(Vector2.UnitY) * desiredSpeed;

                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, accel);
                }
            }

            if (Projectile.ai[0] == StateDashForward)
            {
                Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);

                // Commit to speed (less lerp = more hits)
                Projectile.velocity = dir * dashSpeed * 1.55f;

                float dot = Vector2.Dot(target.Center - Projectile.Center, Projectile.velocity);
                // dot flips sign when we pass the target
                if (DashDot != 0f && dot > DashDot)
                {
                    // passed target -> reverse dash
                    Projectile.velocity *= -1f;
                    Projectile.ai[0] = StateDashBack;
                    Projectile.netUpdate = true;
                }
                DashDot = dot;
            }
            else if (Projectile.ai[0] == StateDashBack)
            {
                // keep going away a moment, then return to idle
                if (Vector2.Distance(Projectile.Center, target.Center) > 160f || !IsValidTarget(target))
                {
                    Projectile.ai[0] = StateIdle;
                    DashDot = 0f;
                }
            }
        }

        private void DoIdle(Vector2 idlePos, float accel)
        {
            Vector2 toIdle = idlePos - Projectile.Center;
            float dist = toIdle.Length();

            // If we're basically at the idle spot, stop moving to prevent jitter.
            if (dist < 6f)
            {
                Projectile.velocity *= 0.85f;

                if (Projectile.velocity.Length() < 0.15f)
                {
                    Projectile.Center = idlePos;          // snap to exact pixel-ish position
                    Projectile.velocity = Vector2.Zero;
                }

                // Don't bob when settled (bobbing causes visible vibration)
                return;
            }

            float speed = dist > 300f ? ReturnSpeed : 10f;
            Vector2 desiredVel = toIdle.SafeNormalize(Vector2.Zero) * speed;

            // Smooth approach
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, 0.08f);

            // Optional: tiny bob only while moving (safe)
            Projectile.velocity.Y += (float)System.Math.Sin(Main.GameUpdateCount * 0.06f) * 0.02f;
        }

        private void FaceVelocity()
        {
            if (Projectile.velocity.LengthSquared() > 0.001f)
                Projectile.rotation = Projectile.velocity.ToRotation();
        }

        private Vector2 GetIdlePosition(Player owner, int index)
        {
            int count = GetMinionCount(owner);
            if (count <= 0) count = 1;

            Vector2 center = owner.MountedCenter + new Vector2(0f, -70f - owner.gfxOffY);

            float t = Main.GameUpdateCount * 0.055f; // orbit speed
            float angle = t + (MathHelper.TwoPi * index / count);

            // “Depth” (-1 back, +1 front)
            float depth = (float)System.Math.Sin(angle);

            // Horizontal orbit is big; vertical is small (gives the 3D-ish spin)
            float rx = 44f;
            float ry = 12f;

            Vector2 offset = new Vector2((float)System.Math.Cos(angle) * rx, depth * ry);

            // Visual depth illusion (bigger + more opaque when “front”)
            float depth01 = (depth + 1f) * 0.5f; // 0..1
            Projectile.scale = 0.85f + 0.30f * depth01;
            Projectile.alpha = (int)(120f - 90f * depth01); // back is more transparent

            return center + offset;
        }

        private int GetMinionIndex(Player owner)
        {
            // stable ordering: count active minions of same type, sorted by whoAmI
            int index = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == owner.whoAmI && p.type == Type)
                {
                    if (p.whoAmI == Projectile.whoAmI)
                        break;
                    index++;
                }
            }
            return index;
        }

        private int AcquireTarget(Player owner, VampireSummonReduxPlayer mp, int focusMaxTicks, out bool hasTarget)
        {
            hasTarget = false;

            // 1) Vanilla forced target (whips OR anything else setting MinionAttackTargetNPC)
            int forced = owner.MinionAttackTargetNPC;
            if (forced >= 0 && forced < Main.maxNPCs)
            {
                NPC n = Main.npc[forced];
                if (IsValidTarget(n) && Vector2.Distance(owner.Center, n.Center) <= TargetSearchRange)
                {
                    hasTarget = true;
                    return forced;
                }
            }

            // 2) Focused target (your “attack wish”)
            if (FocusedTarget >= 0 && FocusTicks > 0 && FocusTicks <= focusMaxTicks)
            {
                NPC n = Main.npc[FocusedTarget];
                if (IsValidTarget(n))
                {
                    hasTarget = true;
                    return FocusedTarget;
                }
            }

            // 3) Find target based on mode
            int best = -1;
            float bestDist = float.MaxValue;

            Vector2 origin = mp.TargetMode == TargetingMode.ClosestToMinion ? Projectile.Center : owner.Center;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!IsValidTarget(n)) continue;

                float d = Vector2.Distance(origin, n.Center);
                if (d < bestDist && d <= TargetSearchRange)
                {
                    bestDist = d;
                    best = i;
                }
            }

            if (best != -1)
            {
                hasTarget = true;
                return best;
            }

            return -1;
        }

        private bool IsValidTarget(NPC n)
        {
            if (n == null || !n.active)
                return false;

            // Don't attack target dummies
            if (n.type == NPCID.TargetDummy)
                return false;

            // Standard minion rules
            if (n.friendly || n.dontTakeDamage)
                return false;

            // Extra safety
            if (n.lifeMax <= 5)
                return false;

            return true;
        }
    }
}
