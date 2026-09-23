using System.Collections.Generic;
using System.Linq;
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

        // 상단 카드 3개: 시드 / 허수아비 / 연결 기록. 보석 분포는 보드 오른쪽 세로 패널(사용자 요청으로 자리 바꿈).
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
            var (statusText, logText) = BuildLogCard(ui, root.transform);

            // 보드 카드: 행·열 수는 PuzzleConfig를 따른다.
            var pitch = CellSize + Spacing;
            var boardSize = new Vector2(config.Columns * pitch - Spacing, config.Rows * pitch - Spacing);
            var cardSize = boardSize + Vector2.one * (BoardPadding * 2f);
            var cardY = -540f + 36f + cardSize.y / 2f;
            // 양옆 세로 칸: 상단 카드 아래부터 보드 카드 아랫변까지.
            var side = new SideColumn(
                cardSize.x / 2f + 42f + SideWidth / 2f,
                CardY - CardSize.y / 2f - 16f,
                cardY - cardSize.y / 2f);

            // 보석 분포: 보드 오른쪽 세로 패널 (허수아비와 자리를 바꿈, 사용자 요청). 아래에 연속 보너스 안내.
            var (statTexts, summary) = BuildStatsPanel(ui, root.transform, database, config, side);

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
                ("cellRoot", board), ("tileSprite", theme.gemTile), ("outlineSprite", theme.gemTileOutline), ("shadowSprite", theme.shadow),
                ("glowSprite", theme.glow));
            SetColor(boardView, "specialColor", theme.accent);

            // 연결선 층: 보드 위에 같은 크기로 겹친다.
            var overlay = UIFactory.Rect("Connection", boardCard);
            UIFactory.Place(overlay, boardSize, Vector2.zero);
            var connectionView = overlay.gameObject.AddComponent<ConnectionView>();
            var (badge, badgeText) = CountBadge(ui, overlay);
            UIFactory.Wire(connectionView,
                ("puzzle", puzzle), ("boardView", boardView), ("lineRoot", overlay), ("dotSprite", theme.circle),
                ("countBadge", badge), ("countText", badgeText), ("tagSprite", theme.rounded));
            SetColor(connectionView, "lineColor", BootUIBuilder.WithAlpha(theme.accent, 0.85f));
            SetColor(connectionView, "tagColor", BootUIBuilder.WithAlpha(theme.surface, 0.92f));
            SetColor(connectionView, "tagTextColor", theme.accent);
            SetColor(connectionView, "comboTagColor", theme.accentSecondary);

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

            // 보석 효과 확인: 보드 왼쪽 위 메인 캐릭터·아래 연결 효과, 상단 가운데 허수아비, 떠오르는 숫자 층.
            BuildEffectPanels(ui, root, puzzle, side);

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

        /// <summary>
        /// 보석 분포 세로 패널(오른쪽 칸 전체): 제목 · "6 × 12 · 72칸 · 특수 0" · 종류별 한 줄 [보석] [이름] [N개 · N%]
        /// · 아래에 연속 보너스 안내(값은 PuzzleConfig에서 읽는다).
        /// </summary>
        private static (TMP_Text[] counts, TMP_Text summary) BuildStatsPanel(UIFactory ui, Transform parent, GameDatabase database, PuzzleConfig config,
            SideColumn side)
        {
            var theme = ui.Theme;
            var panel = ui.Card("StatsPanel", parent, new Vector2(SideWidth, side.Height), new Vector2(side.X, side.Center));
            SidePanelHeader(ui, panel, "보석 분포");

            // 연속 보너스 안내: 5종 줄(~454) 아래.
            ui.Divider(panel, 252f, 474f);
            var legendHeader = ui.Text("LegendHeader", panel, "연속 보너스", 22f, theme.textSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.AnchorTop(legendHeader.rectTransform, new Vector2(252f, 30f), 488f);
            var legend = ui.Text("Legend", panel,
                $"같은 종류 2번째부터 <color=#{Hex(theme.accent)}>+{config.ChainBonusPerGem * 100f:0}%</color>씩 누적\n" +
                $"<color=#{Hex(theme.accentSecondary)}>{config.ComboLength}연속</color> → 강공격 등 연속 강화\n" +
                $"<color=#{Hex(theme.accent)}>{config.SpecialLength}연속</color> → 마지막 칸에 특수 보석\n" +
                "칸 옆 ×1.2 = 그 보석이 받는 배율",
                18f, theme.textPrimary, alignment: TextAlignmentOptions.TopLeft);
            UIFactory.AnchorTop(legend.rectTransform, new Vector2(252f, 130f), 522f);
            legend.richText = true;
            legend.lineSpacing = 12f;

            var summary = ui.Text("Summary", panel, "", 20f, theme.textSecondary);
            UIFactory.AnchorTop(summary.rectTransform, new Vector2(252f, 26f), 58f);
            ui.Divider(panel, 252f, 96f);

            var texts = new List<TMP_Text>();
            for (var i = 0; i < StatTypes.Length; i++)
            {
                var type = StatTypes[i];
                var gem = database.GetGem(type);
                var row = UIFactory.Rect(type.ToString(), panel);
                UIFactory.AnchorTop(row, new Vector2(252f, 56f), 114f + i * 68f);
                UIFactory.Row(row, 12f, TextAnchor.MiddleLeft);

                ui.GemBadge("Badge", row, gem, 48f);

                var name = ui.Text("Name", row, gem != null ? gem.DisplayName : type.ToString(), 24f, theme.textPrimary, FontStyles.Bold,
                    TextAlignmentOptions.MidlineLeft);
                name.rectTransform.sizeDelta = new Vector2(70f, 40f);

                var count = ui.Text("Count", row, "-", 22f, theme.accent, alignment: TextAlignmentOptions.MidlineRight);
                count.rectTransform.sizeDelta = new Vector2(110f, 40f);
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

            var log = ui.Text("Log", card, "", 22f, theme.textPrimary, alignment: TextAlignmentOptions.TopLeft);
            UIFactory.Anchor(log.rectTransform, new Vector2(0f, 1f), new Vector2(524f, 150f), new Vector2(28f, -68f));
            log.richText = true;
            log.textWrappingMode = TextWrappingModes.NoWrap; // 한 기록 = 한 줄. 길면 말줄임
            log.overflowMode = TextOverflowModes.Ellipsis;
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

        private const float SideWidth = 300f;
        private const float SideGap = 16f;
        private const float MainPanelHeight = 318f;

        /// <summary>보드 양옆 세로 칸. X = 가운데에서 떨어진 거리(오른쪽 +), Top·Bottom = 캔버스 가운데 기준 y.</summary>
        private readonly struct SideColumn
        {
            public SideColumn(float x, float top, float bottom)
            {
                X = x;
                Top = top;
                Bottom = bottom;
            }

            public float X { get; }
            public float Top { get; }
            public float Bottom { get; }
            public float Height => Top - Bottom;
            public float Center => (Top + Bottom) / 2f;
        }

        /// <summary>
        /// 보석 효과 확인 화면(개발용):
        /// - 보드 왼쪽 위 메인 캐릭터(HP·임시 스탯 −/+·HP −20), 왼쪽 아래 연결 효과 실시간 표시(9일차, 사용자 요청)
        /// - 상단 가운데 허수아비 3마리(왼쪽부터 순번 0·1·2, HP·행동 카운트·되살리기). 실제 전투처럼 적이 보드 위에 선다.
        /// 왼쪽 패널들은 보이지 않는 취소 영역 위에 있다(그 위에서 놓으면 취소).
        /// </summary>
        private static void BuildEffectPanels(UIFactory ui, GameObject root, PuzzleManager puzzle, SideColumn side)
        {
            var theme = ui.Theme;
            var healColor = new Color32(48, 192, 122, 255);

            // 메인 캐릭터: 왼쪽 칸 위. 제목 줄 오른쪽에 HP -20.
            var main = ui.Card("MainPanel", root.transform, new Vector2(SideWidth, MainPanelHeight),
                new Vector2(-side.X, side.Top - MainPanelHeight / 2f));
            CardHeader(ui, main, "메인 캐릭터");
            var (mainHpText, mainBar, mainFill) = HpRow(ui, main, healColor);
            ui.Divider(main, 252f, 140f);

            var hurt = ui.Button("HurtButton", main, "HP -20", ButtonStyle.Secondary, 20f);
            UIFactory.Anchor((RectTransform)hurt.transform, new Vector2(1f, 1f), new Vector2(96f, 38f), new Vector2(-20f, -18f));

            // 연결 효과: 왼쪽 칸 아래 나머지.
            var previewHeight = side.Height - MainPanelHeight - SideGap;
            BuildPreviewPanel(ui, root.transform, puzzle, new Vector2(SideWidth, previewHeight), new Vector2(-side.X, side.Bottom + previewHeight / 2f));

            var statNames = new[] { "물리 공격", "마법 공격", "회복력" };
            var statTexts = new Object[3];
            var minusButtons = new Object[3];
            var plusButtons = new Object[3];
            for (var i = 0; i < statNames.Length; i++)
            {
                var row = UIFactory.Rect($"Stat{i}", main);
                UIFactory.AnchorTop(row, new Vector2(252f, 44f), 156f + i * 52f);
                UIFactory.Row(row, 12f, TextAnchor.MiddleLeft);

                ui.Text("Label", row, statNames[i], 22f, theme.textSecondary, alignment: TextAlignmentOptions.MidlineLeft)
                    .rectTransform.sizeDelta = new Vector2(96f, 44f);
                var minus = ui.Button("Minus", row, "-", ButtonStyle.Secondary, 26f);
                ((RectTransform)minus.transform).sizeDelta = new Vector2(44f, 44f);
                var value = ui.Text("Value", row, "0", 26f, theme.accent, FontStyles.Bold);
                value.rectTransform.sizeDelta = new Vector2(32f, 44f);
                var plus = ui.Button("Plus", row, "+", ButtonStyle.Secondary, 26f);
                ((RectTransform)plus.transform).sizeDelta = new Vector2(44f, 44f);

                statTexts[i] = value;
                minusButtons[i] = minus;
                plusButtons[i] = plus;
            }

            // 허수아비 3마리: 상단 가운데 카드(원래 보석 분포 자리). 클릭하면 공격 대상. 왼쪽부터 순번 0·1·2.
            var enemies = ui.Card("DummyPanel", root.transform, CardSize, new Vector2(0f, CardY));
            CardHeader(ui, enemies, "허수아비");
            var clickHint = ui.Text("ClickHint", enemies, "클릭해 공격 대상 지정", 18f, theme.textSecondary, alignment: TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(clickHint.rectTransform, new Vector2(0f, 1f), new Vector2(200f, 36f), new Vector2(150f, -22f));

            var revive = ui.Button("ReviveButton", enemies, "모두 되살리기", ButtonStyle.Secondary, 20f);
            UIFactory.Anchor((RectTransform)revive.transform, new Vector2(1f, 1f), new Vector2(150f, 40f), new Vector2(-20f, -16f));

            // 칸 위치: 제목 줄(16~58) 아래. 칸 윗변에 걸치는 "대상" 표시(칸 위 26px)가 제목 줄과 겹치지 않게 86부터.
            var rows = new DummyRow[DummyNames.Length];
            var pitch = DummyRowSize.x + DummyRowGap;
            for (var i = 0; i < DummyNames.Length; i++)
            {
                var x = (i - (DummyNames.Length - 1) / 2f) * pitch;
                rows[i] = BuildDummyRow(ui, enemies, DummyNames[i], new Vector2(x, 86f));
            }

            // 떠오르는 숫자는 패널 위에 그린다.
            var floatRoot = UIFactory.Rect("FloatText", root.transform);
            UIFactory.Stretch(floatRoot);

            var panel = root.AddComponent<EffectTestPanel>();
            UIFactory.Wire(panel,
                ("puzzle", puzzle),
                ("mainHpFill", mainFill), ("mainHpText", mainHpText), ("mainFloatAnchor", mainBar), ("hurtButton", hurt),
                ("reviveButton", revive), ("floatRoot", floatRoot));
            UIFactory.WireArray(panel, "statTexts", statTexts);
            UIFactory.WireArray(panel, "statMinusButtons", minusButtons);
            UIFactory.WireArray(panel, "statPlusButtons", plusButtons);
            UIFactory.WireArray(panel, "dummyButtons", rows.Select(row => (Object)row.Button).ToArray());
            UIFactory.WireArray(panel, "dummyHpFills", rows.Select(row => (Object)row.HpFill).ToArray());
            UIFactory.WireArray(panel, "dummyHpTexts", rows.Select(row => (Object)row.HpText).ToArray());
            UIFactory.WireArray(panel, "dummyCountTexts", rows.Select(row => (Object)row.CountText).ToArray());
            UIFactory.WireArray(panel, "dummyBorders", rows.Select(row => (Object)row.Border).ToArray());
            UIFactory.WireArray(panel, "dummyTargetMarks", rows.Select(row => (Object)row.TargetMark).ToArray());
            UIFactory.WireArray(panel, "dummyGroups", rows.Select(row => (Object)row.Group).ToArray());
            SetColor(panel, "healColor", new Color32(80, 220, 150, 255));
            SetColor(panel, "actionPointColor", theme.accentSecondary);
            SetColor(panel, "targetBorderColor", theme.accent);
            SetColor(panel, "normalBorderColor", theme.outline);
        }

        private static readonly string[] DummyNames = { "허수아비 A", "허수아비 B", "허수아비 C" };
        // 카드 너비 580 = 여백 20 + 칸 172 × 3 + 간격 12 × 2 + 여백 20
        private static readonly Vector2 DummyRowSize = new Vector2(172f, 118f);
        private const float DummyRowGap = 12f;

        private readonly struct DummyRow
        {
            public DummyRow(Button button, RectTransform hpFill, TMP_Text hpText, TMP_Text countText, Image border, GameObject targetMark, CanvasGroup group)
            {
                Button = button;
                HpFill = hpFill;
                HpText = hpText;
                CountText = countText;
                Border = border;
                TargetMark = targetMark;
                Group = group;
            }

            public Button Button { get; }
            public RectTransform HpFill { get; }
            public TMP_Text HpText { get; }
            public TMP_Text CountText { get; }
            public Image Border { get; }
            public GameObject TargetMark { get; }
            public CanvasGroup Group { get; }
        }

        /// <summary>허수아비 한 칸: [이름 · 행동까지 N턴] [HP 막대] [HP 숫자]. 칸 전체가 버튼이고, 대상이면 금색 테두리 + "대상" 표시.</summary>
        /// <param name="position">x = 카드 가운데 기준 가로 위치, y = 카드 윗변에서 내려온 거리</param>
        private static DummyRow BuildDummyRow(UIFactory ui, RectTransform parent, string name, Vector2 position)
        {
            var theme = ui.Theme;
            var row = UIFactory.Rect(name, parent);
            UIFactory.Anchor(row, new Vector2(0.5f, 1f), DummyRowSize, new Vector2(position.x, -position.y));
            var group = row.gameObject.AddComponent<CanvasGroup>();

            var body = UIFactory.Image("Body", row, theme.rounded, theme.surfaceRaised, sliced: true, raycast: true);
            UIFactory.Stretch(body.rectTransform);
            var border = UIFactory.Image("Border", row, theme.roundedOutline, theme.outline, sliced: true);
            UIFactory.Stretch(border.rectTransform);

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = UIFactory.StandardColors;

            // 세로로 가운데 정렬: 이름 → 행동까지 N턴 → HP 막대 → HP 숫자
            var inner = DummyRowSize.x - 28f;
            var title = ui.Text("Name", row, name, 22f, theme.textPrimary, FontStyles.Bold);
            UIFactory.AnchorTop(title.rectTransform, new Vector2(inner, 28f), 12f);

            var count = ui.Text("Count", row, "-", 18f, theme.accent, FontStyles.Bold);
            UIFactory.AnchorTop(count.rectTransform, new Vector2(inner, 24f), 42f);

            var (bar, fill) = ui.Bar("HpBar", row, new Vector2(inner, 12f), theme.danger);
            UIFactory.AnchorTop(bar, new Vector2(inner, 12f), 72f);

            var hp = ui.Text("Hp", row, "-", 18f, theme.textSecondary);
            UIFactory.AnchorTop(hp.rectTransform, new Vector2(inner, 22f), 88f);

            // 대상 표시: 칸 윗변에 걸친 금색 알약.
            var mark = ui.Pill("TargetMark", row, "대상", theme.accent, new Vector2(64f, 26f));
            UIFactory.Anchor(mark, new Vector2(0.5f, 1f), mark.sizeDelta, new Vector2(0f, 13f));

            return new DummyRow(button, fill, hp, count, border, mark.gameObject, group);
        }

        /// <summary>
        /// 연결 효과 실시간 표시(사용자 요청): 드래그 중 지금 놓으면 받을 효과.
        /// [마지막 보석] [×1.4 / 물리 3연속] · 다음 안내 · 피해·회복·지연·행동력 합계 · 발동한 연속 강화·특수 보석.
        /// 잇지 않을 때는 안내 문구만 보인다.
        /// </summary>
        private static void BuildPreviewPanel(UIFactory ui, Transform parent, PuzzleManager puzzle, Vector2 size, Vector2 position)
        {
            var theme = ui.Theme;
            var panel = ui.Card("PreviewPanel", parent, size, position);
            CardHeader(ui, panel, "연결 효과");

            var idle = UIFactory.Rect("Idle", panel);
            UIFactory.Stretch(idle);
            var idleText = ui.Text("Text", idle, "보석을 이으면\n받는 효과가 여기에 보입니다", 20f, theme.textSecondary);
            UIFactory.Place(idleText.rectTransform, new Vector2(252f, 80f), new Vector2(0f, -10f));

            var active = UIFactory.Rect("Active", panel);
            UIFactory.Stretch(active);

            // 마지막 보석 + 배율
            var tile = UIFactory.Image("GemTile", active, theme.gemTile, Color.white);
            UIFactory.Anchor(tile.rectTransform, new Vector2(0f, 1f), new Vector2(64f, 64f), new Vector2(24f, -64f));
            var icon = UIFactory.Image("Icon", tile.transform, null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(38f, 38f);
            var ring = UIFactory.Image("SpecialRing", tile.transform, theme.gemTileOutline, theme.accent);
            UIFactory.Stretch(ring.rectTransform, 3f);

            var multiplier = ui.Text("Multiplier", active, "×1.0", 44f, theme.accent, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(multiplier.rectTransform, new Vector2(0f, 1f), new Vector2(172f, 46f), new Vector2(104f, -60f));
            var chain = ui.Text("Chain", active, "-", 22f, theme.textPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(chain.rectTransform, new Vector2(0f, 1f), new Vector2(172f, 26f), new Vector2(104f, -104f));

            var next = ui.Text("Next", active, "-", 19f, theme.textSecondary, alignment: TextAlignmentOptions.TopLeft);
            UIFactory.Anchor(next.rectTransform, new Vector2(0f, 1f), new Vector2(252f, 50f), new Vector2(24f, -140f));
            next.richText = true;

            ui.Divider(active, 252f, 196f);

            // 합계 2×2: 피해 · 회복 / 지연 · 행동력
            var damage = TotalCell(ui, active, "피해", new Color32(255, 110, 110, 255), new Vector2(24f, -208f));
            var heal = TotalCell(ui, active, "회복", new Color32(80, 220, 150, 255), new Vector2(150f, -208f));
            var delay = TotalCell(ui, active, "지연", new Color32(180, 150, 255, 255), new Vector2(24f, -242f));
            var actionPoints = TotalCell(ui, active, "행동력", theme.accentSecondary, new Vector2(150f, -242f));

            var bonus = ui.Text("Bonus", active, "-", 19f, theme.accent, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(bonus.rectTransform, new Vector2(0f, 1f), new Vector2(252f, 28f), new Vector2(24f, -280f));

            var view = panel.gameObject.AddComponent<ConnectionPreviewView>();
            UIFactory.Wire(view,
                ("puzzle", puzzle), ("idleGroup", idle.gameObject), ("activeGroup", active.gameObject),
                ("gemTile", tile), ("gemIcon", icon), ("gemSpecialRing", ring.gameObject),
                ("multiplierText", multiplier), ("chainText", chain), ("nextText", next),
                ("damageText", damage), ("healText", heal), ("delayText", delay), ("actionPointText", actionPoints), ("bonusText", bonus));
            SetColor(view, "baseColor", theme.textSecondary);
            SetColor(view, "bonusColor", theme.accent);
            SetColor(view, "comboColor", theme.accentSecondary);
        }

        /// <summary>합계 한 칸: [이름] [값]. 값 글자를 돌려준다.</summary>
        private static TMP_Text TotalCell(UIFactory ui, RectTransform parent, string label, Color valueColor, Vector2 topLeft)
        {
            var name = ui.Text($"{label}Label", parent, label, 20f, ui.Theme.textSecondary, alignment: TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(62f, 30f), topLeft);

            var value = ui.Text($"{label}Value", parent, "0", 24f, valueColor, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(value.rectTransform, new Vector2(0f, 1f), new Vector2(60f, 30f), topLeft + new Vector2(64f, 0f));
            return value;
        }

        private static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        private static void SidePanelHeader(UIFactory ui, RectTransform panel, string text)
        {
            var header = ui.Text("Header", panel, text, 26f, ui.Theme.textPrimary, FontStyles.Bold);
            UIFactory.AnchorTop(header.rectTransform, new Vector2(252f, 36f), 20f);
        }

        /// <summary>"HP  60 / 100" 한 줄 + 그 아래 막대.</summary>
        private static (TMP_Text value, RectTransform bar, RectTransform fill) HpRow(UIFactory ui, RectTransform panel, Color fillColor)
        {
            var label = ui.Text("HpLabel", panel, "HP", 24f, ui.Theme.textSecondary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(100f, 36f), new Vector2(24f, -64f));

            var value = ui.Text("HpValue", panel, "-", 26f, ui.Theme.textPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UIFactory.Anchor(value.rectTransform, new Vector2(1f, 1f), new Vector2(170f, 36f), new Vector2(-24f, -64f));

            var (bar, fill) = ui.Bar("HpBar", panel, new Vector2(252f, 18f), fillColor);
            UIFactory.AnchorTop(bar, new Vector2(252f, 18f), 108f);
            return (value, bar, fill);
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
