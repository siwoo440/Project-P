using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 연결 경로 실시간 표시. 기획서 9.5 — 선택한 보석과 연결선을 실시간으로 보여준다.
    ///
    /// PuzzleManager 이벤트를 받아 그리기만 한다. 선 색은 혼합 연결이 기본이라 한 가지(강조색)로 통일한다.
    /// 이 오브젝트는 BoardView의 cellRoot와 같은 위치·크기로 보드 위에 겹쳐 둔다.
    /// </summary>
    public class ConnectionView : MonoBehaviour
    {
        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private BoardView boardView;
        [SerializeField] private RectTransform lineRoot;
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Color lineColor = new Color32(242, 193, 78, 235);
        // 선택 표시는 칸 외곽선·확대가 맡고, 선은 보석 아이콘을 가리지 않도록 가늘게 둔다.
        [SerializeField] private float lineWidth = 9f;
        [SerializeField] private float dotSize = 15f;
        [SerializeField] private RectTransform countBadge;
        [SerializeField] private TMP_Text countText;

        [Header("칸별 배율 표시 (9일차)")]
        [Tooltip("연결한 칸 오른쪽 아래에 붙는 ×1.2 표시의 바탕")]
        [SerializeField] private Sprite tagSprite;
        [SerializeField] private Color tagColor = new Color32(26, 31, 51, 235);
        [SerializeField] private Color tagTextColor = new Color32(242, 193, 78, 255);
        [Tooltip("연속 강화가 터지는 칸(4연속째)")]
        [SerializeField] private Color comboTagColor = new Color32(79, 195, 247, 255);
        [SerializeField] private Vector2 tagOffset = new Vector2(26f, -32f);

        private readonly List<Image> segments = new List<Image>();
        private readonly List<Image> dots = new List<Image>();
        private readonly List<(Image body, TMP_Text label)> tags = new List<(Image, TMP_Text)>();
        private RectTransform segmentLayer;
        private RectTransform dotLayer;
        private RectTransform tagLayer;

        private void Awake()
        {
            // 선이 점을 덮지 않도록 층을 나눈다: 선 → 점 → 칸별 배율 → 개수 표시.
            segmentLayer = CreateLayer("Segments");
            dotLayer = CreateLayer("Dots");
            tagLayer = CreateLayer("Tags");
        }

        private void OnEnable()
        {
            puzzle.ConnectionChanged += OnChanged;
            puzzle.ConnectionConfirmed += OnEnded;
            puzzle.ConnectionWasted += OnEnded;
            puzzle.ConnectionCanceled += Clear;
            puzzle.BoardChanged += OnBoardChanged;
            Clear();
        }

        private void OnDisable()
        {
            puzzle.ConnectionChanged -= OnChanged;
            puzzle.ConnectionConfirmed -= OnEnded;
            puzzle.ConnectionWasted -= OnEnded;
            puzzle.ConnectionCanceled -= Clear;
            puzzle.BoardChanged -= OnBoardChanged;
        }

        private void OnBoardChanged(Board board) => Clear();

        private void OnChanged(IReadOnlyList<BoardPosition> positions)
        {
            boardView.SetSelection(positions);
            Draw(positions);
        }

        // 사용한 보석의 터짐·낙하 연출은 PuzzleManager가 BoardView에 맡긴다. 여기서는 선만 지운다.
        private void OnEnded(IReadOnlyList<BoardPosition> positions) => Clear();

        private void Clear()
        {
            boardView.SetSelection(System.Array.Empty<BoardPosition>());
            foreach (var segment in segments) segment.enabled = false;
            foreach (var dot in dots) dot.enabled = false;
            foreach (var tag in tags) tag.body.gameObject.SetActive(false);
            countBadge.gameObject.SetActive(false);
        }

        private void Draw(IReadOnlyList<BoardPosition> positions)
        {
            var points = new Vector2[positions.Count];
            for (var i = 0; i < positions.Count; i++) points[i] = boardView.GetCellCenter(positions[i]);

            for (var i = 0; i < Mathf.Max(segments.Count, points.Length - 1); i++)
            {
                if (i >= points.Length - 1)
                {
                    segments[i].enabled = false;
                    continue;
                }

                var segment = Get(segments, segmentLayer, i, "Segment", null);
                var delta = points[i + 1] - points[i];
                segment.rectTransform.anchoredPosition = (points[i] + points[i + 1]) / 2f;
                segment.rectTransform.sizeDelta = new Vector2(delta.magnitude, lineWidth);
                segment.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                segment.enabled = true;
            }

            for (var i = 0; i < Mathf.Max(dots.Count, points.Length); i++)
            {
                if (i >= points.Length)
                {
                    dots[i].enabled = false;
                    continue;
                }

                var dot = Get(dots, dotLayer, i, "Dot", dotSprite);
                dot.rectTransform.anchoredPosition = points[i];
                dot.rectTransform.sizeDelta = Vector2.one * (i == points.Length - 1 ? dotSize * 1.45f : dotSize);
                dot.enabled = true;
            }

            countBadge.gameObject.SetActive(true);
            countBadge.SetAsLastSibling();
            countBadge.anchoredPosition = points[points.Length - 1] + new Vector2(44f, 44f);
            countText.text = $"{positions.Count}개";

            DrawTags(positions, points);
        }

        /// <summary>
        /// 칸별 배율 표시(사용자 요청): 같은 종류 2번째부터 그 칸이 받는 연속 배율(×1.2 · ×1.4 …)을 칸 오른쪽 아래에 붙인다.
        /// 연속 강화가 터지는 칸(4연속째)은 다른 색, 특수 보석 칸은 "특수"로 표시한다. 배율 1.0인 칸은 표시하지 않는다.
        /// </summary>
        private void DrawTags(IReadOnlyList<BoardPosition> positions, Vector2[] points)
        {
            var preview = puzzle.PreviewEffects();
            var comboAt = new HashSet<int>();
            if (preview != null)
            {
                foreach (var effect in preview.Events)
                {
                    if (effect.Source == EffectSource.Combo) comboAt.Add(effect.Index);
                }
            }

            var used = 0;
            for (var i = 0; i < positions.Count; i++)
            {
                string text;
                if (puzzle.Board.IsSpecial(positions[i])) text = "특수";
                else if (preview != null && i < preview.ChainMultipliers.Count && preview.ChainMultipliers[i] > 1.001f) text = $"×{preview.ChainMultipliers[i]:0.0}";
                else continue;

                var (body, label) = GetTag(used++);
                body.rectTransform.anchoredPosition = points[i] + tagOffset;
                label.text = text;
                label.color = comboAt.Contains(i) ? comboTagColor : tagTextColor;
                body.gameObject.SetActive(true);
            }

            for (var i = used; i < tags.Count; i++) tags[i].body.gameObject.SetActive(false);
        }

        private (Image body, TMP_Text label) GetTag(int index)
        {
            while (tags.Count <= index)
            {
                var go = new GameObject($"Tag {tags.Count}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(tagLayer, false);
                var body = go.GetComponent<Image>();
                body.sprite = tagSprite;
                body.type = Image.Type.Sliced;
                body.pixelsPerUnitMultiplier = 2f;
                body.color = tagColor;
                body.raycastTarget = false;
                body.rectTransform.sizeDelta = new Vector2(58f, 28f);

                var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(go.transform, false);
                var rect = (RectTransform)textObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var label = textObject.GetComponent<TextMeshProUGUI>();
                label.fontSize = 19f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;

                tags.Add((body, label));
            }

            return tags[index];
        }

        private RectTransform CreateLayer(string layerName)
        {
            var layer = (RectTransform)new GameObject(layerName, typeof(RectTransform)).transform;
            layer.SetParent(lineRoot, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            return layer;
        }

        /// <summary>선·점 이미지는 미리 만들지 않고 필요할 때 만들어 재사용한다.</summary>
        private Image Get(List<Image> pool, RectTransform layer, int index, string imageName, Sprite sprite)
        {
            while (pool.Count <= index)
            {
                var go = new GameObject($"{imageName} {pool.Count}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(layer, false);
                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.color = lineColor;
                image.raycastTarget = false;
                pool.Add(image);
            }

            return pool[index];
        }
    }
}
