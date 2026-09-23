using System;
using System.Collections;
using System.Collections.Generic;
using ProjectP.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// Board를 화면에 그린다. 보드를 읽기만 하고 수정하지 않는다.
    /// 칸 배치는 BoardLayout을 따른다 (보드 중심 = 0, 행 0 = 맨 아래).
    /// 연결 선택 강조·확정 반짝임 표시 기능을 제공하고, 언제 쓸지는 ConnectionView가 정한다.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const float SelectedScale = 1.08f;
        private const float FlashDuration = 0.3f;

        [SerializeField] private RectTransform cellRoot;
        [SerializeField] private Sprite tileSprite;
        [SerializeField] private Sprite outlineSprite;
        [SerializeField] private Sprite shadowSprite;
        [SerializeField] private float cellSize = 92f;
        [SerializeField] private float spacing = 4f;
        [SerializeField, Range(0.3f, 0.9f)] private float iconScale = 0.58f;

        private readonly List<CellView> cells = new List<CellView>();
        private readonly HashSet<int> selected = new HashSet<int>();

        /// <summary>현재 보드의 배치 계산. 첫 Render 전에는 null.</summary>
        public BoardLayout Layout { get; private set; }

        public void Render(Board board, Func<GemType, GemData> gemLookup)
        {
            if (Layout == null || Layout.Rows != board.Rows || Layout.Columns != board.Columns) Rebuild(board.Rows, board.Columns);

            for (var i = 0; i < cells.Count; i++)
            {
                cells[i].Show(gemLookup(board[board.FromIndex(i)]));
            }

            SetSelection(Array.Empty<BoardPosition>());
        }

        /// <summary>칸 중심 위치 (cellRoot 기준 로컬 좌표).</summary>
        public Vector2 GetCellCenter(BoardPosition position)
        {
            var (x, y) = Layout.CellCenter(position);
            return new Vector2(x, y);
        }

        /// <summary>경로에 든 칸만 강조한다. 나머지는 원래대로 돌린다.</summary>
        public void SetSelection(IReadOnlyList<BoardPosition> positions)
        {
            if (Layout == null) return;

            selected.Clear();
            foreach (var position in positions) selected.Add(ToIndex(position));
            for (var i = 0; i < cells.Count; i++) cells[i].SetSelected(selected.Contains(i));
        }

        /// <summary>확정된 칸을 한 번 반짝인다.</summary>
        public void Flash(IReadOnlyList<BoardPosition> positions)
        {
            if (Layout == null || !isActiveAndEnabled) return;

            var targets = new List<CellView>();
            foreach (var position in positions) targets.Add(cells[ToIndex(position)]);
            StartCoroutine(FlashRoutine(targets));
        }

        private IEnumerator FlashRoutine(List<CellView> targets)
        {
            for (var time = 0f; time < FlashDuration; time += Time.unscaledDeltaTime)
            {
                var alpha = 0.85f * (1f - time / FlashDuration);
                foreach (var cell in targets) cell.SetFlash(alpha);
                yield return null;
            }

            foreach (var cell in targets) cell.SetFlash(0f);
        }

        private int ToIndex(BoardPosition position) => position.Row * Layout.Columns + position.Column;

        private void Rebuild(int rows, int columns)
        {
            foreach (var cell in cells) Destroy(cell.Root);
            cells.Clear();

            Layout = new BoardLayout(rows, columns, cellSize, spacing);
            cellRoot.sizeDelta = new Vector2(Layout.Width, Layout.Height);

            // Board 칸 번호 순서(행 0부터, 각 행은 열 0부터)와 같은 순서로 만든다.
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var position = new BoardPosition(row, column);
                    cells.Add(CreateCell($"Cell {row},{column}", GetCellCenter(position)));
                }
            }
        }

        private CellView CreateCell(string cellName, Vector2 position)
        {
            var root = new GameObject(cellName, typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(cellRoot, false);
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            rect.anchoredPosition = position;

            var shadow = CreateImage("Shadow", rect, shadowSprite, new Color(0f, 0f, 0f, 0.45f));
            shadow.type = Image.Type.Sliced;
            Stretch(shadow.rectTransform, cellSize * 0.17f);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -4f);

            var tile = CreateImage("Tile", rect, tileSprite, Color.white);
            Stretch(tile.rectTransform, 0f);

            var icon = CreateImage("Icon", rect, null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(cellSize * iconScale, cellSize * iconScale);

            var flash = CreateImage("Flash", rect, tileSprite, new Color(1f, 1f, 1f, 0f));
            Stretch(flash.rectTransform, 0f);

            var outline = CreateImage("Outline", rect, outlineSprite, Color.white);
            Stretch(outline.rectTransform, 3f);
            outline.enabled = false;

            return new CellView(root, tile, icon, flash, outline);
        }

        private static Image CreateImage(string imageName, RectTransform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(imageName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, float expand)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-expand, -expand);
            rect.offsetMax = new Vector2(expand, expand);
        }

        private sealed class CellView
        {
            private readonly Image tile;
            private readonly Image icon;
            private readonly Image flash;
            private readonly Image outline;

            public CellView(GameObject root, Image tile, Image icon, Image flash, Image outline)
            {
                Root = root;
                this.tile = tile;
                this.icon = icon;
                this.flash = flash;
                this.outline = outline;
            }

            public GameObject Root { get; }

            public void Show(GemData gem)
            {
                if (gem == null)
                {
                    // 정의가 없는 보석은 눈에 띄게 표시해 데이터 누락을 바로 알 수 있게 한다.
                    tile.color = Color.magenta;
                    icon.enabled = false;
                    return;
                }

                tile.color = gem.Color;
                icon.sprite = gem.Icon;
                icon.color = gem.IconColor;
                icon.enabled = gem.Icon != null;
            }

            public void SetSelected(bool isSelected)
            {
                outline.enabled = isSelected;
                Root.transform.localScale = Vector3.one * (isSelected ? SelectedScale : 1f);
                if (isSelected) Root.transform.SetAsLastSibling(); // 커진 칸이 이웃 칸에 가려지지 않게 맨 위로
            }

            public void SetFlash(float alpha) => flash.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
