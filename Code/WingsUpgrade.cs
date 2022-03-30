﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Reflection;
using MonoMod.Utils;

// still WIP, might eventually be cool


namespace Celeste.Mod.HonlyHelper
{
    [CustomEntity("HonlyHelper/WingsRefill")]
    public class WingsUpgrade : Entity

    {
        public int feathers;

        public WingsUpgrade(Vector2 position, bool oneUse, int feathers) : base(position)
        {
            base.Collider = new Hitbox(16f, 16f, -8f, -8f);
            base.Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            this.oneUse = oneUse;
            this.feathers = feathers;
            string str;
            str = "objects/HonlyHelper/WingsUpgrade/";
            this.p_shatter = Refill.P_Shatter;
            this.p_regen = Refill.P_Regen;
            this.p_glow = Refill.P_Glow;
            base.Add(this.outline = new Image(GFX.Game[str + "outline"]));
            this.outline.CenterOrigin();
            this.outline.Visible = false;
            base.Add(this.sprite = new Sprite(GFX.Game, str + "idle"));
            this.sprite.AddLoop("idle", "", 0.1f);
            this.sprite.Play("idle", false, false);
            this.sprite.CenterOrigin();
            base.Add(this.flash = new Sprite(GFX.Game, str + "flash"));
            this.flash.Add("flash", "", 0.05f);
            this.flash.OnFinish = delegate (string anim)
            {
                this.flash.Visible = false;
            };
            this.flash.CenterOrigin();
            base.Add(this.wiggler = Wiggler.Create(1f, 4f, delegate (float v)
            {
                this.sprite.Scale = (this.flash.Scale = Vector2.One * (1f + v * 0.2f));
            }, false, false));
            base.Add(new MirrorReflection());
            base.Add(this.bloom = new BloomPoint(0.8f, 16f));
            base.Add(this.light = new VertexLight(Color.White, 1f, 16, 48));
            base.Add(this.sine = new SineWave(0.6f, 0f));
            this.sine.Randomize();
            this.UpdateY();
            base.Depth = -100;
        }

        public WingsUpgrade(EntityData data, Vector2 offset) : this(data.Position + offset, data.Bool("oneUse", false), data.Int("feathers", 1))
        {
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            this.level = base.SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();
            bool flag = this.respawnTimer > 0f;
            if (flag)
            {
                this.respawnTimer -= Engine.DeltaTime;
                bool flag2 = this.respawnTimer <= 0f;
                if (flag2)
                {
                    this.Respawn();
                }
            }
            else
            {
                bool flag3 = base.Scene.OnInterval(0.1f);
                if (flag3)
                {
                    this.level.ParticlesFG.Emit(BadelineBoost.P_Ambience, 1, this.Position, Vector2.One * 5f);
                }
            }
            this.UpdateY();
            this.light.Alpha = Calc.Approach(this.light.Alpha, this.sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            this.bloom.Alpha = this.light.Alpha * 0.8f;
            bool flag4 = base.Scene.OnInterval(2f) && this.sprite.Visible;
            if (flag4)
            {
                this.flash.Play("flash", true, false);
                this.flash.Visible = true;
            }
        }

        private void Respawn()
        {
            bool flag = !this.Collidable;
            if (flag)
            {
                this.Collidable = true;
                this.sprite.Visible = true;
                this.outline.Visible = false;
                base.Depth = -100;
                this.wiggler.Start();
                Audio.Play(this.twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_return" : "event:/game/general/diamond_return", this.Position);
                this.level.ParticlesFG.Emit(BadelineBoost.P_Move, 16, this.Position, Vector2.One * 2f);
            }
        }

        private void UpdateY()
        {
            this.flash.Y = (this.sprite.Y = (this.bloom.Y = this.sine.Value * 2f));
        }

        public override void Render()
        {
            bool visible = this.sprite.Visible;
            if (visible)
            {
                this.sprite.DrawOutline(1);
            }
            base.Render();
        }

        private void OnPlayer(Player player)
        {
            //DynData<Player> selfData = new DynData<Player>(player);

            

            ActualWings wings = base.Scene.Tracker.GetEntity<ActualWings>();
            if(wings == null)
            {
                wings = new ActualWings(player.Position, feathers, player);
                base.Scene.Add(wings);
                DoTheThing(player);
            }
            else
            {
                if (wings.Refill(feathers))
                {
                    DoTheThing(player);
                }
            }

            //selfData["playerWings"] = wings;

            // the cool line of code
            //player.UseRefill(false);
            //CherryHelper.Session.HasShadowDash = true;
            
        }

        private void DoTheThing(Player player)
        {
            Audio.Play(this.twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_touch" : "event:/game/general/diamond_touch", this.Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            this.Collidable = false;
            base.Add(new Coroutine(this.RefillRoutine(player), true));
            this.respawnTimer = 2.5f;
        }

        private IEnumerator RefillRoutine(Player player)
        {
            Celeste.Freeze(0.05f);
            yield return null;
            this.level.Shake(0.3f);
            this.sprite.Visible = (this.flash.Visible = false);
            bool flag = !this.oneUse;
            if (flag)
            {
                this.outline.Visible = true;
            }
            this.Depth = 8999;
            yield return 0.05f;
            float angle = player.Speed.Angle();
            this.level.ParticlesFG.Emit(Refill.P_ShatterTwo, 5, this.Position, Vector2.One * 4f, angle - 1.5707964f);
            this.level.ParticlesFG.Emit(Refill.P_ShatterTwo, 5, this.Position, Vector2.One * 4f, angle + 1.5707964f);
            SlashFx.Burst(this.Position, angle);
            bool flag2 = this.oneUse;
            if (flag2)
            {
                this.RemoveSelf();
            }
            yield break;
        }

        public static ParticleType P_Shatter;

        public static ParticleType P_Regen;

        public static ParticleType P_Glow;

        public static ParticleType P_ShatterTwo;

        public static ParticleType P_RegenTwo;

        public static ParticleType P_GlowTwo;

        private Sprite sprite;

        private Sprite flash;

        private Image outline;

        private Wiggler wiggler;

        private BloomPoint bloom;

        private VertexLight light;

        private Level level;

        private SineWave sine;

        private bool twoDashes;

        private bool oneUse;

        private ParticleType p_shatter;

        private ParticleType p_regen;

        private ParticleType p_glow;

        private float respawnTimer;
    }
}
