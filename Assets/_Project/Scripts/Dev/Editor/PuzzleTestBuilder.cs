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
    /// 실제 PuzzleManager·BoardView·BoardInput·ConnectionView를 그대로 쓰고, 개발용 제어(재생성·시드·분포·연결 기록)만 덧붙인다.
    /// 보드는 실제 HUD처럼 화면 중앙 하단에 둔다(기획서 9.3).
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
        private const float CardY = 272f;
        private const float CardGap = 620f;

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

            var summary = BuildTopBar(ui, root.transform);
            var (seedText, seedInput, seedButton, regenerateButton) = BuildSeedCard(ui, root.transform);
            var statTexts = BuildStatsCard(ui, root.transform, database);
            var (statusText, logText) = BuildLogCard(ui, root.transform);

            // 보드: 행·열 수는 PuzzleConfig를 따른다.
            var pitch = CellSize + Spacing;
            var boardSize = new Vector2(config.Columns * pitch - Spacing, config.Rows * pitch - Spacing);
            var cardSize = boardSize + Vector2.one * (BoardPadding * 2f);
            var boardCard = ui.Card("BoardCard", root.transform, cardSize, new Vector2(0f, -540f + 36f + cardSize.y / 2f));

            // 보드 영역: 칸 표시(BoardView) + 마우스 입력(BoardInput). 입력을 받으려면 투명 Graphic이 필요하다.
            var board = UIFactory.Rect("Board", boardCard);
            UIFactory.Place(board, boardSize, Vector2.zero);
            var hitArea = board.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            var boardView = board.gameObject.AddComponent<BoardView>();
            var input = board.gameObject.AddComponent<BoardInput>();
            UIFactory.Wire(boardView,
                ("cellRoot", board), ("tileSprite", theme.gemTile), ("outlineSprite", theme.gemTileOutline), ("shadowSprite", theme.shadow));
            UIFactory.Wire(input, ("puzzle", puzzle), ("boardView", boardView));

            // 연결선 층: 보드 위에 같은 크기로 겹친다.
            var overlay = UIFactory.Rect("Connection", boardCard);
            UIFactory.Place(overlay, boardSize, Vector2.zero);
            var connectionView = overlay.gameObject.AddComponent<ConnectionView>();
            var (badge, badgeText) = CountBadge(ui, overlay);
            UIFactory.Wire(connectionView,
                ("puzzle", puzzle), ("boardView", boardView), ("lineRoot", overlay), ("dotSprite", theme.circle),
                ("countBadge", badge), ("countText", badgeText));
            SetColor(connectionView, "lineColor", BootUIBuilder.WithAlpha(theme.accent, 0.85f));

            UIFactory.Wire(puzzle, ("boardView", boardView));
            UIFactory.Wire(panel,
                ("puzzle", puzzle),
                ("seedText", seedText),
                ("summaryText", summary),
                ("seedInput", seedInput),
                ("regenerateButton", regenerateButton),
                ("seedButton", seedButton),
                ("connectionStatusText", statusText),
                ("connectionLogText", logText));
            UIFactory.WireEnumArray(panel, "statTypes", StatTypes);
            UIFactory.WireArray(panel, "statTexts", statTexts);

            UIFactory.SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
            UIFactory.SaveScene(scene);
            Debug.Log("[PuzzleTestBuilder] Dev_PuzzleTest 화면 조립 완료");
        }

        private static TMP_Text BuildTopBar(UIFactory ui, Transform parent)
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

            var hint = ui.Text("Hint", bar.transform, "드래그: 연결  ·  놓기: 확정  ·  우클릭 / ESC: 취소", 24f, theme.textSecondary,
                alignment: TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(hint.rectTransform, new Vector2(0f, 0.5f), new Vector2(800f, 40f), new Vector2(378f, 0f));

            var summary = ui.Text("Summary", bar.transform, "", 28f, theme.textSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UIFactory.Anchor(summary.rectTransform, new Vector2(1f, 0.5f), new Vector2(500f, 60f), new Vector2(-48f, 0f));
            return summary;
        }

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

        private static TMP_Text[] BuildStatsCard(UIFactory ui, Transform parent, GameDatabase database)
        {
            var theme = ui.Theme;
            var card = ui.Card("StatsCard", parent, CardSize, new Vector2(0f, CardY));
            CardHeader(ui, card, "보석 분포");

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

            return texts.ToArray();
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
