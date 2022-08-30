using System;
using System.Collections;
using System.Collections.Generic;
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
        private static GameObject holder;
        internal static ModConfig config = new ModConfig();
        internal const string HarmonyID = "exund.buildertools";
        internal static Harmony harmony = new Harmony(HarmonyID);

        internal static bool kbdCategroryKeys = false;

        private static void SetupCOM()
        {
            var ontop = new Material(BuilderToolsContainer.Contents.FindAsset("OnTop") as Shader);
            var go = new GameObject();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = ontop;
            Texture2D texture = BuilderToolsContainer.Contents.FindAsset("CO_Icon") as Texture2D;
            mr.material.mainTexture = texture;

            var mf = go.AddComponent<MeshFilter>();
            Mesh mesh = null;
            foreach (UnityEngine.Object obj in BuilderToolsContainer.Contents.FindAllAssets("CO.obj"))
            {
                if (obj != null)
                {
                    if (obj is Mesh mesh1)
                    {
                        mesh = mesh1;
                        break;
                    }
                    else if (obj is GameObject gameObject)
                    {
                        mesh = gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
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
            go.transform.SetParent(holder.transform, false);

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
            try
            {
                holder = new GameObject();
                holder.AddComponent<BlockPicker>();

                UnityEngine.Object.DontDestroyOnLoad(holder);

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
                holder.AddComponent<PhysicsInfo>();
                holder.AddComponent<PaletteTextFilter>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
