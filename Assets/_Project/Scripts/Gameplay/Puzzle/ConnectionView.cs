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

        private readonly List<Image> segments = new List<Image>();
        private readonly List<Image> dots = new List<Image>();
        private RectTransform segmentLayer;
        private RectTransform dotLayer;

        private void Awake()
        {
            // 선이 점을 덮지 않도록 층을 나눈다: 선 → 점 → 개수 표시.
            segmentLayer = CreateLayer("Segments");
            dotLayer = CreateLayer("Dots");
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
