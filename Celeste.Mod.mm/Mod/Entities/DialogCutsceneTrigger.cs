using Microsoft.Xna.Framework;

namespace Celeste.Mod.Entities {
    [CustomEntity("everest/dialogTrigger", "dialog/dialogtrigger", "cavern/dialogtrigger")]
    public class DialogCutsceneTrigger : Trigger {

        private string dialogEntry;
        private bool triggered;
        private EntityID id;
        private bool onlyOnce;
        private bool endLevel;
        private int deathCount;

        public DialogCutsceneTrigger(EntityData data, Vector2 offset, EntityID entId)
            : base(data, offset) {
            dialogEntry = data.Attr("dialogId");
            onlyOnce = data.Bool("onlyOnce", true);
            endLevel = data.Bool("endLevel", false);
            deathCount = data.Int("deathCount", -1);
            triggered = false;
            id = entId;
        }

        public override void OnEnter(Player player) {
            if (Scene is not Level level)
                return;

            if (triggered || (deathCount >= 0 && level.Session.DeathsInCurrentLevel != deathCount))
                return;

            triggered = true;

            Scene.Add(new DialogCutscene(dialogEntry, player, endLevel));

            if (onlyOnce) {
                // can't remove the flag, some maps might depend on it
                level.Session.SetFlag("DoNotLoad" + id, true);
                level.Session.DoNotLoad.Add(id);
            }
        }

    }
}
