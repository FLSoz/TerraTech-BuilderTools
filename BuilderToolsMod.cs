using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Nuterra.NativeOptions;
using ModHelper;

namespace BuilderTools
{
    public class BuilderToolsMod : ModBase
    {
        private static GameObject _holder;
        internal static AssetBundle assetBundle;
        internal static ModConfig config = new ModConfig();
        internal static string asm_path = Assembly.GetExecutingAssembly().Location.Replace("BuilderTools.dll", "");
        internal const string HarmonyID = "exund.buildertools";
        internal static Harmony harmony = new Harmony(HarmonyID);

        internal static bool kbdCategroryKeys = false;

        private static void SetupCOM()
        {
            var ontop = new Material(assetBundle.LoadAsset<Shader>("OnTop"));
            var go = new GameObject();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = ontop;
            Texture2D texture = BuilderToolsContainer.Contents.FindAsset("CO_Icon") as Texture2D;
            mr.material.mainTexture = texture;
            mr.material.mainTexture.filterMode = FilterMode.Point;
            var mf = go.AddComponent<MeshFilter>();
            Mesh mesh = null;
            foreach (UnityEngine.Object obj in BuilderToolsContainer.Contents.FindAllAssets("CO.obj"))
            {
                if (obj != null)
                {
                    if (obj is Mesh)
                    {
                        mesh = (Mesh)obj;
                        break;
                    }
                    else if (obj is GameObject)
                    {
                        mesh = ((GameObject)obj).GetComponentInChildren<MeshFilter>().sharedMesh;
                        break;
                    }
                }
            }
            mf.sharedMesh = mf.mesh = mesh;

            var line = new GameObject();
            var lr = line.AddComponent<LineRenderer>();
            lr.startWidth = 0.5f;
            lr.endWidth = 0;
            lr.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });
            lr.useWorldSpace = true;
            lr.material = ontop;
            lr.material.color = lr.material.color.SetAlpha(1);
            line.transform.SetParent(go.transform, false);

            go.SetActive(false);
            go.transform.SetParent(_holder.transform, false);

            PhysicsInfo.COM = GameObject.Instantiate(go);
            var commat = PhysicsInfo.COM.GetComponent<MeshRenderer>().material;
            commat.color = Color.yellow;
            commat.renderQueue = 1;
            PhysicsInfo.COM.GetComponentInChildren<LineRenderer>().enabled = false;

            PhysicsInfo.COT = GameObject.Instantiate(go);
            PhysicsInfo.COT.transform.localScale *= 0.8f;
            var cotmat = PhysicsInfo.COT.GetComponent<MeshRenderer>().material;
            cotmat.color = Color.magenta;
            cotmat.renderQueue = 3;
            var COTlr = PhysicsInfo.COT.GetComponentInChildren<LineRenderer>();
            COTlr.material.renderQueue = 2;
            COTlr.startColor = COTlr.endColor = Color.magenta;

