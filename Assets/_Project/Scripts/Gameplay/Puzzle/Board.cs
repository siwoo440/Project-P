using System;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보드 규칙 모델. Unity에 의존하지 않는 순수 C#이라 Play 없이 자동 테스트할 수 있다.
    ///
    /// - 좌표는 BoardPosition 기준 (행 0 = 맨 아래, 열 0 = 맨 왼쪽).
    /// - 칸 번호(index)는 row * Columns + column. 맨 아래 행 왼쪽부터 오른쪽으로 센다.
    /// - 칸마다 보석 종류와 특수 보석 여부(9일차)를 가진다. 인덱서로 종류를 바꿔도 특수 여부는 그대로다.
    /// - 화면(BoardView)은 이 모델을 읽기만 한다.
    /// </summary>
    public sealed class Board
    {
        private readonly GemType[] cells;
        private readonly bool[] special;

        public Board(int rows, int columns)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), rows, "행 수는 1 이상이어야 합니다.");
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns), columns, "열 수는 1 이상이어야 합니다.");

            Rows = rows;
            Columns = columns;
            cells = new GemType[rows * columns];
            special = new bool[rows * columns];
        }

        public int Rows { get; }
        public int Columns { get; }
        public int CellCount => cells.Length;

        public GemType this[BoardPosition position]
        {
            get => cells[ToIndex(position)];
            set => cells[ToIndex(position)] = value;
        }

        /// <summary>특수 보석 여부(기획서 5.6). 종류는 인덱서 값을 따른다 — 물리 특수 보석 = Physical + 특수.</summary>
        public bool IsSpecial(BoardPosition position) => special[ToIndex(position)];

        public void SetSpecial(BoardPosition position, bool isSpecial) => special[ToIndex(position)] = isSpecial;

        public PathGem GetPathGem(BoardPosition position) => new PathGem(this[position], IsSpecial(position));

        /// <summary>보드 위 특수 보석 수.</summary>
        public int SpecialCount
        {
            get
            {
                var count = 0;
                foreach (var flag in special)
                {
                    if (flag) count++;
                }

                return count;
            }
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
