using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BuilderTools
{
    class BlockPicker : MonoBehaviour
    {
        internal static KeyCode block_picker_key = KeyCode.LeftShift;
        internal static bool open_inventory = false;
        internal static bool global_filters = true;

        static readonly BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        public static Type T_UIPaletteBlockSelect = typeof(UIPaletteBlockSelect);
        static readonly FieldInfo m_Grid = T_UIPaletteBlockSelect.GetField("m_Grid", flags);
        static readonly FieldInfo m_CategoryToggles = T_UIPaletteBlockSelect.GetField("m_CategoryToggles", flags);
        static readonly FieldInfo m_CorpToggles = T_UIPaletteBlockSelect.GetField("m_CorpToggles", flags);
        static readonly FieldInfo m_Controller = typeof(UICorpToggles).GetField("m_Controller", flags);

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && Input.GetKey(block_picker_key) && ManPlayer.inst.PaletteUnlocked)
            {
                UIPaletteBlockSelect palette = Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.BlockPalette) as UIPaletteBlockSelect;
                if (!palette.IsExpanded && open_inventory)
                {
                    var blockMenuSelection = Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.BlockMenuSelection) as UIBlockMenuSelection;
                    blockMenuSelection.Hide(new UIBlockMenuSelection.Context() { targetMode = UIBlockMenuSelection.ModeMask.BlockPaletteAndTechs });
                    blockMenuSelection.Show(new UIBlockMenuSelection.Context() { targetMode = UIBlockMenuSelection.ModeMask.BlockPalette });
                    palette.Expand(new UIShopBlockSelect.ExpandContext() { expandReason = UIShopBlockSelect.ExpandReason.Button });

                    try
                    {
                        Singleton.Manager<ManHUD>.inst.HideHudElement(ManHUD.HUDElementType.TechLoader);
                    }
                    catch { }
                }

                if (palette.IsExpanded)
                {
                    var temp_block = Singleton.Manager<ManPointer>.inst.targetVisible?.block;
                    if (temp_block)
                    {
                        var grid = m_Grid.GetValue(palette) as UIBlockSelectGrid;
                        grid.PreventSelection = false;

                        var catToggles = m_CategoryToggles.GetValue(palette) as UICategoryToggles;
                        var corpToggles = m_CorpToggles.GetValue(palette) as UICorpToggles;
                        var controller = m_Controller.GetValue(corpToggles) as UITogglesController;

                        if (global_filters)
                        {
                            catToggles.ToggleAllOn();
                            corpToggles.ToggleAllOn();
                        }
                        else
                        {
                            catToggles.SetToggleSelected((int)temp_block.BlockCategory, true);
                            controller.SetToggleSelected((int)Singleton.Manager<ManSpawn>.inst.GetCorporation(temp_block.BlockType), true);
                        }

                        grid.Repopulate();


                        palette.TrySelectBlockType(temp_block.BlockType);
                    }
                }
                Singleton.Manager<ManPointer>.inst.ChangeBuildMode(ManPointer.BuildingMode.PaintBlock);
            }
        }
    }
}
