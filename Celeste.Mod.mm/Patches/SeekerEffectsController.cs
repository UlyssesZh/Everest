using Monocle;
using MonoMod;

namespace Celeste {
    public class patch_SeekerEffectsController : SeekerEffectsController {

        [MonoModLinkTo("Monocle.Entity", "Added")]
        [MonoModIgnore]
        public extern void base_Added(Scene scene);

        // In vanilla, this just sets music layer 3 to 0f.
        // Since music layer 3 is otherwise untouched by SeekerEffectsController, this is most likely the remains of an incompletely implemented feature.
        // This means that when saving and reentering any of the mirror temple seeker rooms causes layer 3 to become disabled when it shouldn't be.
        [MonoModReplace]
        public override void Added(Scene scene) {
            base_Added(scene);
        }

    }
}
