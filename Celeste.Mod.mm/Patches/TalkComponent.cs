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
            // use the same collision as the TalkComponent entity
            // (see https://github.com/EverestAPI/Everest/issues/759)

            private float alpha;
            private float slide;
            private float timer;

            [MonoModConstructor]
            [MonoModIgnore]
            public patch_TalkComponentUI(TalkComponent handler) : base(handler) {
                // no-op. MonoMod ignores this - we only need this to make the compiler happy.
            }

            [MonoModLinkTo("Monocle.Entity", "Awake")]
            [MonoModIgnore]
            public extern void base_Awake(Scene scene);
            [MonoModReplace]
            public override void Awake(Scene scene) {
                base_Awake(scene);
                if (Handler.Entity == null || Scene.CollideCheck<FakeWall>(new Rectangle((int)(Handler.Entity.X + Handler.Bounds.X), (int)(Handler.Entity.Y + Handler.Bounds.Y), Handler.Bounds.Width, Handler.Bounds.Height))) {
                    alpha = 0f;
                }
            }

            [MonoModLinkTo("Monocle.Entity", "Update")]
            [MonoModIgnore]
            public extern void base_Update();
            [MonoModReplace]
            public override void Update() {
                timer += Engine.DeltaTime;
                slide = Calc.Approach(slide, Display ? 1 : 0, Engine.DeltaTime * 4f);
                if (alpha < 1f && Handler.Entity != null && !Scene.CollideCheck<FakeWall>(new Rectangle((int)(Handler.Entity.X + Handler.Bounds.X), (int)(Handler.Entity.Y + Handler.Bounds.Y), Handler.Bounds.Width, Handler.Bounds.Height))) {
                    alpha = Calc.Approach(alpha, 1f, 2f * Engine.DeltaTime);
                }
                base_Update();
            }
        }
    }
}
