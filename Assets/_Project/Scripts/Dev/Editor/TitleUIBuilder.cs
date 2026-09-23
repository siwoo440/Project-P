using System.IO;
using ProjectP.Title;
using ProjectP.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// 01_Title의 UI를 코드로 조립하는 에디터 메뉴. (메뉴: Project P > Build Title UI)
    ///
    /// 다시 실행하면 [TitleUI]와 EventSystem을 지우고 새로 만든다. 씬에서 직접 고친 배치는 사라지므로,
    /// 한 번 만든 뒤에는 씬에서 직접 다듬고 이 메뉴는 구조를 처음부터 다시 만들 때만 쓴다.
    ///
    /// 함께 처리하는 것:
    /// - TextMeshPro 필수 리소스가 없으면 가져온다.
    /// - 한글 표시용 fallback 폰트를 TMP 설정에 등록한다. Windows에 설치된 맑은 고딕을 참조만 하고
    ///   폰트 파일은 프로젝트에 포함하지 않는다. 개발용 임시 조치이며 출시 전에 배포 가능한 폰트로 교체한다.
    /// </summary>
    public static class TitleUIBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/01_Title.unity";
        private const string RootName = "[TitleUI]";
        private const string FontFolder = "Assets/_Project/Art/Fonts";
        private const string KoreanFallbackPath = FontFolder + "/KoreanFallback_MalgunGothic.asset";

        // 기준 해상도. 기획서 10.1이 공란이라 레거시 옵션표 기본값을 쓴다.
        private static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        private static readonly Color BackgroundColor = new Color32(20, 22, 28, 255);
        private static readonly Color PanelColor = new Color32(37, 42, 51, 255);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);

        [MenuItem("Project P/Build Title UI")]
        public static void Build()
        {
            if (!EnsureTmpEssentials()) return;
            EnsureKoreanFallback();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == RootName || root.GetComponent<EventSystem>() != null) Object.DestroyImmediate(root);
            }

            BuildEventSystem();
            BuildTitleUI();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[TitleUIBuilder] 01_Title UI 생성 완료");
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Project P", "01_Title UI를 만들었습니다.", "확인");
        }

        // ---------------------------------------------------------------- 준비

        private static bool EnsureTmpEssentials()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            var path = package == null
                ? null
                : Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");

            if (path == null || !File.Exists(path))
            {
                Debug.LogError("[TitleUIBuilder] TMP 필수 리소스 패키지를 찾지 못했습니다. " +
                               "Window > TextMeshPro > Import TMP Essential Resources를 직접 실행한 뒤 다시 시도하세요.");
                return false;
            }

            AssetDatabase.ImportPackage(path, false);
            AssetDatabase.Refresh();
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

            Debug.LogWarning("[TitleUIBuilder] TMP 필수 리소스를 가져오는 중입니다. 끝나면 메뉴를 다시 실행하세요.");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Project P", "TextMeshPro 필수 리소스를 가져왔습니다.\n가져오기가 끝나면 메뉴를 한 번 더 실행하세요.", "확인");
            }

            return false;
        }

        private static void EnsureKoreanFallback()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFallbackPath);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 90);
                if (font == null)
                {
                    Debug.LogWarning("[TitleUIBuilder] 맑은 고딕을 찾지 못해 한글 fallback을 등록하지 못했습니다. 한글이 네모로 표시됩니다.");
                    return;
                }

                if (!AssetDatabase.IsValidFolder(FontFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Fonts");

                font.name = Path.GetFileNameWithoutExtension(KoreanFallbackPath);
                AssetDatabase.CreateAsset(font, KoreanFallbackPath);

                // 머티리얼과 아틀라스를 하위 에셋으로 저장해야 참조가 유지된다.
                if (font.material != null)
                {
                    font.material.name = font.name + " Material";
                    AssetDatabase.AddObjectToAsset(font.material, font);
                }

                for (var i = 0; i < font.atlasTextures.Length; i++)
                {
                    if (font.atlasTextures[i] == null) continue;
                    font.atlasTextures[i].name = $"{font.name} Atlas {i}";
                    AssetDatabase.AddObjectToAsset(font.atlasTextures[i], font);
                }

                AssetDatabase.SaveAssets();
            }

            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            var serialized = new SerializedObject(settings);
            var fallbacks = serialized.FindProperty("m_fallbackFontAssets");
            if (fallbacks == null)
            {
                Debug.LogWarning("[TitleUIBuilder] TMP 설정의 fallback 목록을 찾지 못했습니다. " +
                                 $"Project Settings > TextMesh Pro > Settings의 Fallback Font Assets에 {KoreanFallbackPath}를 직접 추가하세요.");
                return;
            }

            for (var i = 0; i < fallbacks.arraySize; i++)
            {
                if (fallbacks.GetArrayElementAtIndex(i).objectReferenceValue == font) return;
            }

            fallbacks.arraySize++;
            fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[TitleUIBuilder] 한글 fallback 폰트를 TMP 설정에 등록했습니다.");
        }

        // ---------------------------------------------------------------- 조립

        private static void BuildEventSystem()
        {
            // 프로젝트가 새 Input System 전용이라 StandaloneInputModule 대신 InputSystemUIInputModule을 쓴다.
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void BuildTitleUI()
        {
            var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var screen = root.AddComponent<TitleScreen>();

            Stretch(CreateImage("Background", root.transform, BackgroundColor).rectTransform);

            var title = CreateText("TitleText", root.transform, "PROJECT P", 120);
            title.fontStyle = FontStyles.Bold;
            PlaceTop(title.rectTransform, new Vector2(1200, 160), 180);

            var menu = CreateVerticalGroup("Menu", root.transform, 24);
            Place(menu, new Vector2(520, 96 * 4 + 24 * 3), new Vector2(0, -80));

            var newGame = CreateButton("NewGameButton", menu, "새 게임", 44);
            var continueGame = CreateButton("ContinueButton", menu, "이어하기", 44);
            var settingsButton = CreateButton("SettingsButton", menu, "설정", 44);
            var quit = CreateButton("QuitButton", menu, "게임 종료", 44);

            var dialog = BuildConfirmDialog(root.transform);
            var settingsPanel = BuildSettingsPanel(root.transform);

            Wire(screen,
                ("newGameButton", newGame),
                ("continueButton", continueGame),
                ("settingsButton", settingsButton),
                ("quitButton", quit),
                ("confirmDialog", dialog),
                ("settingsPanel", settingsPanel));

            dialog.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
        }

        private static ConfirmDialog BuildConfirmDialog(Transform parent)
        {
            var dim = CreateImage("ConfirmDialog", parent, DimColor);
            Stretch(dim.rectTransform);
            var dialog = dim.gameObject.AddComponent<ConfirmDialog>();

            var panel = CreateImage("Panel", dim.transform, PanelColor);
            Place(panel.rectTransform, new Vector2(820, 380), Vector2.zero);

            var message = CreateText("Message", panel.transform, "", 40);
            PlaceTop(message.rectTransform, new Vector2(740, 180), 40);

            var buttons = CreateHorizontalGroup("Buttons", panel.transform, 40);
            PlaceBottom(buttons, new Vector2(560, 96), 40);
            var yes = CreateButton("YesButton", buttons, "예", 40);
            var no = CreateButton("NoButton", buttons, "아니오", 40);

            Wire(dialog, ("messageText", message), ("yesButton", yes), ("noButton", no));
            return dialog;
        }

        private static SettingsPanel BuildSettingsPanel(Transform parent)
        {
            var dim = CreateImage("SettingsPanel", parent, DimColor);
            Stretch(dim.rectTransform);
            var settingsPanel = dim.gameObject.AddComponent<SettingsPanel>();

            var panel = CreateImage("Panel", dim.transform, PanelColor);
            Place(panel.rectTransform, new Vector2(960, 620), Vector2.zero);

            var header = CreateText("Header", panel.transform, "설정", 56);
            PlaceTop(header.rectTransform, new Vector2(880, 80), 40);

            var rows = CreateVerticalGroup("Rows", panel.transform, 30);
            Place(rows, new Vector2(840, 80 * 3 + 30 * 2), new Vector2(0, 10));
            var (master, masterValue) = CreateVolumeRow("Master", rows, "마스터 볼륨");
            var (bgm, bgmValue) = CreateVolumeRow("Bgm", rows, "배경음");
            var (sfx, sfxValue) = CreateVolumeRow("Sfx", rows, "효과음");

            var close = CreateButton("CloseButton", panel.transform, "닫기", 40);
            PlaceBottom((RectTransform)close.transform, new Vector2(320, 96), 40);

            Wire(settingsPanel,
                ("masterSlider", master), ("bgmSlider", bgm), ("sfxSlider", sfx),
                ("masterValueText", masterValue), ("bgmValueText", bgmValue), ("sfxValueText", sfxValue),
                ("closeButton", close));
            return settingsPanel;
        }

        private static (Slider slider, TMP_Text value) CreateVolumeRow(string name, Transform parent, string label)
        {
            var row = CreateHorizontalGroup(name + "Row", parent, 24);
            var group = row.GetComponent<HorizontalLayoutGroup>();
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            var labelText = CreateText("Label", row, label, 36);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            SetPreferredSize(labelText.gameObject, 240, 80);

            var sliderObject = DefaultControls.CreateSlider(UiResources());
            sliderObject.name = "Slider";
            sliderObject.transform.SetParent(row, false);
            SetPreferredSize(sliderObject, 460, 30);
            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;

            var valueText = CreateText("Value", row, "0", 36);
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            SetPreferredSize(valueText.gameObject, 90, 80);

            return (slider, valueText);
        }

        // ---------------------------------------------------------------- 공통 도우미

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            text.text = content;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, float fontSize)
        {
            var go = TMP_DefaultControls.CreateButton(TmpResources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var text = go.GetComponentInChildren<TMP_Text>();
            text.text = label;
            text.fontSize = fontSize;
            return go.GetComponent<Button>();
        }

        private static RectTransform CreateVerticalGroup(string name, Transform parent, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var group = go.GetComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            return (RectTransform)go.transform;
        }

        private static RectTransform CreateHorizontalGroup(string name, Transform parent, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var group = go.GetComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            return (RectTransform)go.transform;
        }

        private static void SetPreferredSize(GameObject go, float width, float height)
        {
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void PlaceTop(RectTransform rect, Vector2 size, float margin)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0, -margin);
        }

        private static void PlaceBottom(RectTransform rect, Vector2 size, float margin)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0, margin);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        /// <summary>private [SerializeField] 필드를 이름으로 연결한다. 이름이 틀리면 바로 예외를 던진다.</summary>
        private static void Wire(Object target, params (string field, Object value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in references)
            {
                var property = serialized.FindProperty(field);
                if (property == null)
                {
                    throw new System.InvalidOperationException($"{target.GetType().Name}.{field} 필드를 찾을 수 없습니다.");
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_DefaultControls.Resources TmpResources() => new TMP_DefaultControls.Resources
        {
            standard = BuiltinSprite("UI/Skin/UISprite.psd"),
            background = BuiltinSprite("UI/Skin/Background.psd"),
            inputField = BuiltinSprite("UI/Skin/InputFieldBackground.psd"),
            knob = BuiltinSprite("UI/Skin/Knob.psd"),
            checkmark = BuiltinSprite("UI/Skin/Checkmark.psd"),
            dropdown = BuiltinSprite("UI/Skin/DropdownArrow.psd"),
            mask = BuiltinSprite("UI/Skin/UIMask.psd")
        };

        private static DefaultControls.Resources UiResources() => new DefaultControls.Resources
        {
            standard = BuiltinSprite("UI/Skin/UISprite.psd"),
            background = BuiltinSprite("UI/Skin/Background.psd"),
            inputField = BuiltinSprite("UI/Skin/InputFieldBackground.psd"),
            knob = BuiltinSprite("UI/Skin/Knob.psd"),
            checkmark = BuiltinSprite("UI/Skin/Checkmark.psd"),
            dropdown = BuiltinSprite("UI/Skin/DropdownArrow.psd"),
            mask = BuiltinSprite("UI/Skin/UIMask.psd")
        };

        private static Sprite BuiltinSprite(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
    }
}
