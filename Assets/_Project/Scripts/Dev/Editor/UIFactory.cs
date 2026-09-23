using System;
using ProjectP.Data;
using ProjectP.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjectP.EditorTools
{
    public enum ButtonStyle
    {
        Primary,
        Secondary,
        Danger
    }

    /// <summary>
    /// UI 조립 도구 공통 부품. 모든 화면이 같은 테마·구조·크기 규칙으로 만들어지도록 한 곳에 모은다.
    /// 기준 해상도 1920×1080 (CLAUDE.md UI 규칙).
    /// </summary>
    internal sealed class UIFactory
    {
        public const string ThemePath = "Assets/_Project/Art/UI/UITheme.asset";
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        private UIFactory(UITheme theme) => Theme = theme;

        public UITheme Theme { get; }

        public static UIFactory Load()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null || theme.rounded == null)
            {
                throw new InvalidOperationException("UI 테마가 없습니다. 메뉴 Project P > Build All을 실행하세요.");
            }

            return new UIFactory(theme);
        }

        // ---------------------------------------------------------------- 뼈대

        public GameObject Canvas(string name, int sortingOrder = 0)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool sliced = false, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>화면 전체 그라디언트 배경.</summary>
        public Image Background(Transform parent)
        {
            var image = Image("Background", parent, Theme.backgroundGradient, Color.white);
            Stretch(image.rectTransform);
            return image;
        }

        public Image Glow(Transform parent, Color color, Vector2 size, Vector2 position)
        {
            var image = Image("Glow", parent, Theme.glow, color);
            Place(image.rectTransform, size, position);
            return image;
        }

        /// <summary>뒤쪽 입력을 막는 반투명 전체 화면 막.</summary>
        public Image Dim(string name, Transform parent)
        {
            var image = Image(name, parent, null, Theme.dim, raycast: true);
            Stretch(image.rectTransform);
            return image;
        }

        /// <summary>그림자·면·외곽선을 가진 둥근 카드. 반환된 RectTransform 아래에 내용을 넣는다.</summary>
        public RectTransform Card(string name, Transform parent, Vector2 size, Vector2 position, Color? body = null)
        {
            var root = Rect(name, parent);
            Place(root, size, position);

            var shadow = Image("Shadow", root, Theme.shadow, new Color(0f, 0f, 0f, 0.9f), sliced: true);
            Stretch(shadow.rectTransform, 28f);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -10f);

            var face = Image("Body", root, Theme.rounded, body ?? Theme.surface, sliced: true, raycast: true);
            Stretch(face.rectTransform);

            var border = Image("Border", root, Theme.roundedOutline, Theme.outline, sliced: true);
            Stretch(border.rectTransform);
            return root;
        }

        public Image Divider(Transform parent, float width, float y)
        {
            var line = Image("Divider", parent, null, Theme.outline);
            AnchorTop(line.rectTransform, new Vector2(width, 2f), y);
            return line;
        }

        // ---------------------------------------------------------------- 글자

        public TextMeshProUGUI Text(string name, Transform parent, string content, float size, Color? color = null,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color ?? Theme.textPrimary;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>둥근 알약 모양 태그 ("DEV" 같은 표시).</summary>
        public RectTransform Pill(string name, Transform parent, string content, Color color, Vector2 size)
        {
            var body = Image(name, parent, Theme.rounded, color, sliced: true);
            body.pixelsPerUnitMultiplier = 2f;
            body.rectTransform.sizeDelta = size;
            var label = Text("Label", body.transform, content, size.y * 0.55f, Theme.accentText, FontStyles.Bold);
            Stretch(label.rectTransform);
            return body.rectTransform;
        }

        // ---------------------------------------------------------------- 조작 부품

        /// <summary>
        /// 버튼. 구조: 루트(Button) → Shadow, Body(색이 바뀌는 부분), Label.
        /// 부모가 자식보다 먼저 그려지므로 그림자를 자식으로 두고 루트에는 그래픽을 두지 않는다.
        /// </summary>
        public Button Button(string name, Transform parent, string label, ButtonStyle style, float fontSize = 36f)
        {
            var root = Rect(name, parent);
            var button = root.gameObject.AddComponent<Button>();

            var shadow = Image("Shadow", root, Theme.shadow, new Color(0f, 0f, 0f, 0.8f), sliced: true);
            Stretch(shadow.rectTransform, 18f);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -6f);

            var (fill, textColor) = style switch
            {
                ButtonStyle.Primary => (Theme.accent, Theme.accentText),
                ButtonStyle.Danger => (Theme.danger, Color.white),
                _ => (Theme.surfaceRaised, Theme.textPrimary)
            };

            var body = Image("Body", root, Theme.rounded, fill, sliced: true, raycast: true);
            Stretch(body.rectTransform);

            if (style == ButtonStyle.Secondary)
            {
                var border = Image("Border", root, Theme.roundedOutline, Theme.outline, sliced: true);
                Stretch(border.rectTransform);
            }

            var text = Text("Label", root, label, fontSize, textColor, FontStyles.Bold);
            Stretch(text.rectTransform);

            button.targetGraphic = body;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                highlightedColor = Color.white,
                pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f),
                selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            var fade = root.gameObject.AddComponent<SelectableLabelFade>();
            Wire(fade, ("label", text));
            return button;
        }

        /// <summary>둥근 트랙·강조색 채움·원형 손잡이를 가진 슬라이더 (0~100 정수).</summary>
        public Slider Slider(string name, Transform parent, Vector2 size)
        {
            var root = Rect(name, parent);
            root.sizeDelta = size;
            var slider = root.gameObject.AddComponent<Slider>();

            var track = Image("Track", root, Theme.rounded, Theme.surfaceRaised, sliced: true, raycast: true);
            track.pixelsPerUnitMultiplier = 3f;
            track.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.65f);
            track.rectTransform.sizeDelta = Vector2.zero;

            var fillArea = Rect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.35f);
            fillArea.anchorMax = new Vector2(1f, 0.65f);
            fillArea.sizeDelta = Vector2.zero;

            var fill = Image("Fill", fillArea, Theme.rounded, Theme.accent, sliced: true);
            fill.pixelsPerUnitMultiplier = 3f;
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Rect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.sizeDelta = new Vector2(-size.y, 0f);

            var handle = Image("Handle", handleArea, Theme.circle, Theme.textPrimary, raycast: true);
            handle.rectTransform.sizeDelta = new Vector2(size.y, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            return slider;
        }

        /// <summary>체크 상자 + 글자. 켜지면 상자 안이 강조색으로 채워진다. size는 전체 크기(상자 + 글자).</summary>
        public Toggle Toggle(string name, Transform parent, string label, Vector2 size, float fontSize = 24f)
        {
            var root = Rect(name, parent);
            root.sizeDelta = size;
            var toggle = root.gameObject.AddComponent<Toggle>();

            var box = Image("Box", root, Theme.rounded, Theme.surfaceRaised, sliced: true, raycast: true);
            box.pixelsPerUnitMultiplier = 2f;
            Anchor(box.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 36f), Vector2.zero);

            var border = Image("Border", box.transform, Theme.roundedOutline, Theme.outline, sliced: true);
            border.pixelsPerUnitMultiplier = 2f;
            Stretch(border.rectTransform);

            var check = Image("Check", box.transform, Theme.rounded, Theme.accent, sliced: true);
            check.pixelsPerUnitMultiplier = 3f;
            Stretch(check.rectTransform, -8f);

            var text = Text("Label", root, label, fontSize, Theme.textPrimary, alignment: TextAlignmentOptions.MidlineLeft);
            Anchor(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(size.x - 48f, size.y), new Vector2(48f, 0f));

            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = true;
            toggle.colors = new ColorBlock
            {
                normalColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                highlightedColor = Color.white,
                pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f),
                selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            return toggle;
        }

        public TMP_InputField InputField(string name, Transform parent, string placeholder, float fontSize = 28f)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);

            var background = go.GetComponent<Image>();
            background.sprite = Theme.rounded;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            background.color = Theme.surfaceRaised;

            var field = go.GetComponent<TMP_InputField>();
            field.textComponent.fontSize = fontSize;
            field.textComponent.color = Theme.textPrimary;
            field.textComponent.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderText = (TMP_Text)field.placeholder;
            placeholderText.text = placeholder;
            placeholderText.fontSize = fontSize;
            placeholderText.color = new Color(Theme.textSecondary.r, Theme.textSecondary.g, Theme.textSecondary.b, 0.6f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderText.fontStyle = FontStyles.Normal;

            var textArea = (RectTransform)field.textViewport;
            textArea.offsetMin = new Vector2(20f, 6f);
            textArea.offsetMax = new Vector2(-20f, -6f);
            return field;
        }

        /// <summary>보석 타일 + 아이콘 (장식·통계 표시용).</summary>
        public RectTransform GemBadge(string name, Transform parent, GemData gem, float size)
        {
            var tile = Image(name, parent, Theme.gemTile, gem != null ? gem.Color : Color.magenta);
            tile.rectTransform.sizeDelta = new Vector2(size, size);

            if (gem != null && gem.Icon != null)
            {
                var icon = Image("Icon", tile.transform, gem.Icon, gem.IconColor);
                icon.preserveAspect = true;
                icon.rectTransform.sizeDelta = new Vector2(size * 0.58f, size * 0.58f);
            }

            return tile.rectTransform;
        }

        // ---------------------------------------------------------------- 배치 도우미

        /// <summary>부모를 꽉 채운다. expand만큼 사방으로 넓힌다.</summary>
        public static void Stretch(RectTransform rect, float expand = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-expand, -expand);
            rect.offsetMax = new Vector2(expand, expand);
        }

        /// <summary>부모 가운데 기준으로 놓는다.</summary>
        public static void Place(RectTransform rect, Vector2 size, Vector2 position) =>
            Anchor(rect, new Vector2(0.5f, 0.5f), size, position);

        /// <summary>부모 위쪽 가운데에서 y만큼 내려 놓는다.</summary>
        public static void AnchorTop(RectTransform rect, Vector2 size, float y) =>
            Anchor(rect, new Vector2(0.5f, 1f), size, new Vector2(0f, -y));

        /// <summary>부모 아래쪽 가운데에서 y만큼 올려 놓는다.</summary>
        public static void AnchorBottom(RectTransform rect, Vector2 size, float y) =>
            Anchor(rect, new Vector2(0.5f, 0f), size, new Vector2(0f, y));

        /// <summary>anchor와 pivot을 같은 점에 두고 크기·위치를 정한다.</summary>
        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        public static HorizontalLayoutGroup Row(RectTransform rect, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        public static VerticalLayoutGroup Column(RectTransform rect, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = alignment;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        // ---------------------------------------------------------------- 씬·연결 도우미

        public static Scene OpenScene(string path)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException("씬 저장이 취소되어 UI 조립을 중단했습니다.");
            }

            return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        public static void SaveScene(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>이전에 조립 도구가 만든 루트 오브젝트를 지운다.</summary>
        public static void RemoveRoots(Scene scene, params string[] names)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (Array.IndexOf(names, root.name) >= 0 || root.GetComponent<EventSystem>() != null) Object.DestroyImmediate(root);
            }
        }

        /// <summary>프로젝트가 새 Input System 전용이라 InputSystemUIInputModule을 쓴다.</summary>
        public static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        /// <summary>private [SerializeField] 필드를 이름으로 연결한다. 이름이 틀리면 바로 예외를 던진다.</summary>
        public static void Wire(Object target, params (string field, Object value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in references) Property(serialized, field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = Property(serialized, field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireEnumArray<T>(Object target, string field, T[] values) where T : Enum
        {
            var serialized = new SerializedObject(target);
            var property = Property(serialized, field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).intValue = Convert.ToInt32(values[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Property(SerializedObject serialized, string field) =>
            serialized.FindProperty(field) ??
            throw new InvalidOperationException($"{serialized.targetObject.GetType().Name}.{field} 필드를 찾을 수 없습니다.");
    }
}
