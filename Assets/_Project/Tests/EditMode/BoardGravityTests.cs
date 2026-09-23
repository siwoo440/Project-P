using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectP.Data;
using ProjectP.Gameplay.Puzzle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>보석 제거·낙하·보충 자동 테스트. 기획서 5.1</summary>
    public class BoardGravityTests
    {
        private const int Rows = 6;
        private const int Columns = 12;

        private static BoardPosition P(int row, int column) => new BoardPosition(row, column);

        /// <summary>열마다 아래부터 Physical, Magic, Heal, Chaos, Balance, Physical 순서로 채운 보드.</summary>
        private static Board PatternBoard()
        {
            var types = new[] { GemType.Physical, GemType.Magic, GemType.Heal, GemType.Chaos, GemType.Balance, GemType.Physical };
            var board = new Board(Rows, Columns);
            for (var row = 0; row < Rows; row++)
            for (var column = 0; column < Columns; column++)
                board[P(row, column)] = types[row];
            return board;
        }

        private static Func<GemType> SpawnAlways(GemType type) => () => type;

        private static GemType[] ColumnOf(Board board, int column) =>
            Enumerable.Range(0, board.Rows).Select(row => board[P(row, column)]).ToArray();

        [Test]
        public void RemovingBottomCell_ShiftsColumnDownAndSpawnsOnTop()
        {
            var board = PatternBoard();
            BoardGravity.Collapse(board, new[] { P(0, 3) }, SpawnAlways(GemType.Special));

            CollectionAssert.AreEqual(
                new[] { GemType.Magic, GemType.Heal, GemType.Chaos, GemType.Balance, GemType.Physical, GemType.Special },
                ColumnOf(board, 3));
        }

        [Test]
        public void NonContiguousRemoval_KeepsRemainingOrder()
        {
            var board = PatternBoard();
            BoardGravity.Collapse(board, new[] { P(0, 0), P(2, 0), P(4, 0) }, SpawnAlways(GemType.Special));

            CollectionAssert.AreEqual(
                new[] { GemType.Magic, GemType.Chaos, GemType.Physical, GemType.Special, GemType.Special, GemType.Special },
                ColumnOf(board, 0), "남은 보석(Magic·Chaos·Physical)은 순서를 유지한 채 내려온다");
        }

        [Test]
        public void OtherColumns_AreUntouched()
        {
            var board = PatternBoard();
            var before = ColumnOf(board, 5);

            var drops = BoardGravity.Collapse(board, new[] { P(1, 4), P(2, 6) }, SpawnAlways(GemType.Special));

            CollectionAssert.AreEqual(before, ColumnOf(board, 5));
            Assert.IsTrue(drops.All(drop => drop.To.Column == 4 || drop.To.Column == 6), "제거가 없는 열은 움직이지 않는다");
        }

        [Test]
        public void DiagonalPath_RemovesOneCellPerColumn()
        {
            var board = PatternBoard();
            var path = new[] { P(0, 0), P(1, 1), P(2, 2), P(3, 3) };
            var drops = BoardGravity.Collapse(board, path, SpawnAlways(GemType.Special));

            Assert.AreEqual(4, drops.Count(drop => drop.Spawned), "열마다 1개씩 새로 생긴다");
            for (var column = 0; column < 4; column++) Assert.AreEqual(GemType.Special, board[P(Rows - 1, column)], $"{column}열 맨 위");
        }

        [Test]
        public void Drops_DescribeFallFromAbove()
        {
            var board = PatternBoard();
            var drops = BoardGravity.Collapse(board, new[] { P(0, 7), P(1, 7) }, SpawnAlways(GemType.Special));

            var moved = drops.Where(drop => !drop.Spawned).OrderBy(drop => drop.To.Row).ToList();
            Assert.AreEqual(4, moved.Count, "위의 4개가 내려온다");
            Assert.IsTrue(moved.All(drop => drop.Distance == 2), "2칸씩 내려온다");

            var spawned = drops.Where(drop => drop.Spawned).OrderBy(drop => drop.To.Row).ToList();
            CollectionAssert.AreEqual(new[] { Rows, Rows + 1 }, spawned.Select(drop => drop.FromRow).ToArray(), "보드 바로 위부터 차례로 출발");
            CollectionAssert.AreEqual(new[] { Rows - 2, Rows - 1 }, spawned.Select(drop => drop.To.Row).ToArray());
        }

        [Test]
        public void SameSpawnSequence_ProducesSameBoard()
        {
            Board Run()
            {
                var board = new BoardGenerator(new[] { (GemType.Physical, 1), (GemType.Magic, 1), (GemType.Heal, 1) }).Generate(Rows, Columns, 5);
                var random = new Random(99);
                var generator = new BoardGenerator(new[] { (GemType.Chaos, 1), (GemType.Balance, 1) });
                BoardGravity.Collapse(board, new[] { P(0, 0), P(1, 1), P(1, 2), P(3, 9) }, () => generator.Pick(random));
                return board;
            }

            var first = Run();
            var second = Run();
            for (var i = 0; i < first.CellCount; i++) Assert.AreEqual(first[first.FromIndex(i)], second[second.FromIndex(i)], $"칸 {i}");
        }

        [Test]
        public void BoardStaysFull_AndRemovedCountIsRefilled()
        {
            var board = PatternBoard();
            var removed = new List<BoardPosition> { P(0, 0), P(0, 1), P(1, 1), P(5, 11) };
            var spawnCount = 0;
            BoardGravity.Collapse(board, removed, () => { spawnCount++; return GemType.Special; });

            Assert.AreEqual(removed.Count, spawnCount, "없앤 만큼 새로 생긴다");
            Assert.AreEqual(removed.Count, board.Count(GemType.Special));
        }

        [Test]
        public void SpecialGem_FallsWithItsFlag_AndRefillIsNormal()
        {
            // 9일차: 특수 보석 표시는 보석과 함께 떨어지고, 새로 채운 보석은 일반 보석이다.
            var board = PatternBoard();
            board.SetSpecial(P(3, 2), true);
            board.SetSpecial(P(5, 2), true); // 맨 위 칸 — 사용해서 없앤다

            BoardGravity.Collapse(board, new[] { P(0, 2), P(1, 2), P(5, 2) }, SpawnAlways(GemType.Heal));

            Assert.IsTrue(board.IsSpecial(P(1, 2)), "행 3의 특수 보석이 두 칸 내려옴");
            Assert.AreEqual(GemType.Chaos, board[P(1, 2)]);
            Assert.IsFalse(board.IsSpecial(P(3, 2)), "떠난 자리는 일반 보석");
            for (var row = 3; row < Rows; row++) Assert.IsFalse(board.IsSpecial(P(row, 2)), $"새로 채운 {row}행");
            Assert.AreEqual(1, board.SpecialCount);
        }

        [Test]
        public void Board_StartsWithoutSpecialGems_AndKeepsFlagWhenTypeChanges()
        {
            var board = PatternBoard();
            Assert.AreEqual(0, board.SpecialCount);

            board.SetSpecial(P(0, 0), true);
            board[P(0, 0)] = GemType.Magic;
            Assert.AreEqual(new PathGem(GemType.Magic, true), board.GetPathGem(P(0, 0)));
        }

        [Test]
        public void InvalidRemoval_IsRejected()
        {
            var board = PatternBoard();
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardGravity.Collapse(board, new[] { P(Rows, 0) }, SpawnAlways(GemType.Special)));
            Assert.Throws<ArgumentException>(() => BoardGravity.Collapse(board, new[] { P(0, 0), P(0, 0) }, SpawnAlways(GemType.Special)));
        }
    }
}
