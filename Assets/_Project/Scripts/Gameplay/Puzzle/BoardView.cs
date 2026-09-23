using System;
using System.Collections.Generic;
using ProjectP.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// Board를 화면에 그린다. 보드를 읽기만 하고 수정하지 않는다.
    /// 칸 배치는 Board 좌표를 그대로 따른다 (행 0 = 맨 아래, 열 0 = 맨 왼쪽).
    /// 입력(드래그 연결)은 6일차에 추가한다.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private RectTransform cellRoot;
        [SerializeField] private Sprite tileSprite;
        [SerializeField] private Sprite shadowSprite;
        [SerializeField] private float cellSize = 92f;
        [SerializeField] private float spacing = 4f;
        [SerializeField, Range(0.3f, 0.9f)] private float iconScale = 0.58f;

        private readonly List<CellView> cells = new List<CellView>();
        private int rows;
        private int columns;

        /// <summary>셀 크기와 간격을 반영한 보드 전체 크기.</summary>
        public Vector2 GetBoardSize(int boardRows, int boardColumns)
        {
            var pitch = cellSize + spacing;
            return new Vector2(boardColumns * pitch - spacing, boardRows * pitch - spacing);
        }

        public void Render(Board board, Func<GemType, GemData> gemLookup)
        {
            if (board.Rows != rows || board.Columns != columns) Rebuild(board.Rows, board.Columns);

            for (var i = 0; i < cells.Count; i++)
            {
                cells[i].Show(gemLookup(board[board.FromIndex(i)]));
            }
        }

        private void Rebuild(int boardRows, int boardColumns)
        {
            foreach (var cell in cells) Destroy(cell.Root);
            cells.Clear();

            rows = boardRows;
            columns = boardColumns;

            var size = GetBoardSize(rows, columns);
            cellRoot.sizeDelta = size;
            var pitch = cellSize + spacing;

            // Board 칸 번호 순서(행 0부터, 각 행은 열 0부터)와 같은 순서로 만든다.
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var position = new Vector2(
                        column * pitch + cellSize / 2f - size.x / 2f,
                        row * pitch + cellSize / 2f - size.y / 2f);
                    cells.Add(CreateCell($"Cell {row},{column}", position));
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
            shadow.rectTransform.sizeDelta = new Vector2(cellSize * 0.35f, cellSize * 0.35f);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            shadow.rectTransform.anchorMin = Vector2.zero;
            shadow.rectTransform.anchorMax = Vector2.one;

            var tile = CreateImage("Tile", rect, tileSprite, Color.white);
            tile.rectTransform.anchorMin = Vector2.zero;
            tile.rectTransform.anchorMax = Vector2.one;
            tile.rectTransform.sizeDelta = Vector2.zero;

            var icon = CreateImage("Icon", rect, null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(cellSize * iconScale, cellSize * iconScale);

            return new CellView(root, tile, icon);
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

        private sealed class CellView
        {
            private readonly Image tile;
            private readonly Image icon;

            public CellView(GameObject root, Image tile, Image icon)
            {
                Root = root;
                this.tile = tile;
                this.icon = icon;
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
        }
    }
}
