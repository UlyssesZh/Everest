using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Runtime.CompilerServices;

namespace Celeste.Editor {
    class patch_LevelTemplate : LevelTemplate {
        // expose this private field to our patch
        private static Color[] fgTilesColor;
        
        public patch_LevelTemplate(LevelData data)
            : base(data) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

        [MonoModConstructor]
        [MonoModIgnore] // we don't want to change anything in the method...
        [PatchTrackableStrawberryCheck] // except manipulating it with MonoModRules
        public extern void ctor(LevelData data);

        [AddLevelTemplateCulling]
        [PatchLevelTemplateRenderContents]
        [MonoModIgnore]
        public new extern void RenderContents(Camera camera, System.Collections.Generic.List<LevelTemplate> allLevels);

        [AddLevelTemplateCulling]
        [MonoModIgnore]
        public new extern void RenderOutline(Camera camera);

        [AddLevelTemplateCulling]
        [MonoModIgnore]
        public new extern void RenderHighlight(Camera camera, bool hovered, bool selected);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsVisible(Camera camera) {
            return CullHelper.IsRectangleVisible(X, Y, Width, Height, camera: camera);
        }

        private static Color GetEditorColor(int editorColorIndex) {
            // fallback for negative indices
            if (editorColorIndex < 0)
                return Color.White;
            
            // look it up in the list of colors if we can
            if (editorColorIndex < fgTilesColor.Length)
                return fgTilesColor[editorColorIndex];
            
            // else, unpack the color from the integer
            // should be in format 0x00RRGGBB (we must leave space for color indices, so we can't use all 32 bits with an alpha channel)
            uint packedColor = Convert.ToUInt32(editorColorIndex - fgTilesColor.Length);
            byte red = (byte) (packedColor >> 16 & 0xFF);
            byte green = (byte) (packedColor >> 8 & 0xFF);
            byte blue = (byte) (packedColor >> 0 & 0xFF);
            return new Color(red, green, blue);
        }
    }
}

namespace MonoMod {
    /// <summary>
    /// Patch a LevelTemplate method to add camera culling
    /// </summary>
    [MonoModCustomMethodAttribute(nameof(MonoModRules.AddLevelTemplateCulling))]
    class AddLevelTemplateCullingAttribute : Attribute { }

    /// <summary>
    /// Patch `LevelTemplate.RenderContent` to support custom template colors.
    /// </summary>
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchLevelTemplateRenderContents))]
    class PatchLevelTemplateRenderContents : Attribute { }

    static partial class MonoModRules {
        public static void AddLevelTemplateCulling(ILContext il, CustomAttribute attrib) {
            var cursor = new ILCursor(il);
            var label = cursor.DefineLabel();

            /*
            Add cull check at the start of the method:
            + if (!IsVisible(camera))
            +    return;
            */
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Call, il.Method.DeclaringType.FindMethod("System.Boolean IsVisible(Monocle.Camera)")!);
            cursor.Emit(OpCodes.Brtrue, label);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(label);
        }

        public static void PatchLevelTemplateRenderContents(ILContext il, CustomAttribute attrib) {
            /*
             * patch the `fgTilesColor` lookup to use `GetEditorColor` instead
             * ```diff
             * - Draw.Rect(X + solid.X, Y + solid.Y, solid.Width, solid.Height, Dummy ? dummyFgTilesColor : fgTilesColor[EditorColorIndex]);
             * + Draw.Rect(X + solid.X, Y + solid.Y, solid.Width, solid.Height, Dummy ? dummyFgTilesColor : GetEditorColor(EditorColorIndex));
             * ```
             */
            
            FieldDefinition f_LevelTemplate_EditorColorIndex = il.Method.DeclaringType.FindField("EditorColorIndex");
            MethodDefinition m_LevelTemplate_GetEditorColor = il.Method.DeclaringType.FindMethod("GetEditorColor");
            
            ILCursor cursor = new(il);
            
            /*
             * IL_0170: ldsfld valuetype [FNA]Microsoft.Xna.Framework.Color[] Celeste.Editor.LevelTemplate::fgTilesColor
             * IL_0175: ldarg.0
             * IL_0176: ldfld int32 Celeste.Editor.LevelTemplate::EditorColorIndex
             * IL_017b: ldelem [FNA]Microsoft.Xna.Framework.Color
             */
            cursor.GotoNext(MoveType.Before,
                instr => instr.MatchLdsfld("Celeste.Editor.LevelTemplate", "fgTilesColor"),
                instr => instr.MatchLdarg0(),
                instr => instr.MatchLdfld("Celeste.Editor.LevelTemplate", "EditorColorIndex"),
                instr => instr.MatchLdelemAny("Microsoft.Xna.Framework.Color"));
            cursor.RemoveRange(4);
            cursor.EmitLdarg0();
            cursor.EmitLdfld(f_LevelTemplate_EditorColorIndex);
            cursor.EmitCall(m_LevelTemplate_GetEditorColor);
        }
    }
}