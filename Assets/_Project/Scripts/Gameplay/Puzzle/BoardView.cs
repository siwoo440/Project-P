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
    ///
    /// 연출:
    /// - 연결 선택 강조 (ConnectionView가 호출)
    /// - 사용한 보석 제거 → 남은 보석 낙하 → 새 보석이 위에서 떨어짐 (PuzzleManager가 호출)
    /// - 턴을 쓴 뒤 다음 턴 대기 상태면 보드를 어둡게
    /// 칸 오브젝트는 제자리에 두고 내용만 바꾼 뒤, 떨어져 온 거리만큼 위에서 출발시켜 낙하를 표현한다.
    /// 보드 밖에서 출발하는 새 보석은 보드 영역 마스크(RectMask2D)로 가린다.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const float SelectedScale = 1.08f;
        private const float PopDuration = 0.18f;
        private const float FallBaseDuration = 0.1f;
        private const float FallPerRowDuration = 0.05f;
        private const float LandDuration = 0.08f;
        private const float LockedAlpha = 0.55f;

        [SerializeField] private RectTransform cellRoot;
        [SerializeField] private Sprite tileSprite;
        [SerializeField] private Sprite outlineSprite;
        [SerializeField] private Sprite shadowSprite;
        [SerializeField] private float cellSize = 92f;
        [SerializeField] private float spacing = 4f;
        [SerializeField, Range(0.3f, 0.9f)] private float iconScale = 0.58f;

        private readonly List<CellView> cells = new List<CellView>();
        private readonly HashSet<int> selected = new HashSet<int>();
        private CanvasGroup group;

        /// <summary>현재 보드의 배치 계산. 첫 Render 전에는 null.</summary>
        public BoardLayout Layout { get; private set; }

        // Unity 오브젝트에는 ?? 연산자를 쓰지 않는다(Unity의 null 판정을 거치지 않음).
        private CanvasGroup Group
        {
            get
            {
                if (group == null) group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
                return group;
            }
        }

        public void Render(Board board, Func<GemType, GemData> gemLookup)
        {
            StopAllCoroutines();
            if (Layout == null || Layout.Rows != board.Rows || Layout.Columns != board.Columns) Rebuild(board.Rows, board.Columns);

            for (var i = 0; i < cells.Count; i++)
            {
                cells[i].ResetMotion();
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

        /// <summary>다음 턴 대기 중이면 보드를 어둡게 해 지금은 조작할 수 없음을 보여준다.</summary>
        public void SetLocked(bool locked) => Group.alpha = locked ? LockedAlpha : 1f;

        /// <summary>
        /// 사용한 보석을 터뜨리고, 바뀐 보드(board)대로 보석을 떨어뜨린다. 끝나면 onComplete를 부른다.
        /// board는 BoardGravity.Collapse가 이미 바꿔 놓은 상태여야 한다.
        /// </summary>
        public void PlayResolve(IReadOnlyList<BoardPosition> removed, IReadOnlyList<GemDrop> drops, Board board,
            Func<GemType, GemData> gemLookup, Action onComplete)
        {
            StartCoroutine(ResolveRoutine(removed, drops, board, gemLookup, onComplete));
        }

        private IEnumerator ResolveRoutine(IReadOnlyList<BoardPosition> removed, IReadOnlyList<GemDrop> drops, Board board,
            Func<GemType, GemData> gemLookup, Action onComplete)
        {
            // 1) 사용한 보석: 하얗게 번쩍이며 부풀었다가 사라진다.
            var popped = new List<CellView>();
            foreach (var position in removed) popped.Add(cells[ToIndex(position)]);

            for (var time = 0f; time < PopDuration; time += Time.unscaledDeltaTime)
            {
                var t = time / PopDuration;
                var scale = t < 0.35f ? Mathf.Lerp(1f, 1.2f, t / 0.35f) : Mathf.Lerp(1.2f, 0f, (t - 0.35f) / 0.65f);
                foreach (var cell in popped) cell.SetPop(scale, 0.85f * (1f - t));
                yield return null;
            }

            // 2) 바뀐 보드 내용을 칸에 채우고, 움직인 칸은 출발 위치에서 떨어뜨린다.
            for (var i = 0; i < cells.Count; i++)
            {
                cells[i].ResetMotion();
                cells[i].Show(gemLookup(board[board.FromIndex(i)]));
            }

            var falling = new List<(CellView cell, float height, float duration)>();
            var longest = 0f;
            foreach (var drop in drops)
            {
                var duration = FallBaseDuration + FallPerRowDuration * drop.Distance;
                falling.Add((cells[ToIndex(drop.To)], drop.Distance * Layout.Pitch, duration));
                longest = Mathf.Max(longest, duration);
            }

            for (var time = 0f; time < longest + LandDuration; time += Time.unscaledDeltaTime)
            {
                foreach (var (cell, height, duration) in falling)
                {
                    if (time < duration)
                    {
                        var t = time / duration;
                        cell.SetFall(height * (1f - t * t), 1f); // 점점 빨라지는 낙하(ease-in)
                    }
                    else
                    {
                        var land = Mathf.Clamp01((time - duration) / LandDuration);
                        cell.SetFall(0f, 1f - 0.12f * Mathf.Sin(land * Mathf.PI)); // 착지 때 살짝 눌림
                    }
                }

                yield return null;
            }

            foreach (var cell in cells) cell.ResetMotion();
            onComplete?.Invoke();
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

        private CellView CreateCell(string cellName, Vector2 home)
        {
            var root = new GameObject(cellName, typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(cellRoot, false);
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            rect.anchoredPosition = home;

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

            return new CellView(rect, home, tile, icon, flash, outline);
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
            private readonly RectTransform rect;
            private readonly Vector2 home;
            private readonly Image tile;
            private readonly Image icon;
            private readonly Image flash;
            private readonly Image outline;

            public CellView(RectTransform rect, Vector2 home, Image tile, Image icon, Image flash, Image outline)
            {
                this.rect = rect;
                this.home = home;
                this.tile = tile;
                this.icon = icon;
                this.flash = flash;
                this.outline = outline;
            }

            public GameObject Root => rect.gameObject;

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
                rect.localScale = Vector3.one * (isSelected ? SelectedScale : 1f);
                if (isSelected) rect.SetAsLastSibling(); // 커진 칸이 이웃 칸에 가려지지 않게 맨 위로
            }

            public void SetPop(float scale, float flashAlpha)
            {
                outline.enabled = false;
                rect.localScale = Vector3.one * scale;
                flash.color = new Color(1f, 1f, 1f, flashAlpha);
            }

            public void SetFall(float height, float squash)
            {
                rect.anchoredPosition = home + new Vector2(0f, height);
                rect.localScale = new Vector3(2f - squash, squash, 1f);
            }

            public void ResetMotion()
            {
                rect.anchoredPosition = home;
                rect.localScale = Vector3.one;
                flash.color = new Color(1f, 1f, 1f, 0f);
                outline.enabled = false;
            }
        }
    }
}
