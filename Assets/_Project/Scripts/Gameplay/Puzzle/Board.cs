using System;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보드 규칙 모델. Unity에 의존하지 않는 순수 C#이라 Play 없이 자동 테스트할 수 있다.
    ///
    /// - 좌표는 BoardPosition 기준 (행 0 = 맨 아래, 열 0 = 맨 왼쪽).
    /// - 칸 번호(index)는 row * Columns + column. 맨 아래 행 왼쪽부터 오른쪽으로 센다.
    /// - 화면(BoardView)은 이 모델을 읽기만 한다.
    /// </summary>
    public sealed class Board
    {
        private readonly GemType[] cells;

        public Board(int rows, int columns)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), rows, "행 수는 1 이상이어야 합니다.");
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns), columns, "열 수는 1 이상이어야 합니다.");

            Rows = rows;
            Columns = columns;
            cells = new GemType[rows * columns];
        }

        public int Rows { get; }
        public int Columns { get; }
        public int CellCount => cells.Length;

        public GemType this[BoardPosition position]
        {
            get => cells[ToIndex(position)];
            set => cells[ToIndex(position)] = value;
        }

        public bool Contains(BoardPosition position) =>
            position.Row >= 0 && position.Row < Rows && position.Column >= 0 && position.Column < Columns;

        public int ToIndex(BoardPosition position)
        {
            if (!Contains(position)) throw new ArgumentOutOfRangeException(nameof(position), $"보드 밖 좌표입니다: {position}");
            return position.Row * Columns + position.Column;
        }

        public BoardPosition FromIndex(int index)
        {
            if (index < 0 || index >= cells.Length) throw new ArgumentOutOfRangeException(nameof(index), index, "보드 밖 칸 번호입니다.");
            return new BoardPosition(index / Columns, index % Columns);
        }

        public int Count(GemType type)
        {
            var count = 0;
            foreach (var cell in cells)
            {
                if (cell == type) count++;
            }

            return count;
        }
    }
}
