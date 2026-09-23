using System;
using System.Collections.Generic;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보드의 화면 배치 계산. Unity에 의존하지 않는 순수 C#이라 자동 테스트할 수 있다.
    /// 좌표는 보드 중심이 (0, 0)이고 위가 +다. 칸 배치는 Board 좌표를 따른다(행 0 = 맨 아래).
    /// </summary>
    public sealed class BoardLayout
    {
        public BoardLayout(int rows, int columns, float cellSize, float spacing)
        {
            if (rows <= 0 || columns <= 0) throw new ArgumentOutOfRangeException(nameof(rows), "보드 크기는 1 이상이어야 합니다.");
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "칸 크기는 0보다 커야 합니다.");

            Rows = rows;
            Columns = columns;
            CellSize = cellSize;
            Spacing = spacing;
        }

        public int Rows { get; }
        public int Columns { get; }
        public float CellSize { get; }
        public float Spacing { get; }
        public float Pitch => CellSize + Spacing;
        public float Width => Columns * Pitch - Spacing;
        public float Height => Rows * Pitch - Spacing;

        public (float x, float y) CellCenter(BoardPosition position) =>
            (position.Column * Pitch + CellSize / 2f - Width / 2f,
             position.Row * Pitch + CellSize / 2f - Height / 2f);

        /// <summary>
        /// 점이 칸 중심에서 (칸 절반 × hitRatio) 반지름 원 안에 있으면 그 칸을 돌려준다.
        /// 원 밖(칸 모서리·칸 사이)은 선택되지 않으므로 대각선으로 그을 때 옆 칸이 잘못 잡히지 않는다.
        /// </summary>
        public bool TryHit(float x, float y, float hitRatio, out BoardPosition position)
        {
            var column = (int)Math.Floor((x + Width / 2f) / Pitch);
            var row = (int)Math.Floor((y + Height / 2f) / Pitch);
            position = new BoardPosition(row, column);
            if (row < 0 || row >= Rows || column < 0 || column >= Columns) return false;

            var (cx, cy) = CellCenter(position);
            var radius = CellSize / 2f * hitRatio;
            return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius;
        }

        /// <summary>
        /// 선분 (x0,y0)→(x1,y1)을 촘촘히 훑어 지나간 칸을 순서대로 돌려준다(연속 중복 제거).
        /// 빠르게 드래그해 한 프레임에 여러 칸을 건너뛰어도 지나간 칸을 놓치지 않기 위함이다.
        /// </summary>
        public List<BoardPosition> HitsAlong(float x0, float y0, float x1, float y1, float hitRatio)
        {
            var hits = new List<BoardPosition>();
            var distance = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            var steps = Math.Max(1, (int)Math.Ceiling(distance / (CellSize * 0.25f)));

            for (var i = 1; i <= steps; i++)
            {
                var t = (float)i / steps;
                if (!TryHit(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, hitRatio, out var position)) continue;
                if (hits.Count == 0 || hits[hits.Count - 1] != position) hits.Add(position);
            }

            return hits;
        }
    }
}
