using System.Collections.Generic;
using ProjectP.Data;
using ProjectP.Dev;
using ProjectP.Gameplay.Puzzle;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// Dev_PuzzleTest 화면 조립. 기획서 12.15
    /// 실제 퍼즐 부품(PuzzleManager·BoardView·BoardInput·ConnectionView·ActionPointView·CancelZone)을 그대로 쓰고,
    /// 개발용 제어(재생성·시드·분포·연결 기록·턴·행동력 조정)만 덧붙인다.
    /// 보드는 실제 HUD처럼 화면 중앙 하단(기획서 9.3), 행동력은 보드 상단 중앙(기획서 9.5), 취소 영역은 보드 양옆에 둔다.
    /// </summary>
    internal static class PuzzleTestBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Dev/Dev_PuzzleTest.unity";
        private const string UIRootName = "[PuzzleTestUI]";
        private const string PuzzleRootName = "[Puzzle]";

        // BoardView 기본값과 같아야 한다 (칸 92 + 간격 4).
        private const float CellSize = 92f;
        private const float Spacing = 4f;
        private const float BoardPadding = 26f;

        // 상단 카드 3개: 시드 / 보석 분포 / 연결 기록
        private static readonly Vector2 CardSize = new Vector2(580f, 230f);
        private const float CardY = 282f;
        private const float CardGap = 620f;

        // 보드 카드 가장자리에서 이만큼 떨어진 바깥부터 취소 영역이다. 보드 끝 칸을 긋다가 잘못 취소되지 않게 여유를 둔다.
        private const float CancelZoneGap = 24f;

        // 취소 상태 표시: 화면 가장자리(안쪽 5% 구간)만 옅게 붉어진다.
        private const float CancelVignetteAlpha = 0.25f;

        private static readonly GemType[] StatTypes =
        {
            GemType.Physical, GemType.Magic, GemType.Heal, GemType.Chaos, GemType.Balance
        };

        [MenuItem("Project P/Rebuild/Dev_PuzzleTest UI")]
        private static void Menu() => Build(UIFactory.Load());

        public static void Build(UIFactory ui)
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
            var config = database != null ? database.Puzzle : null;
            if (config == null) throw new System.InvalidOperationException("PuzzleConfig가 없습니다. Project P > Build All을 먼저 실행하세요.");

            var scene = UIFactory.OpenScene(ScenePath);
            UIFactory.RemoveRoots(scene, UIRootName, PuzzleRootName);
            UIFactory.CreateEventSystem();

            var theme = ui.Theme;
            var puzzle = new GameObject(PuzzleRootName).AddComponent<PuzzleManager>();

            var root = ui.Canvas(UIRootName);
            var panel = root.AddComponent<PuzzleTestPanel>();
            ui.Background(root.transform);

            var top = BuildTopBar(ui, root.transform);
            var (seedText, seedInput, seedButton, regenerateButton) = BuildSeedCard(ui, root.transform);
            var (statTexts, summary) = BuildStatsCard(ui, root.transform, database);
            var (statusText, logText) = BuildLogCard(ui, root.transform);

            // 보드 카드: 행·열 수는 PuzzleConfig를 따른다.
            var pitch = CellSize + Spacing;
            var boardSize = new Vector2(config.Columns * pitch - Spacing, config.Rows * pitch - Spacing);
            var cardSize = boardSize + Vector2.one * (BoardPadding * 2f);
            var cardY = -540f + 36f + cardSize.y / 2f;
            var boardCard = ui.Card("BoardCard", root.transform, cardSize, new Vector2(0f, cardY));

            // 보드 영역: 칸 표시 + 마우스 입력. 입력을 받으려면 투명 Graphic이 필요하다.
            // 위에서 떨어지는 새 보석은 보드 영역 밖에서는 보이지 않게 가린다(선택 외곽선이 잘리지 않도록 약간 여유).
            var board = UIFactory.Rect("Board", boardCard);
            UIFactory.Place(board, boardSize, Vector2.zero);
            var hitArea = board.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            board.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-12f, -12f, -12f, -12f);
            board.gameObject.AddComponent<CanvasGroup>();
            var boardView = board.gameObject.AddComponent<BoardView>();
            var input = board.gameObject.AddComponent<BoardInput>();
            UIFactory.Wire(boardView,
                ("cellRoot", board), ("tileSprite", theme.gemTile), ("outlineSprite", theme.gemTileOutline), ("shadowSprite", theme.shadow));

            // 연결선 층: 보드 위에 같은 크기로 겹친다.
            var overlay = UIFactory.Rect("Connection", boardCard);
            UIFactory.Place(overlay, boardSize, Vector2.zero);
            var connectionView = overlay.gameObject.AddComponent<ConnectionView>();
            var (badge, badgeText) = CountBadge(ui, overlay);
            UIFactory.Wire(connectionView,
                ("puzzle", puzzle), ("boardView", boardView), ("lineRoot", overlay), ("dotSprite", theme.circle),
                ("countBadge", badge), ("countText", badgeText));
            SetColor(connectionView, "lineColor", BootUIBuilder.WithAlpha(theme.accent, 0.85f));

            // 행동력: 보드 상단 중앙, 카드 윗변에 걸친다.
            BuildActionPointTab(ui, root.transform, puzzle, cardY + cardSize.y / 2f);

            // 취소 영역: 보드 양옆 바깥 전체(화면 끝까지, 위아래 전체). 보이지 않는다.
            var zoneInset = cardSize.x / 2f + CancelZoneGap;
            var leftZone = BuildCancelZone(root.transform, "CancelZoneLeft", -zoneInset, pivotX: 1f);
            var rightZone = BuildCancelZone(root.transform, "CancelZoneRight", zoneInset, pivotX: 0f);

            var hint = ui.Text("Hint", root.transform,
                "드래그해 잇고 놓으면 사용  ·  1개만 놓아도 턴 소모  ·  보드 양옆 바깥에서 화면이 붉어질 때 놓으면 취소  ·  우클릭 / ESC 취소",
                20f, theme.textSecondary);
            UIFactory.AnchorBottom(hint.rectTransform, new Vector2(1700f, 28f), 4f);

            // 취소 상태 표시: 화면 맨 위에 겹치는 붉은 막. 입력은 막지 않는다.
            var cancelOverlay = BuildCancelOverlay(ui, root.transform);
            UIFactory.Wire(input, ("puzzle", puzzle), ("boardView", boardView), ("cancelOverlay", cancelOverlay));
            UIFactory.WireArray(input, "cancelZones", new Object[] { leftZone, rightZone });

            UIFactory.Wire(puzzle, ("boardView", boardView));
            UIFactory.Wire(panel,
                ("puzzle", puzzle),
                ("seedText", seedText),
                ("summaryText", summary),
                ("seedInput", seedInput),
                ("regenerateButton", regenerateButton),
                ("seedButton", seedButton),
                ("connectionStatusText", statusText),
                ("connectionLogText", logText),
                ("turnText", top.Turn),
                ("nextTurnButton", top.NextTurn),
                ("autoTurnToggle", top.AutoTurn),
                ("actionPointMinusButton", top.Minus),
                ("actionPointPlusButton", top.Plus),
                ("actionPointBaseText", top.BaseValue));
            UIFactory.WireEnumArray(panel, "statTypes", StatTypes);
            UIFactory.WireArray(panel, "statTexts", statTexts);

            UIFactory.SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
            UIFactory.SaveScene(scene);
            Debug.Log("[PuzzleTestBuilder] Dev_PuzzleTest 화면 조립 완료");
        }

        // ---------------------------------------------------------------- 상단 바

        private readonly struct TopBar
        {
            public TopBar(TMP_Text turn, Button nextTurn, Toggle autoTurn, Button minus, Button plus, TMP_Text baseValue)
            {
                Turn = turn;
                NextTurn = nextTurn;
                AutoTurn = autoTurn;
                Minus = minus;
                Plus = plus;
                BaseValue = baseValue;
            }

            public TMP_Text Turn { get; }
            public Button NextTurn { get; }
            public Toggle AutoTurn { get; }
            public Button Minus { get; }
            public Button Plus { get; }
            public TMP_Text BaseValue { get; }
        }

        private static TopBar BuildTopBar(UIFactory ui, Transform parent)
        {
            var theme = ui.Theme;
            var bar = UIFactory.Image("TopBar", parent, null, theme.surface);
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = new Vector2(1f, 1f);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f);
            bar.rectTransform.sizeDelta = new Vector2(0f, 96f);
            bar.rectTransform.anchoredPosition = Vector2.zero;

            var line = UIFactory.Image("BottomLine", bar.transform, null, theme.outline);
            line.rectTransform.anchorMin = Vector2.zero;
            line.rectTransform.anchorMax = new Vector2(1f, 0f);
            line.rectTransform.pivot = new Vector2(0.5f, 0f);
            line.rectTransform.sizeDelta = new Vector2(0f, 2f);

            var title = ui.Text("Title", bar.transform, "퍼즐 테스트", 38f, theme.textPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(240f, 60f), new Vector2(48f, 0f));

            var pill = ui.Pill("DevTag", bar.transform, "DEV", theme.accentSecondary, new Vector2(84f, 36f));
            UIFactory.Anchor(pill, new Vector2(0f, 0.5f), pill.sizeDelta, new Vector2(270f, 0f));

            var turn = ui.Text("Turn", bar.transform, "턴 1  ·  연결 가능", 30f, theme.textPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(turn.rectTransform, new Vector2(0f, 0.5f), new Vector2(560f, 60f), new Vector2(386f, 0f));

            // 오른쪽: 기본 행동력 −/+ · 자동 다음 턴 · 다음 턴
            var controls = UIFactory.Rect("TurnControls", bar.transform);
            UIFactory.Anchor(controls, new Vector2(1f, 0.5f), new Vector2(790f, 56f), new Vector2(-40f, 0f));
            UIFactory.Row(controls, 14f, TextAnchor.MiddleRight);

            var label = ui.Text("ActionPointLabel", controls, "기본 행동력", 24f, theme.textSecondary, alignment: TextAlignmentOptions.MidlineRight);
            label.rectTransform.sizeDelta = new Vector2(140f, 56f);

            var minus = ui.Button("ActionPointMinus", controls, "-", ButtonStyle.Secondary, 30f);
            ((RectTransform)minus.transform).sizeDelta = new Vector2(52f, 52f);

            var value = ui.Text("ActionPointValue", controls, "6", 32f, theme.accent, FontStyles.Bold);
            value.rectTransform.sizeDelta = new Vector2(48f, 56f);

            var plus = ui.Button("ActionPointPlus", controls, "+", ButtonStyle.Secondary, 30f);
            ((RectTransform)plus.transform).sizeDelta = new Vector2(52f, 52f);

            UIFactory.Rect("Spacer", controls).sizeDelta = new Vector2(24f, 56f);

            var autoTurn = ui.Toggle("AutoNextTurn", controls, "자동 다음 턴", new Vector2(210f, 40f));

            var nextTurn = ui.Button("NextTurnButton", controls, "다음 턴", ButtonStyle.Primary, 26f);
            ((RectTransform)nextTurn.transform).sizeDelta = new Vector2(170f, 56f);

            return new TopBar(turn, nextTurn, autoTurn, minus, plus, value);
        }

        // ---------------------------------------------------------------- 상단 카드

        private static (TMP_Text seed, TMP_InputField input, Button seedButton, Button regenerate) BuildSeedCard(UIFactory ui, Transform parent)
        {
            var theme = ui.Theme;
            var card = ui.Card("SeedCard", parent, CardSize, new Vector2(-CardGap, CardY));
            CardHeader(ui, card, "현재 시드");

            var seed = ui.Text("SeedValue", card, "-", 56f, theme.accent, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(seed.rectTransform, new Vector2(0f, 1f), new Vector2(524f, 64f), new Vector2(28f, -62f));

            var controls = UIFactory.Rect("Controls", card);
            UIFactory.AnchorBottom(controls, new Vector2(532f, 64f), 26f);
            UIFactory.Row(controls, 14f);

            var input = ui.InputField("SeedInput", controls, "시드 입력", 26f);
            ((RectTransform)input.transform).sizeDelta = new Vector2(200f, 64f);
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 9;

            var seedButton = ui.Button("SeedButton", controls, "시드로 생성", ButtonStyle.Secondary, 24f);
            ((RectTransform)seedButton.transform).sizeDelta = new Vector2(152f, 64f);

            var regenerate = ui.Button("RegenerateButton", controls, "새 보드", ButtonStyle.Primary, 24f);
            ((RectTransform)regenerate.transform).sizeDelta = new Vector2(152f, 64f);

            return (seed, input, seedButton, regenerate);
        }

        private static (TMP_Text[] counts, TMP_Text summary) BuildStatsCard(UIFactory ui, Transform parent, GameDatabase database)
        {
            var theme = ui.Theme;
            var card = ui.Card("StatsCard", parent, CardSize, new Vector2(0f, CardY));
            CardHeader(ui, card, "보석 분포");

            var summary = ui.Text("Summary", card, "", 22f, theme.textSecondary, alignment: TextAlignmentOptions.MidlineRight);
            UIFactory.Anchor(summary.rectTransform, new Vector2(1f, 1f), new Vector2(300f, 36f), new Vector2(-28f, -22f));

            var columns = UIFactory.Rect("Columns", card);
            UIFactory.AnchorBottom(columns, new Vector2(552f, 140f), 22f);
            UIFactory.Row(columns, 8f);

            var texts = new List<TMP_Text>();
            foreach (var type in StatTypes)
            {
                var gem = database.GetGem(type);
                var column = UIFactory.Rect(type.ToString(), columns);
                column.sizeDelta = new Vector2(104f, 140f);
                UIFactory.Column(column, 4f);

                ui.GemBadge("Badge", column, gem, 56f);

                var name = ui.Text("Name", column, gem != null ? gem.DisplayName : type.ToString(), 24f, theme.textPrimary, FontStyles.Bold);
                name.rectTransform.sizeDelta = new Vector2(104f, 32f);

                var count = ui.Text("Count", column, "-", 22f, theme.accent);
                count.rectTransform.sizeDelta = new Vector2(104f, 30f);
                texts.Add(count);
            }

            return (texts.ToArray(), summary);
        }

        private static (TMP_Text status, TMP_Text log) BuildLogCard(UIFactory ui, Transform parent)
        {
            var theme = ui.Theme;
            var card = ui.Card("LogCard", parent, CardSize, new Vector2(CardGap, CardY));
            CardHeader(ui, card, "연결 기록");

            var status = ui.Text("Status", card, "", 22f, theme.accentSecondary, alignment: TextAlignmentOptions.MidlineRight);
            UIFactory.Anchor(status.rectTransform, new Vector2(1f, 1f), new Vector2(320f, 36f), new Vector2(-28f, -22f));

            var log = ui.Text("Log", card, "", 24f, theme.textPrimary, alignment: TextAlignmentOptions.TopLeft);
            UIFactory.Anchor(log.rectTransform, new Vector2(0f, 1f), new Vector2(524f, 150f), new Vector2(28f, -68f));
            log.richText = true;
            return (status, log);
        }

        private static void CardHeader(UIFactory ui, RectTransform card, string text)
        {
            var header = ui.Text("Header", card, text, 26f, ui.Theme.textSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(260f, 36f), new Vector2(28f, -22f));
        }

        // ---------------------------------------------------------------- 보드 주변

        /// <summary>행동력 탭: [행동력] ●●●●●● [4 / 6] [다음 턴 +N]. 칩 수에 맞춰 너비가 자동으로 늘어난다.</summary>
        private static void BuildActionPointTab(UIFactory ui, Transform parent, PuzzleManager puzzle, float y)
        {
            var theme = ui.Theme;
            var tab = UIFactory.Rect("ActionPoints", parent);
            UIFactory.Place(tab, new Vector2(360f, 64f), new Vector2(0f, y));

            var layout = tab.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 0, 0);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            tab.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 배경(그림자·면·외곽선)은 배치 계산에서 뺀다.
            var shadow = UIFactory.Image("Shadow", tab, theme.shadow, new Color(0f, 0f, 0f, 0.9f), sliced: true);
            UIFactory.Stretch(shadow.rectTransform, 22f);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            var body = UIFactory.Image("Body", tab, theme.rounded, theme.surface, sliced: true);
            UIFactory.Stretch(body.rectTransform);
            var border = UIFactory.Image("Border", tab, theme.roundedOutline, theme.accent, sliced: true);
            UIFactory.Stretch(border.rectTransform);
            foreach (var background in new Component[] { shadow, body, border })
            {
                background.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }

            ui.Text("Label", tab, "행동력", 24f, theme.textSecondary, FontStyles.Bold);

            var chips = UIFactory.Rect("Chips", tab);
            var chipLayout = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 7f;
            chipLayout.childAlignment = TextAnchor.MiddleCenter;
            chipLayout.childControlWidth = true;
            chipLayout.childControlHeight = true;
            chipLayout.childForceExpandWidth = false;
            chipLayout.childForceExpandHeight = false;

            var value = ui.Text("Value", tab, "6 / 6", 30f, theme.accent, FontStyles.Bold);
            value.gameObject.AddComponent<LayoutElement>().minWidth = 96f;

            var bonus = ui.Text("NextTurnBonus", tab, "다음 턴 +0", 22f, theme.accentSecondary, FontStyles.Bold);

            var view = tab.gameObject.AddComponent<ActionPointView>();
            UIFactory.Wire(view,
                ("puzzle", puzzle), ("chipRoot", chips), ("chipSprite", theme.circle), ("valueText", value), ("bonusText", bonus));
            SetColor(view, "availableColor", theme.accent);
            SetColor(view, "bonusColor", theme.accentSecondary);
            SetColor(view, "previewColor", BootUIBuilder.WithAlpha(theme.accent, 0.28f));
            SetColor(view, "spentColor", theme.outline);
        }

        /// <summary>
        /// 보이지 않는 취소 영역. 보드 가장자리 바깥(inset)에서 화면 끝까지, 위아래 전체를 덮는다.
        /// 화면 비율이 달라도 보드 기준으로 붙도록 가운데에 고정하고 바깥쪽으로 넉넉히 늘린다.
        /// </summary>
        private static CancelZone BuildCancelZone(Transform parent, string name, float inset, float pivotX)
        {
            var zone = UIFactory.Rect(name, parent);
            zone.anchorMin = new Vector2(0.5f, 0f);
            zone.anchorMax = new Vector2(0.5f, 1f);
            zone.pivot = new Vector2(pivotX, 0.5f);
            zone.sizeDelta = new Vector2(4000f, 0f);
            zone.anchoredPosition = new Vector2(inset, 0f);
            return zone.gameObject.AddComponent<CancelZone>();
        }

        /// <summary>취소 상태에서 화면 가장자리를 옅게 붉게 물들이는 막.</summary>
        private static CancelOverlay BuildCancelOverlay(UIFactory ui, Transform parent)
        {
            var theme = ui.Theme;
            var overlay = UIFactory.Rect("[CancelOverlay]", parent);
            UIFactory.Stretch(overlay);
            overlay.gameObject.AddComponent<CanvasGroup>();

            var vignette = UIFactory.Image("Vignette", overlay, theme.vignette, BootUIBuilder.WithAlpha(theme.danger, CancelVignetteAlpha));
            UIFactory.Stretch(vignette.rectTransform);

            return overlay.gameObject.AddComponent<CancelOverlay>();
        }

        /// <summary>경로 끝에 붙는 "3개" 표시. 어두운 알약 + 금색 글자.</summary>
        private static (RectTransform badge, TMP_Text text) CountBadge(UIFactory ui, Transform parent)
        {
            var body = UIFactory.Image("CountBadge", parent, ui.Theme.rounded, ui.Theme.surface, sliced: true);
            body.pixelsPerUnitMultiplier = 2f;
            body.rectTransform.sizeDelta = new Vector2(84f, 40f);

            var border = UIFactory.Image("Border", body.transform, ui.Theme.roundedOutline, ui.Theme.accent, sliced: true);
            border.pixelsPerUnitMultiplier = 2f;
            UIFactory.Stretch(border.rectTransform);

            var text = ui.Text("Label", body.transform, "0개", 24f, ui.Theme.accent, FontStyles.Bold);
            UIFactory.Stretch(text.rectTransform);
            return (body.rectTransform, text);
        }

        private static void SetColor(Object target, string field, Color color)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field) ??
                           throw new System.InvalidOperationException($"{target.GetType().Name}.{field} 필드를 찾을 수 없습니다.");
            property.colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
