using Microsoft.Xna.Framework;

namespace Celeste.Mod.Entities {
    [CustomEntity("everest/dialogTrigger", "dialog/dialogtrigger", "cavern/dialogtrigger")]
    public class DialogCutsceneTrigger : Trigger {

        private string dialogEntry;
        private bool triggered;
        private EntityID id;
        private bool noRespawnAfterUse;
        private bool triggerOnlyOnce;
        private bool ignoreIntroState;
        private bool endLevel;
        private int deathCount;

        public DialogCutsceneTrigger(EntityData data, Vector2 offset, EntityID entId)
            : base(data, offset) {
            dialogEntry = data.Attr("dialogId");
            noRespawnAfterUse = data.Bool("onlyOnce", true); // don't rename the EntityData name for backwards compat
            triggerOnlyOnce = data.Bool("triggerOnlyOnce", true);
            ignoreIntroState = data.Bool("ignoreIntroState", false);
            endLevel = data.Bool("endLevel", false);
            deathCount = data.Int("deathCount", -1);
            triggered = false;
            id = entId;
        }

        public override void OnStay(Player player) {
            if (Scene is not Level level)
                return;

            if (triggered)
                return;

            if (deathCount >= 0 && level.Session.DeathsInCurrentLevel != deathCount)
                return;

            if (ignoreIntroState && ((patch_Player) player).IsIntroState)
                return;

            triggered = true;

            Scene.Add(new DialogCutscene(dialogEntry, player, endLevel));

            if (noRespawnAfterUse) {
                // can't remove the flag, some maps might depend on it
                level.Session.SetFlag("DoNotLoad" + id, true);
                level.Session.DoNotLoad.Add(id);
            }
        }

        public override void OnLeave(Player player) {
            if (!triggerOnlyOnce)
                triggered = false;
        }

    }
}
