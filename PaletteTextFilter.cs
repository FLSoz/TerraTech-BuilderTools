using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BuilderTools
{
    class PaletteTextFilter : MonoBehaviour
    {
        static readonly FieldInfo m_UpdateGrid = BlockPicker.T_UIPaletteBlockSelect.GetField("m_UpdateGrid", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo SetUIInputMode = typeof(ManInput).GetMethod("SetUIInputMode", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        public static readonly Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();

        public static readonly Font ExoRegular = fonts.First(f => f.name == "Exo-Regular");
        public static readonly Sprite TEXT_FIELD_VERT_LEFT = sprites.First(f => f.name.Contains("TEXT_FIELD_VERT_LEFT"));

        public static bool clearOnCollapse = true;

        static UIInputMode mode;
        static bool wasFocused = false;
        static InputField inputField;
        static RectTransform inputFieldRect; 
        static UIPaletteBlockSelect blockPalette;

        static string filter = "";
        public static bool BlockFilterFunction(BlockTypes blockType)
        {
            if (filter == "") return true;
            var blockName = StringLookup.GetItemName(ObjectTypes.Block, (int)blockType).ToLower();
            return blockName.Contains(filter.ToLower());
        }

        static void OnTextChanged(string text)
        {
            filter = text;
            m_UpdateGrid.SetValue(blockPalette, true);
        }

        public static void Init(UIPaletteBlockSelect palette)
        {
            blockPalette = palette;
            var inputFieldGo = DefaultControls.CreateInputField(new DefaultControls.Resources() {
                inputField = TEXT_FIELD_VERT_LEFT
            });

            inputField = inputFieldGo.GetComponent<InputField>();
            inputField.onValueChanged.AddListener(OnTextChanged);

            var image = inputFieldGo.GetComponent<Image>();
            image.color = new Color(0.2745f, 0.2745f, 0.2745f);

            var textColor = new Color(0.4666f, 0.7529f, 1f);

            foreach (var text in inputFieldGo.GetComponentsInChildren<Text>())
            {
                text.text = "";
                text.alignment = TextAnchor.MiddleLeft;
                text.font = ExoRegular;
                text.fontSize = 20;
                text.fontStyle = FontStyle.Italic;
                text.color = textColor;
                text.lineSpacing = 1;
            }

            inputField.placeholder.enabled = true;
            var placeholderText = inputField.placeholder.GetComponent<Text>();
            placeholderText.text = "Block name";
            placeholderText.color = new Color(0.6784f, 0.6784f, 0.6784f);

            inputField.transform.SetParent(blockPalette.transform.Find("HUD_BlockPainting_BG"), false);
            var rect = inputFieldGo.GetComponent<RectTransform>();
            rect.pivot = rect.anchorMax = rect.anchorMin = new Vector2(1, 1);
            rect.anchoredPosition3D = new Vector3(-5, -5, 77);
            rect.sizeDelta = new Vector2(210, 40);

            var scrollviewRect = blockPalette.transform.Find("HUD_BlockPainting_BG/Scroll View") as RectTransform;
            var anchoredPosition3D = scrollviewRect.anchoredPosition3D;
            anchoredPosition3D.y -= 40;
            scrollviewRect.anchoredPosition3D = anchoredPosition3D;
            var sizeDelta = scrollviewRect.sizeDelta;
            sizeDelta.y -= 40;
            scrollviewRect.sizeDelta = sizeDelta;

            var scrollbarRect = blockPalette.transform.Find("HUD_BlockPainting_BG/Scrollbar") as RectTransform;
            anchoredPosition3D = scrollbarRect.anchoredPosition3D;
            anchoredPosition3D.y -= 40;
            scrollbarRect.anchoredPosition3D = anchoredPosition3D;
            sizeDelta = scrollbarRect.sizeDelta;
            sizeDelta.y -= 40;
            scrollbarRect.sizeDelta = sizeDelta;

            inputFieldRect = rect;

            Singleton.Manager<ManGameMode>.inst.ModeSwitchEvent.Subscribe(OnModeChange);
        }

        private void Update()
        {
            HandleInputFieldFocus();
        }

        internal static void HandleInputFieldFocus()
        {
            if (inputField)
            {
                if (inputField.isFocused)
                {
                    if (!wasFocused)
                    {
                        wasFocused = true;
                        mode = ManInput.inst.GetCurrentUIInputMode();
                        Singleton.Manager<ManInput>.inst.SetControllerMapsForUI(ManUI.inst, true, UIInputMode.FullscreenUI);
                        SetUIInputMode.Invoke(ManInput.inst, new object[] { mode, UIInputMode.FullscreenUI });
                    }
                }
                else if (wasFocused)
                {
                    wasFocused = false;
                    Singleton.Manager<ManInput>.inst.SetControllerMapsForUI(ManUI.inst, true, UIInputMode.BlockBuilding);
                    SetUIInputMode.Invoke(ManInput.inst, new object[] { UIInputMode.FullscreenUI, UIInputMode.BlockBuilding });
                }
            }
        }

        public static bool PreventPause()
        {
            return !(!inputField || inputField.isFocused);
        }

        internal static void OnPaletteCollapse(bool collapse)
        {
            if (clearOnCollapse && collapse)
            {
                ClearInput();
            } 
        }

        internal static void OnModeChange()
        {
            ClearInput();
        }

        static void ClearInput()
        {
            if(inputField) inputField.text = "";
        }
    }
}
