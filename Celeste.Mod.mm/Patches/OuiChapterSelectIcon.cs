#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a #nullable annotations context
#pragma warning disable SYSLIB1045 // Use GeneratedRegexAttribute to generate the regular expression implementation at compile time.

using Celeste.Mod;
using Monocle;
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste {
    public class patch_OuiChapterSelectIcon : OuiChapterSelectIcon {

        private class AnimatedMapIconMeta {
            public float? FrontFPS { get; set; }
            public float? BackFPS { get; set; }

            public string? FrontFrames { get; set; }
            public string? BackFrames { get; set; }
        }

        private const string DetectionPath = "AnimatedMapIcons";

        private bool isAnimated;

        private List<MTexture> FrontTextures;
        private List<MTexture> BackTextures;

        private MTexture front;
        private MTexture back;

        private int[] FrontOrder;
        private int[] BackOrder;

        private float FrontFPS;
        private float BackFPS;

        private int FrontFrame = 0;
        private int BackFrame = 0;

        private float FrontFrameTimer = 0f;
        private float BackFrameTimer = 0f;

        // We're effectively in OuiChapterSelectIcon, but still need to "expose" private fields to our mod.
        private bool hidden;
        public bool IsHidden => hidden;

        private bool selected;
        public bool IsSelected => selected;

        public patch_OuiChapterSelectIcon(int area, MTexture front, MTexture back)
            : base(area, front, back) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

        public extern void orig_ctor(int area, MTexture front, MTexture back);
        [MonoModConstructor]
        public void ctor(int area, MTexture front, MTexture back) {
            orig_ctor(area, front, back);

            // Don't bother if its not in the path, perhaps replace with an alternative check for a .meta.yaml file?
            if (!front.AtlasPath.Contains(DetectionPath))
                return;

            isAnimated = true;

            // Remove the frame suffex.
            string frontPath = Regex.Replace(front.AtlasPath, "\\d+$", string.Empty);
            string backPath = frontPath + "_back";

            FrontTextures = GFX.Gui.GetAtlasSubtextures(frontPath);

            // Only grab the back textures if applicable. otherwise default to the front textures.
            if (((patch_Atlas)GFX.Gui).HasAtlasSubtextures(backPath))
                BackTextures = GFX.Gui.GetAtlasSubtextures(backPath);
            else if (GFX.Gui.Has(backPath))
                BackTextures = new List<MTexture>() { GFX.Gui[backPath] };
            else
                BackTextures = FrontTextures;

            FrontOrder = Enumerable.Range(0, FrontTextures.Count).ToArray();
            BackOrder = Enumerable.Range(0, BackTextures.Count).ToArray();

            this.front = FrontTextures[0];
            this.back = BackTextures[0];

            FrontFPS = 12f;
            BackFPS = 12f;

            // Read in data from a .meta.yaml file with the same name as the map icon (excluding the frame suffex).
            if (!Everest.Content.Map.TryGetValue("Graphics/Atlases/Gui/" + frontPath + ".meta", out ModAsset value) || value.Type != typeof(AssetTypeYaml))
                return;

            AnimatedMapIconMeta meta;

            using (TextReader input = new StreamReader(value.Stream))
                meta = YamlHelper.Deserializer.Deserialize<AnimatedMapIconMeta>(input);

            // Back sprite should always default to the front sprite.
            FrontFPS = meta.FrontFPS ?? FrontFPS;
            BackFPS = meta.BackFPS ?? FrontFPS;

            if (meta.FrontFrames is not null)
                FrontOrder = Calc.ReadCSVIntWithTricks(meta.FrontFrames);

            if (meta.BackFrames is not null)
                BackOrder = Calc.ReadCSVIntWithTricks(meta.BackFrames);
            else if (meta.FrontFrames is not null &&
                BackTextures.Count == FrontTextures.Count) // Make sure to see if the counts line up.
                BackOrder = FrontOrder;
        }

        public extern void orig_Update();
        public override void Update() {
            orig_Update();

            // Just return if hidden or if its not animated.
            if (hidden || SaveData.Instance == null || !isAnimated)
                return;

            FrontFrameTimer -= Engine.DeltaTime;
            if (FrontFrameTimer < 0f) {
                FrontFrameTimer += 1f / FrontFPS;
                FrontFrame++;
                FrontFrame %= FrontOrder.Length;
                front = FrontTextures[FrontOrder[FrontFrame]];
            }

            BackFrameTimer -= Engine.DeltaTime;
            if (BackFrameTimer < 0f) {
                BackFrameTimer += 1f / BackFPS;
                BackFrame++;
                BackFrame %= BackOrder.Length;
                back = BackTextures[BackOrder[BackFrame]];
            }
        }
    }
    public static class OuiChapterSelectIconExt {

        [Obsolete("Use OuiChapterSelectIcon.IsHidden instead.")]
        public static bool GetIsHidden(this OuiChapterSelectIcon self)
            => ((patch_OuiChapterSelectIcon) self).IsHidden;

        [Obsolete("Use OuiChapterSelectIcon.IsHidden instead.")]
        public static bool GetIsSelected(this OuiChapterSelectIcon self)
            => ((patch_OuiChapterSelectIcon) self).IsSelected;

    }
}
