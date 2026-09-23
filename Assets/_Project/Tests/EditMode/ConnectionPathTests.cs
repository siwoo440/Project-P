using System.Collections.Generic;
using NUnit.Framework;
using ProjectP.Data;
using ProjectP.Gameplay.Puzzle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>보석 연결 규칙 자동 테스트. 기획서 5.3</summary>
    public class ConnectionPathTests
    {
        private static Board NewBoard() => new Board(6, 12);

        private static BoardPosition P(int row, int column) => new BoardPosition(row, column);

        private static ConnectionPath StartedPath(params BoardPosition[] cells)
        {
            var path = new ConnectionPath(NewBoard());
            path.Begin(cells[0]);
            for (var i = 1; i < cells.Length; i++) Assert.AreEqual(ConnectionResult.Added, path.TryAdd(cells[i]), $"준비 단계 {i}");
            return path;
        }

        // ---------------------------------------------------------------- 인접

        [Test]
        public void IsAdjacent_AllEightNeighbors()
        {
            var center = P(2, 5);
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                Assert.IsTrue(ConnectionPath.IsAdjacent(center, P(2 + dr, 5 + dc)), $"({dr}, {dc}) 방향");
            }
        }

        [Test]
        public void IsAdjacent_RejectsSameCellAndFarCells()
        {
            Assert.IsFalse(ConnectionPath.IsAdjacent(P(2, 5), P(2, 5)), "같은 칸");
            Assert.IsFalse(ConnectionPath.IsAdjacent(P(2, 5), P(2, 7)), "가로 2칸");
            Assert.IsFalse(ConnectionPath.IsAdjacent(P(2, 5), P(4, 5)), "세로 2칸");
            Assert.IsFalse(ConnectionPath.IsAdjacent(P(2, 5), P(4, 6)), "나이트 이동");
        }

        // ---------------------------------------------------------------- 추가 규칙

        [Test]
        public void TryAdd_BeforeBegin_IsNotStarted()
        {
            var path = new ConnectionPath(NewBoard());
            Assert.AreEqual(ConnectionResult.NotStarted, path.TryAdd(P(0, 0)));
        }

        [Test]
        public void TryAdd_ConnectsDiagonally()
        {
            var path = StartedPath(P(0, 0), P(1, 1), P(2, 2), P(1, 3));
            Assert.AreEqual(4, path.Count);
        }

        [Test]
        public void TryAdd_RejectsNonAdjacent()
        {
            var path = StartedPath(P(0, 0));
            Assert.AreEqual(ConnectionResult.NotAdjacent, path.TryAdd(P(0, 2)));
            Assert.AreEqual(1, path.Count);
        }

        [Test]
        public void TryAdd_RejectsAlreadySelected()
        {
            var path = StartedPath(P(0, 0), P(0, 1), P(1, 1));
            Assert.AreEqual(ConnectionResult.AlreadySelected, path.TryAdd(P(0, 0)), "직전이 아닌 이미 고른 칸");
            Assert.AreEqual(3, path.Count);
        }

        [Test]
        public void TryAdd_ToPreviousCell_BacktracksLastSelection()
        {
            var path = StartedPath(P(0, 0), P(0, 1), P(0, 2));
            Assert.AreEqual(ConnectionResult.Backtracked, path.TryAdd(P(0, 1)));
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(P(0, 1), path.Positions[path.Count - 1]);

            Assert.AreEqual(ConnectionResult.Added, path.TryAdd(P(1, 2)), "되돌린 뒤 다른 방향으로 이어갈 수 있다");
        }

        [Test]
        public void TryAdd_SameAsLast_IsUnchanged()
        {
            var path = StartedPath(P(0, 0), P(0, 1));
            Assert.AreEqual(ConnectionResult.Unchanged, path.TryAdd(P(0, 1)));
            Assert.AreEqual(2, path.Count);
        }

        [Test]
        public void TryAdd_OutOfBoard_IsRejected()
        {
            var path = StartedPath(P(5, 11));
            Assert.AreEqual(ConnectionResult.OutOfBoard, path.TryAdd(P(6, 11)));
            Assert.AreEqual(ConnectionResult.OutOfBoard, path.TryAdd(P(5, 12)));
            Assert.AreEqual(ConnectionResult.OutOfBoard, new ConnectionPath(NewBoard()).Begin(P(-1, 0)));
        }

        [Test]
        public void TryAdd_IgnoresGemTypes()
        {
            var board = NewBoard();
            board[P(0, 0)] = GemType.Physical;
            board[P(0, 1)] = GemType.Magic;
            board[P(0, 2)] = GemType.Heal;

            var path = new ConnectionPath(board);
            path.Begin(P(0, 0));
            Assert.AreEqual(ConnectionResult.Added, path.TryAdd(P(0, 1)), "서로 다른 종류도 이어진다");
            Assert.AreEqual(ConnectionResult.Added, path.TryAdd(P(0, 2)));
        }

        [Test]
        public void TryAdd_StopsAtMaxLength()
        {
            var path = new ConnectionPath(NewBoard(), maxLength: 3);
            path.Begin(P(0, 0));
            path.TryAdd(P(0, 1));
            path.TryAdd(P(0, 2));

            Assert.AreEqual(ConnectionResult.LimitReached, path.TryAdd(P(0, 3)));
            Assert.AreEqual(3, path.Count);
            Assert.AreEqual(ConnectionResult.Backtracked, path.TryAdd(P(0, 1)), "한도에서도 되돌리기는 된다");
        }

        // ---------------------------------------------------------------- 확정·취소

        [Test]
        public void TryConfirm_NeedsMinimumTwo()
        {
            var path = StartedPath(P(0, 0));
            Assert.IsFalse(path.CanConfirm);
            Assert.IsFalse(path.TryConfirm(out _));
            Assert.AreEqual(1, path.Count, "확정 실패 시 경로는 그대로");
        }

        [Test]
        public void TryConfirm_ReturnsPathInOrderAndClears()
        {
            var path = StartedPath(P(0, 0), P(1, 1), P(1, 2));

            Assert.IsTrue(path.TryConfirm(out List<BoardPosition> confirmed));
            CollectionAssert.AreEqual(new[] { P(0, 0), P(1, 1), P(1, 2) }, confirmed);
            Assert.IsFalse(path.IsActive);
        }

        [Test]
        public void Cancel_ClearsPath()
        {
            var path = StartedPath(P(0, 0), P(0, 1));
            path.Cancel();
            Assert.IsFalse(path.IsActive);
            Assert.AreEqual(ConnectionResult.NotStarted, path.TryAdd(P(0, 2)));
        }

        [Test]
        public void Begin_DiscardsPreviousPath()
        {
            var path = StartedPath(P(0, 0), P(0, 1));
            path.Begin(P(3, 3));
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(P(3, 3), path.Positions[0]);
        }
    }
}
