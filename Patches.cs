using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;

namespace BuilderTools
{
    internal static class Patches
    {
        [HarmonyPatch(typeof(TankPreset.BlockSpec), "InitFromBlockState")]
        private static class BlockSpec_InitFromBlockState
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                var niv = codes.FindIndex(op => op.opcode == OpCodes.Newobj);
                codes[niv - 2].operand = typeof(TankBlock).GetProperty("cachedLocalPosition", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(false);
                codes[niv - 1] = new CodeInstruction(OpCodes.Nop);

                return codes;
            }
        }

        [HarmonyPatch(typeof(ManPointer), "OnMouse")]
        private static class ManControllerTechBuilder_SpawnNewPaintingBlock
        {
            static void Prefix()
            {
                if (Input.GetMouseButton(0) && Input.GetKey(BlockPicker.block_picker_key) && ManPlayer.inst.PaletteUnlocked)
                {
                    ManPointer.inst.ChangeBuildMode((ManPointer.BuildingMode)10);
                }
            }
        }

        private static class UIPaletteBlockSelect_Patches
        {
            [HarmonyPatch(typeof(UIPaletteBlockSelect), "BlockFilterFunction")]
            private static class BlockFilterFunction
            {
                static void Postfix(ref BlockTypes blockType, ref bool __result)
                {
                    if (__result)
                    {
                        __result = PaletteTextFilter.BlockFilterFunction(blockType);
                    }
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "OnPool")]
            private static class OnPool
            {
                static void Postfix(ref UIPaletteBlockSelect __instance)
                {
                    PaletteTextFilter.Init(__instance);
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "Collapse")]
            private static class Collapse
            {
                static void Postfix(ref bool __result)
                {
                    PaletteTextFilter.OnPaletteCollapse(__result);
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "Update")]
            private static class Update
            {
                private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

                private static readonly FieldInfo
                    m_CategoryToggles = typeof(UIPaletteBlockSelect).GetField("m_CategoryToggles", flags),
                    m_Controller = typeof(UICategoryToggles).GetField("m_Controller", flags),
                    m_Entries = typeof(UITogglesController).GetField("m_Entries", flags),
                    m_Toggle = typeof(UITogglesController).GetNestedType("ToggleEntry", BindingFlags.NonPublic).GetField("m_Toggle");

                private static readonly int Alpha1 = (int)KeyCode.Alpha1;

                static void Prefix(ref UIPaletteBlockSelect __instance)
                {
                    if (BuilderToolsMod.kbdCategroryKeys && __instance.IsExpanded && PaletteTextFilter.PreventPause())
                    {
                        var categoryToggles = (UICategoryToggles)m_CategoryToggles.GetValue(__instance);

                        int selected = -1;

                        var max = Alpha1 + categoryToggles.NumToggles;
                        for (int i = Alpha1; i < max; i++)
                        {
                            if (Input.GetKeyDown((KeyCode)i))
                            {
                                selected = i - Alpha1;
                            }
                        }

                        if (selected >= 0)
                        {
                            var controller = m_Controller.GetValue(categoryToggles);
                            var entries = (IList)m_Entries.GetValue(controller);
                            var toggle = (ToggleWrapper)m_Toggle.GetValue(entries[selected]);
                            categoryToggles.GetAllToggle().isOn = false;
                            categoryToggles.ToggleAllOff();

                            toggle.InvokeToggleHandler(true, false);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ManPauseGame), "TogglePauseMenu")]
        private static class ManPauseGame_TogglePauseMenu
        {
            static bool Prefix()
            {
                return PaletteTextFilter.PreventPause();
            }
        }
    }
}
