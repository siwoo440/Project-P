using ProjectP.Title;
using ProjectP.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// 01_Title 화면 조립. 기획서 12.3
    /// 다시 실행하면 [TitleUI]와 EventSystem을 지우고 새로 만든다. 씬에서 직접 다듬은 배치는 사라진다.
    /// </summary>
    internal static class TitleUIBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/01_Title.unity";
        private const string RootName = "[TitleUI]";

        [MenuItem("Project P/Rebuild/Title UI")]
        private static void Menu() => Build(UIFactory.Load());

        public static void Build(UIFactory ui)
        {
            var scene = UIFactory.OpenScene(ScenePath);
            UIFactory.RemoveRoots(scene, RootName);
            UIFactory.CreateEventSystem();

            var theme = ui.Theme;
            var root = ui.Canvas(RootName);
            var screen = root.AddComponent<TitleScreen>();

            // 배경과 로고
            ui.Background(root.transform);
            ui.Glow(root.transform, BootUIBuilder.WithAlpha(theme.accent, 0.10f), new Vector2(1200f, 760f), new Vector2(0f, 250f));
            BootUIBuilder.GemEmblem(ui, root.transform, 84f, 124f, 400f, arc: 24f);

            var logo = ui.Text("Logo", root.transform, "PROJECT P", 140f, theme.textPrimary, FontStyles.Bold);
            logo.characterSpacing = 6f;
            UIFactory.Place(logo.rectTransform, new Vector2(1400f, 170f), new Vector2(0f, 250f));

            var subtitle = ui.Text("Subtitle", root.transform, "보석 연결 퍼즐 · 턴제 전략 RPG", 34f, theme.textSecondary);
            UIFactory.Place(subtitle.rectTransform, new Vector2(1000f, 48f), new Vector2(0f, 150f));

            var accentLine = UIFactory.Image("AccentLine", root.transform, theme.rounded, theme.accent, sliced: true);
            accentLine.pixelsPerUnitMultiplier = 4f;
            UIFactory.Place(accentLine.rectTransform, new Vector2(120f, 6f), new Vector2(0f, 108f));

            // 메뉴
            var menu = UIFactory.Rect("Menu", root.transform);
            UIFactory.Place(menu, new Vector2(440f, 88f * 4f + 20f * 3f), new Vector2(0f, -190f));
            UIFactory.Column(menu, 20f);

            var newGame = MenuButton(ui, menu, "NewGameButton", "새 게임", ButtonStyle.Primary);
            var continueGame = MenuButton(ui, menu, "ContinueButton", "이어하기", ButtonStyle.Secondary);
            var settingsButton = MenuButton(ui, menu, "SettingsButton", "설정", ButtonStyle.Secondary);
            var quit = MenuButton(ui, menu, "QuitButton", "게임 종료", ButtonStyle.Secondary);

            var version = ui.Text("Version", root.transform, "v0.1.0 · 프로토타입", 24f, theme.textSecondary,
                alignment: TextAlignmentOptions.BottomRight);
            UIFactory.Anchor(version.rectTransform, new Vector2(1f, 0f), new Vector2(500f, 40f), new Vector2(-40f, 32f));

            var dialog = BuildConfirmDialog(ui, root.transform);
            var settingsPanel = BuildSettingsPanel(ui, root.transform);

            UIFactory.Wire(screen,
                ("newGameButton", newGame),
                ("continueButton", continueGame),
                ("settingsButton", settingsButton),
                ("quitButton", quit),
                ("confirmDialog", dialog),
                ("settingsPanel", settingsPanel));

            dialog.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            UIFactory.SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
            UIFactory.SaveScene(scene);
            Debug.Log("[TitleUIBuilder] 01_Title 화면 조립 완료");
        }

        private static Button MenuButton(UIFactory ui, Transform parent, string name, string label, ButtonStyle style)
        {
            var button = ui.Button(name, parent, label, style, 38f);
            ((RectTransform)button.transform).sizeDelta = new Vector2(440f, 88f);
            return button;
        }

        private static ConfirmDialog BuildConfirmDialog(UIFactory ui, Transform parent)
        {
            var dim = ui.Dim("ConfirmDialog", parent);
            var dialog = dim.gameObject.AddComponent<ConfirmDialog>();

            var card = ui.Card("Card", dim.transform, new Vector2(760f, 380f), Vector2.zero);

            var title = ui.Text("Title", card, "확인", 40f, ui.Theme.textPrimary, FontStyles.Bold);
            UIFactory.AnchorTop(title.rectTransform, new Vector2(680f, 52f), 40f);

            var message = ui.Text("Message", card, "", 34f, ui.Theme.textSecondary);
            UIFactory.AnchorTop(message.rectTransform, new Vector2(680f, 120f), 110f);

            var buttons = UIFactory.Rect("Buttons", card);
            UIFactory.AnchorBottom(buttons, new Vector2(600f, 88f), 40f);
            UIFactory.Row(buttons, 24f);

            var no = ui.Button("NoButton", buttons, "아니오", ButtonStyle.Secondary, 34f);
            ((RectTransform)no.transform).sizeDelta = new Vector2(288f, 84f);
            var yes = ui.Button("YesButton", buttons, "예", ButtonStyle.Danger, 34f);
            ((RectTransform)yes.transform).sizeDelta = new Vector2(288f, 84f);

            UIFactory.Wire(dialog, ("messageText", message), ("yesButton", yes), ("noButton", no));
            return dialog;
        }

        private static SettingsPanel BuildSettingsPanel(UIFactory ui, Transform parent)
        {
            var theme = ui.Theme;
            var dim = ui.Dim("SettingsPanel", parent);
            var panel = dim.gameObject.AddComponent<SettingsPanel>();

            var card = ui.Card("Card", dim.transform, new Vector2(880f, 620f), Vector2.zero);

            var header = ui.Text("Header", card, "설정", 48f, theme.textPrimary, FontStyles.Bold);
            UIFactory.AnchorTop(header.rectTransform, new Vector2(800f, 60f), 40f);

            var section = ui.Text("Section", card, "사운드", 26f, theme.accent, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.AnchorTop(section.rectTransform, new Vector2(760f, 36f), 122f);
            ui.Divider(card, 760f, 166f);

            var rows = UIFactory.Rect("Rows", card);
            UIFactory.AnchorTop(rows, new Vector2(760f, 64f * 3f + 28f * 2f), 196f);
            UIFactory.Column(rows, 28f);

            var (master, masterValue) = VolumeRow(ui, rows, "Master", "마스터 볼륨");
            var (bgm, bgmValue) = VolumeRow(ui, rows, "Bgm", "배경음");
            var (sfx, sfxValue) = VolumeRow(ui, rows, "Sfx", "효과음");

            var close = ui.Button("CloseButton", card, "닫기", ButtonStyle.Primary, 34f);
            UIFactory.AnchorBottom((RectTransform)close.transform, new Vector2(280f, 84f), 40f);

            UIFactory.Wire(panel,
                ("masterSlider", master), ("bgmSlider", bgm), ("sfxSlider", sfx),
                ("masterValueText", masterValue), ("bgmValueText", bgmValue), ("sfxValueText", sfxValue),
                ("closeButton", close));
            return panel;
        }

        private static (Slider slider, TMP_Text value) VolumeRow(UIFactory ui, Transform parent, string name, string label)
        {
            var row = UIFactory.Rect(name + "Row", parent);
            row.sizeDelta = new Vector2(760f, 64f);
            UIFactory.Row(row, 24f, TextAnchor.MiddleLeft);

            var labelText = ui.Text("Label", row, label, 30f, ui.Theme.textPrimary, alignment: TextAlignmentOptions.MidlineLeft);
            labelText.rectTransform.sizeDelta = new Vector2(220f, 64f);

            var slider = ui.Slider("Slider", row, new Vector2(400f, 36f));

            var value = ui.Text("Value", row, "0", 30f, ui.Theme.accent, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            value.rectTransform.sizeDelta = new Vector2(92f, 64f);

            return (slider, value);
        }
    }
}
