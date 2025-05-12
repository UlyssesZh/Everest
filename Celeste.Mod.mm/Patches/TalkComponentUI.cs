#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using Microsoft.Xna.Framework;
using Monocle;
using System;
using MonoMod;

namespace Celeste {
    class patch_TalkComponent : TalkComponent {

        [MonoModConstructor]
        [MonoModIgnore]
        public patch_TalkComponent(Rectangle bounds, Vector2 drawAt, Action<Player> onTalk, HoverDisplay hoverDisplay = null)
            : base(bounds, drawAt, onTalk, hoverDisplay) {
            // no-op. MonoMod ignores this - we only need this to make the compiler happy.
        }

        public class patch_TalkComponentUI : TalkComponentUI {
            private float alpha;
            private float slide;
            private float timer;

            [MonoModConstructor]
            [MonoModIgnore]
            public patch_TalkComponentUI(TalkComponent handler) : base(handler) {
                // no-op. MonoMod ignores this - we only need this to make the compiler happy.
            }

            public extern void orig_Awake(Scene scene);
            public override void Awake(Scene scene) {
                orig_Awake(scene);
                if (Handler.Entity == null || Scene.CollideCheck<FakeWall>(new Rectangle((int)(Handler.Entity.X + Handler.Bounds.X), (int)(Handler.Entity.Y + Handler.Bounds.Y), Handler.Bounds.Width, Handler.Bounds.Height))) {
                    alpha = 0f;
                }
            }

            public extern void orig_Update();
            public override void Update() {
                timer += Engine.DeltaTime;
                slide = Calc.Approach(slide, Display ? 1 : 0, Engine.DeltaTime * 4f);
                if (alpha < 1f && Handler.Entity != null && !Scene.CollideCheck<FakeWall>(new Rectangle((int)(Handler.Entity.X + Handler.Bounds.X), (int)(Handler.Entity.Y + Handler.Bounds.Y), Handler.Bounds.Width, Handler.Bounds.Height))) {
                    alpha = Calc.Approach(alpha, 1f, 2f * Engine.DeltaTime);
                }
                orig_Update();
            }
        }
    }
}
