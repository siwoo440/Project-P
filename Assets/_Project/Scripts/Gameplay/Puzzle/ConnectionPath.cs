using System;
using System.Collections.Generic;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보석 연결 경로 규칙. 기획서 5.3 — Unity에 의존하지 않는 순수 C#.
    ///
    /// - 8방향(상하좌우·대각선) 인접한 칸만 이어진다.
    /// - 이미 고른 칸은 다시 고를 수 없다. 단, 직전 칸으로 되돌아가면 마지막 선택을 취소한다(되돌리기).
    /// - 보석 종류는 따지지 않는다(혼합 연결).
    /// - MinLength개 이상이어야 확정할 수 있다.
    /// - MaxLength는 7일차 행동력이 들어갈 자리다. 지금은 제한이 없다.
    /// </summary>
    public sealed class ConnectionPath
    {
        private readonly Board board;
        private readonly List<BoardPosition> positions = new List<BoardPosition>();

        public ConnectionPath(Board board, int minLength = 2, int maxLength = int.MaxValue)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            if (minLength < 1) throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "최소 연결 수는 1 이상이어야 합니다.");

            MinLength = minLength;
            MaxLength = maxLength;
        }

        public int MinLength { get; }

        /// <summary>한 경로에 넣을 수 있는 최대 칸 수. 7일차부터 턴마다 행동력으로 바뀐다.</summary>
        public int MaxLength { get; set; }

        public IReadOnlyList<BoardPosition> Positions => positions;
        public int Count => positions.Count;
        public bool IsActive => positions.Count > 0;
        public bool CanConfirm => positions.Count >= MinLength;

        public static bool IsAdjacent(BoardPosition a, BoardPosition b)
        {
            var rowDistance = Math.Abs(a.Row - b.Row);
            var columnDistance = Math.Abs(a.Column - b.Column);
            return rowDistance <= 1 && columnDistance <= 1 && (rowDistance != 0 || columnDistance != 0);
        }

        /// <summary>새 경로를 시작한다. 이전 경로는 버린다.</summary>
        public ConnectionResult Begin(BoardPosition position)
        {
            positions.Clear();
            if (!board.Contains(position)) return ConnectionResult.OutOfBoard;
            if (MaxLength < 1) return ConnectionResult.LimitReached;

            positions.Add(position);
            return ConnectionResult.Added;
        }

        public ConnectionResult TryAdd(BoardPosition position)
        {
            if (!IsActive) return ConnectionResult.NotStarted;
            if (!board.Contains(position)) return ConnectionResult.OutOfBoard;

            var last = positions[positions.Count - 1];
            if (position == last) return ConnectionResult.Unchanged;

            if (positions.Count >= 2 && position == positions[positions.Count - 2])
            {
                positions.RemoveAt(positions.Count - 1);
                return ConnectionResult.Backtracked;
            }

            if (positions.Contains(position)) return ConnectionResult.AlreadySelected;
            if (!IsAdjacent(last, position)) return ConnectionResult.NotAdjacent;
            if (positions.Count >= MaxLength) return ConnectionResult.LimitReached;

            positions.Add(position);
            return ConnectionResult.Added;
        }

        /// <summary>확정할 수 있으면 경로 사본을 돌려주고 비운다. 확정할 수 없으면 경로를 그대로 둔다.</summary>
        public bool TryConfirm(out List<BoardPosition> confirmed)
        {
            if (!CanConfirm)
            {
                confirmed = null;
                return false;
            }

            confirmed = new List<BoardPosition>(positions);
            positions.Clear();
            return true;
        }

        public void Cancel() => positions.Clear();
    }
}
