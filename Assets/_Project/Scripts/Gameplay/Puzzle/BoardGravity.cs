using System;
using System.Collections.Generic;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>보석 한 개의 이동. 화면이 낙하 연출에 쓴다.</summary>
    public readonly struct GemDrop
    {
        public GemDrop(BoardPosition to, int fromRow, bool spawned, GemType type)
        {
            To = to;
            FromRow = fromRow;
            Spawned = spawned;
            Type = type;
        }

        /// <summary>도착 칸.</summary>
        public BoardPosition To { get; }

        /// <summary>출발 행. 새로 생긴 보석은 보드 위(행 수 이상)에서 출발한다.</summary>
        public int FromRow { get; }

        public bool Spawned { get; }
        public GemType Type { get; }

        /// <summary>떨어지는 칸 수.</summary>
        public int Distance => FromRow - To.Row;
    }

    /// <summary>
    /// 보석 제거·낙하·보충 규칙. 기획서 5.1 — 순수 C#.
    ///
    /// 사용한 보석을 없애고, 남은 보석은 같은 열에서 아래로 내린 뒤, 빈 윗칸은 새 보석으로 채운다.
    /// 행 0이 맨 아래이므로 "아래로 내림" = 행 번호를 줄임, "위에서 보충" = 맨 위 행부터 채움.
    /// 새 보석 생성 순서: 열 0부터, 각 열은 아래 빈칸부터. 같은 생성 순서면 결과가 같다(시드 재현).
    /// 특수 보석 표시는 보석과 함께 움직이고, 새로 채운 보석은 일반 보석이다.
    /// 5연속으로 만드는 특수 보석은 호출하는 쪽이 경로 마지막 칸을 removed에서 빼고 특수 보석으로 바꾼 뒤 부른다.
    /// </summary>
    public static class BoardGravity
    {
        /// <summary>board를 직접 바꾸고, 움직인 보석 목록을 돌려준다. 제자리인 보석은 목록에 없다.</summary>
        public static List<GemDrop> Collapse(Board board, IEnumerable<BoardPosition> removed, Func<GemType> spawn)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (removed == null) throw new ArgumentNullException(nameof(removed));
            if (spawn == null) throw new ArgumentNullException(nameof(spawn));

            var removedSet = new HashSet<BoardPosition>();
            foreach (var position in removed)
            {
                if (!board.Contains(position)) throw new ArgumentOutOfRangeException(nameof(removed), $"보드 밖 좌표입니다: {position}");
                if (!removedSet.Add(position)) throw new ArgumentException($"같은 칸이 두 번 들어 있습니다: {position}", nameof(removed));
            }

            var drops = new List<GemDrop>();
            for (var column = 0; column < board.Columns; column++)
            {
                // 남은 보석을 아래부터 채운다.
                var writeRow = 0;
                for (var row = 0; row < board.Rows; row++)
                {
                    var from = new BoardPosition(row, column);
                    if (removedSet.Contains(from)) continue;

                    if (row != writeRow)
                    {
                        var type = board[from];
                        var to = new BoardPosition(writeRow, column);
                        board[to] = type;
                        board.SetSpecial(to, board.IsSpecial(from)); // 특수 보석은 떨어져도 특수 보석이다
                        drops.Add(new GemDrop(to, row, false, type));
                    }

                    writeRow++;
                }

                // 빈 윗칸을 새 보석으로 채운다. 보드 바로 위에서 차례로 떨어진다.
                for (var row = writeRow; row < board.Rows; row++)
                {
                    var type = spawn();
                    var to = new BoardPosition(row, column);
                    board[to] = type;
                    board.SetSpecial(to, false); // 새로 채우는 보석은 언제나 일반 보석
                    drops.Add(new GemDrop(to, board.Rows + (row - writeRow), true, type));
                }
            }

            return drops;
        }
    }
}
