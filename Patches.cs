﻿using System;
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
        [HarmonyPatch(typeof(ManPointer), "OnMouse")]
        private static class ManControllerTechBuilder_SpawnNewPaintingBlock
        {
            private static void Prefix()
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
                private static void Postfix(ref BlockTypes blockType, ref bool __result)
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
                private static void Postfix(ref UIPaletteBlockSelect __instance)
                {
                    PaletteTextFilter.Init(__instance);
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "Collapse")]
            private static class Collapse
            {
                private static void Postfix(ref bool __result)
                {
                    PaletteTextFilter.OnPaletteCollapse(__result);
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "Update")]
            private static class Update
            {
                private static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

                private static readonly FieldInfo
                    m_CategoryToggles = AccessTools.Field(typeof(UIPaletteBlockSelect), "m_CategoryToggles"),
                    m_Controller = AccessTools.Field(typeof(UICategoryToggles), "m_Controller"),
                    m_Entries = AccessTools.Field(typeof(UITogglesController), "m_Entries"),
                    m_Toggle = AccessTools.Inner(typeof(UITogglesController), "ToggleEntry").GetField("m_Toggle");

                private static readonly int Alpha1 = (int)KeyCode.Alpha1;

                private static void Prefix(ref UIPaletteBlockSelect __instance)
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
            private static bool Prefix()
            {
                return PaletteTextFilter.PreventPause();
            }
        }
    }
}
