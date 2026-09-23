using System;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보드 칸 좌표. 행 0 = 맨 아래, 열 0 = 맨 왼쪽.
    /// 화면 좌표(위가 +)와 방향이 같아 낙하(행 감소)와 보충(맨 위 행부터) 계산이 단순해진다.
    /// </summary>
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public readonly int Row;
        public readonly int Column;

        public BoardPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public bool Equals(BoardPosition other) => Row == other.Row && Column == other.Column;

        public override bool Equals(object obj) => obj is BoardPosition other && Equals(other);

        public override int GetHashCode() => (Row * 397) ^ Column;

        public override string ToString() => $"({Row}, {Column})";

        public static bool operator ==(BoardPosition left, BoardPosition right) => left.Equals(right);

        public static bool operator !=(BoardPosition left, BoardPosition right) => !left.Equals(right);
    }
}
