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
    /// 실제 PuzzleManager·BoardView를 그대로 쓰고, 개발용 제어(재생성·시드·분포)만 덧붙인다.
    /// 보드는 실제 HUD처럼 화면 중앙 하단에 둔다(기획서 9.3).
    /// </summary>
    internal static class PuzzleTestBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Dev/Dev_PuzzleTest.unity";
        private const string UIRootName = "[PuzzleTestUI]";
        private const string PuzzleRootName = "[Puzzle]";

        // BoardView 기본값과 같아야 한다 (셀 92 + 간격 4).
        private const float CellSize = 92f;
        private const float Spacing = 4f;
        private const float BoardPadding = 26f;

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
            var root = ui.Canvas(UIRootName);
            var panel = root.AddComponent<PuzzleTestPanel>();
            ui.Background(root.transform);

            var summary = BuildTopBar(ui, root.transform);
            var (seedText, seedInput, seedButton, regenerateButton) = BuildSeedCard(ui, root.transform);
            var statTexts = BuildStatsCard(ui, root.transform, database);

            // 보드: 행·열 수는 PuzzleConfig를 따른다.
            var pitch = CellSize + Spacing;
            var boardSize = new Vector2(config.Columns * pitch - Spacing, config.Rows * pitch - Spacing);
            var cardSize = boardSize + Vector2.one * (BoardPadding * 2f);
            var cardY = -540f + 36f + cardSize.y / 2f;
            var boardCard = ui.Card("BoardCard", root.transform, cardSize, new Vector2(0f, cardY));

            var board = UIFactory.Rect("Board", boardCard);
            UIFactory.Place(board, boardSize, Vector2.zero);
            var boardView = board.gameObject.AddComponent<BoardView>();
            UIFactory.Wire(boardView, ("cellRoot", board), ("tileSprite", theme.gemTile), ("shadowSprite", theme.shadow));

            var puzzleRoot = new GameObject(PuzzleRootName);
            var puzzle = puzzleRoot.AddComponent<PuzzleManager>();
            UIFactory.Wire(puzzle, ("boardView", boardView));

            UIFactory.Wire(panel,
                ("puzzle", puzzle),
                ("seedText", seedText),
                ("summaryText", summary),
                ("seedInput", seedInput),
                ("regenerateButton", regenerateButton),
                ("seedButton", seedButton));
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

            var note = ui.Text("Note", bar.transform, "Dev_PuzzleTest · 빌드 제외 · 실제 PuzzleManager 사용", 24f, theme.textSecondary,
                alignment: TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(note.rectTransform, new Vector2(0f, 0.5f), new Vector2(700f, 40f), new Vector2(378f, 0f));

            var summary = ui.Text("Summary", bar.transform, "", 28f, theme.textSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UIFactory.Anchor(summary.rectTransform, new Vector2(1f, 0.5f), new Vector2(500f, 60f), new Vector2(-48f, 0f));
            return summary;
        }

        private static (TMP_Text seed, TMP_InputField input, Button seedButton, Button regenerate) BuildSeedCard(UIFactory ui, Transform parent)
        {
            var theme = ui.Theme;
            var card = ui.Card("SeedCard", parent, new Vector2(620f, 220f), new Vector2(-330f, 272f));

            var header = ui.Text("Header", card, "현재 시드", 26f, theme.textSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(300f, 36f), new Vector2(28f, -22f));

            var seed = ui.Text("SeedValue", card, "-", 56f, theme.accent, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(seed.rectTransform, new Vector2(0f, 1f), new Vector2(560f, 64f), new Vector2(28f, -60f));

            var controls = UIFactory.Rect("Controls", card);
            UIFactory.AnchorBottom(controls, new Vector2(572f, 64f), 24f);
            UIFactory.Row(controls, 16f);

            var input = ui.InputField("SeedInput", controls, "시드 입력", 26f);
            ((RectTransform)input.transform).sizeDelta = new Vector2(220f, 64f);
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 9;

            var seedButton = ui.Button("SeedButton", controls, "시드로 생성", ButtonStyle.Secondary, 26f);
            ((RectTransform)seedButton.transform).sizeDelta = new Vector2(160f, 64f);

            var regenerate = ui.Button("RegenerateButton", controls, "새 보드", ButtonStyle.Primary, 26f);
            ((RectTransform)regenerate.transform).sizeDelta = new Vector2(160f, 64f);

            return (seed, input, seedButton, regenerate);
        }

        private static TMP_Text[] BuildStatsCard(UIFactory ui, Transform parent, GameDatabase database)
        {
            var theme = ui.Theme;
            var card = ui.Card("StatsCard", parent, new Vector2(620f, 220f), new Vector2(330f, 272f));

            var header = ui.Text("Header", card, "보석 분포", 26f, theme.textSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(300f, 36f), new Vector2(28f, -22f));

            var columns = UIFactory.Rect("Columns", card);
            UIFactory.AnchorBottom(columns, new Vector2(582f, 140f), 20f);
            UIFactory.Row(columns, 8f);

            var texts = new List<TMP_Text>();
            foreach (var type in StatTypes)
            {
                var gem = database.GetGem(type);
                var column = UIFactory.Rect(type.ToString(), columns);
                column.sizeDelta = new Vector2(110f, 140f);
                UIFactory.Column(column, 4f);

                ui.GemBadge("Badge", column, gem, 56f);

                var name = ui.Text("Name", column, gem != null ? gem.DisplayName : type.ToString(), 24f, theme.textPrimary, FontStyles.Bold);
                name.rectTransform.sizeDelta = new Vector2(110f, 32f);

                var count = ui.Text("Count", column, "-", 22f, theme.accent);
                count.rectTransform.sizeDelta = new Vector2(110f, 30f);
                texts.Add(count);
            }

            return texts.ToArray();
        }
    }
}