            PhysicsInfo.COL = GameObject.Instantiate(go);
            PhysicsInfo.COL.transform.localScale *= 0.64f;
            var colmat = PhysicsInfo.COL.GetComponent<MeshRenderer>().material;
            colmat.color = Color.cyan;
            colmat.renderQueue = 5;
            var COLlr = PhysicsInfo.COL.GetComponentInChildren<LineRenderer>();
            COLlr.startColor = COLlr.endColor = Color.cyan;
            COLlr.material.renderQueue = 4;
        }

        private static void Load()
        {
            assetBundle = AssetBundle.LoadFromFile(asm_path + "advancedbuilding.assetbundle");

            try
            {
                _holder = new GameObject();
                _holder.AddComponent<BlockPicker>();

                UnityEngine.Object.DontDestroyOnLoad(_holder);

                config.TryGetConfig<bool>("open_inventory", ref BlockPicker.open_inventory);
                config.TryGetConfig<bool>("global_filters", ref BlockPicker.global_filters);
                var key = (int)BlockPicker.block_picker_key;
                config.TryGetConfig<int>("block_picker_key", ref key);
                BlockPicker.block_picker_key = (KeyCode)key;

                config.TryGetConfig<bool>("clearOnCollapse", ref PaletteTextFilter.clearOnCollapse);

                var key2 = (int)PhysicsInfo.centers_key;
                config.TryGetConfig<int>("centers_key", ref key2);
                PhysicsInfo.centers_key = (KeyCode)key2;

                config.TryGetConfig<bool>("kbdCategroryKeys", ref kbdCategroryKeys);

                string modName = "Builder Tools";
                OptionKey blockPickerKey = new OptionKey("Block Picker activation key", modName, BlockPicker.block_picker_key);
                blockPickerKey.onValueSaved.AddListener(() =>
                {
                    BlockPicker.block_picker_key = blockPickerKey.SavedValue;
                    config["block_picker_key"] = (int)BlockPicker.block_picker_key;
                });

                OptionToggle globalFilterToggle = new OptionToggle("Block Picker - Use global filters", modName, BlockPicker.global_filters);
                globalFilterToggle.onValueSaved.AddListener(() =>
                {
                    BlockPicker.global_filters = globalFilterToggle.SavedValue;
                    config["global_filters"] = BlockPicker.global_filters;
                });

                OptionToggle openInventoryToggle = new OptionToggle("Block Picker - Automatically open the inventory when picking a block", modName, BlockPicker.open_inventory);
                openInventoryToggle.onValueSaved.AddListener(() =>
                {
                    BlockPicker.open_inventory = openInventoryToggle.SavedValue;
                    config["open_inventory"] = BlockPicker.open_inventory;
                });

                OptionToggle clearOnCollapse = new OptionToggle("Block Search - Clear filter when closing inventory", modName, PaletteTextFilter.clearOnCollapse);
                clearOnCollapse.onValueSaved.AddListener(() =>
                {
                    PaletteTextFilter.clearOnCollapse = clearOnCollapse.SavedValue;
                    config["clearOnCollapse"] = PaletteTextFilter.clearOnCollapse;
                });

                OptionKey centersKey = new OptionKey("Open physics info menu (Ctrl + ?)", modName, PhysicsInfo.centers_key);
                centersKey.onValueSaved.AddListener(() =>
                {
                    PhysicsInfo.centers_key = centersKey.SavedValue;
                    config["centers_key"] = (int)PhysicsInfo.centers_key;
                });

                OptionToggle enableKbdCategroryKeys = new OptionToggle("Use numerical keys (1-9) to select block category", modName, kbdCategroryKeys);
                enableKbdCategroryKeys.onValueSaved.AddListener(() =>
                {
                    kbdCategroryKeys = enableKbdCategroryKeys.SavedValue;
                    config["kbdCategroryKeys"] = kbdCategroryKeys;
                });

                NativeOptionsMod.onOptionsSaved.AddListener(() => { config.WriteConfigJsonFile(); });

                SetupCOM();
                _holder.AddComponent<PhysicsInfo>();
                _holder.AddComponent<PaletteTextFilter>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static float NumberField(float value)
        {
            var h = GUILayout.Height(25f);
            float.TryParse(GUILayout.TextField(value.ToString(CultureInfo.InvariantCulture), h), out float val);
            val = (float)Math.Round(val, 6);
            if (Math.Abs(val - value) > 1e-6)
            {
                GUI.changed = true;
            }

            return val;
        }

        public static float NumberField(float value, float interval)
        {
            var h = GUILayout.Height(25f);
            var w = GUILayout.Width(25f);

            float val;
            GUILayout.BeginHorizontal(h);
            {
                float.TryParse(GUILayout.TextField(value.ToString(CultureInfo.InvariantCulture), h), out val);
                if (GUILayout.Button("+", w, h))
                {
                    val += interval;
                }

                if (GUILayout.Button("-", w, h))
                {
                    val -= interval;
                }
            }
            GUILayout.EndHorizontal();

            val = (float)Math.Round(val, 6);
            if (Math.Abs(val - value) > 1e-6)
            {
                GUI.changed = true;
            }

            return val;
        }

        public static Vector3 Vector3Field(Vector3 value, float interval, Vector3 defaultValue, string additionalText, params GUILayoutOption[] options)
        {
            Vector3 ret;
            GUILayout.BeginVertical(options);
            {
                var x = value.x;
                var y = value.y;
                var z = value.z;

                GUILayout.Label($"X {additionalText}");
                x = NumberField(x, interval);

                GUILayout.Label($"Y {additionalText}");
                y = NumberField(y, interval);

                GUILayout.Label($"Z {additionalText}");
                z = NumberField(z, interval);

                ret = new Vector3(x, y, z);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Snap to closest"))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            ret[i] = Mathf.Round(ret[i] / interval) * interval;
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Reset"))
                    {
                        ret = defaultValue;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            return ret;
        }

        private static class Patches
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
                        if (kbdCategroryKeys && __instance.IsExpanded && PaletteTextFilter.PreventPause())
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

        private static bool Inited = false;
        private static ModContainer BuilderToolsContainer;

        public override void EarlyInit()
        {
            if (!Inited)
            {
                Dictionary<string, ModContainer> mods = (Dictionary<string, ModContainer>)AccessTools.Field(typeof(ManMods), "m_Mods").GetValue(Singleton.Manager<ManMods>.inst);
                if (mods.TryGetValue("BuilderTools", out ModContainer thisContainer))
                {
                    BuilderToolsContainer = thisContainer;
                }
                else
                {
                    Console.WriteLine("FAILED TO FETCH BuilderTools ModContainer");
                }
                Inited = true;
                Load();
            }
        }

        public override bool HasEarlyInit()
        {
            return true;
        }

        public override void DeInit()
        {
            harmony.UnpatchAll(HarmonyID);
        }

        public override void Init()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
