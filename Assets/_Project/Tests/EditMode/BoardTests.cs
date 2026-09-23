using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectP.Data;
using ProjectP.Gameplay.Puzzle;
using UnityEngine;

namespace ProjectP.Tests.EditMode
{
    /// <summary>
    /// 보드 모델·생성 규칙 자동 테스트. Play 모드 없이 실행된다.
    /// 실행: Window > General > Test Runner > EditMode > Run All
    /// </summary>
    public class BoardTests
    {
        private const int Rows = 6;
        private const int Columns = 12;

        private static readonly (GemType, int)[] EqualWeights =
        {
            (GemType.Physical, 20), (GemType.Magic, 20), (GemType.Heal, 20), (GemType.Chaos, 20), (GemType.Balance, 20)
        };

        private static readonly GemType[] BasicTypes =
        {
            GemType.Physical, GemType.Magic, GemType.Heal, GemType.Chaos, GemType.Balance
        };

        // ---------------------------------------------------------------- 보드 크기

        [Test]
        public void PuzzleConfig_DefaultsToSixRowsTwelveColumns()
        {
            var config = ScriptableObject.CreateInstance<PuzzleConfig>();
            try
            {
                Assert.AreEqual(6, config.Rows);
                Assert.AreEqual(12, config.Columns);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Generate_CreatesBoardOfRequestedSize()
        {
            var board = new BoardGenerator(EqualWeights).Generate(Rows, Columns, seed: 1);

            Assert.AreEqual(Rows, board.Rows);
            Assert.AreEqual(Columns, board.Columns);
            Assert.AreEqual(72, board.CellCount);
        }

        [Test]
        public void Board_RejectsNonPositiveSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(0, Columns));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(Rows, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(-1, Columns));
        }

        // ---------------------------------------------------------------- 좌표

        [Test]
        public void Board_IndexStartsAtBottomLeftAndGoesRowByRow()
        {
            var board = new Board(Rows, Columns);

            Assert.AreEqual(new BoardPosition(0, 0), board.FromIndex(0), "칸 0은 맨 아래 왼쪽이어야 한다");
            Assert.AreEqual(new BoardPosition(0, Columns - 1), board.FromIndex(Columns - 1), "첫 행의 끝은 맨 아래 오른쪽");
            Assert.AreEqual(new BoardPosition(1, 0), board.FromIndex(Columns), "다음 칸은 한 행 위 왼쪽");
            Assert.AreEqual(new BoardPosition(Rows - 1, Columns - 1), board.FromIndex(board.CellCount - 1), "마지막 칸은 맨 위 오른쪽");
        }

        [Test]
        public void Board_IndexAndPositionRoundTrip()
        {
            var board = new Board(Rows, Columns);
            for (var i = 0; i < board.CellCount; i++)
            {
                Assert.AreEqual(i, board.ToIndex(board.FromIndex(i)));
            }
        }

        [Test]
        public void Board_ContainsOnlyPositionsInsideBoard()
        {
            var board = new Board(Rows, Columns);

            Assert.IsTrue(board.Contains(new BoardPosition(0, 0)));
            Assert.IsTrue(board.Contains(new BoardPosition(Rows - 1, Columns - 1)));
            Assert.IsFalse(board.Contains(new BoardPosition(-1, 0)));
            Assert.IsFalse(board.Contains(new BoardPosition(0, -1)));
            Assert.IsFalse(board.Contains(new BoardPosition(Rows, 0)));
            Assert.IsFalse(board.Contains(new BoardPosition(0, Columns)));
        }

        [Test]
        public void Board_OutOfRangeAccessThrows()
        {
            var board = new Board(Rows, Columns);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = board[new BoardPosition(Rows, 0)]);
            Assert.Throws<ArgumentOutOfRangeException>(() => board.FromIndex(board.CellCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.FromIndex(-1));
        }

        // ---------------------------------------------------------------- 생성

        [Test]
        public void Generate_FillsEveryCellWithBasicGem()
        {
            var board = new BoardGenerator(EqualWeights).Generate(Rows, Columns, seed: 7);

            var total = 0;
            foreach (var type in BasicTypes) total += board.Count(type);

            Assert.AreEqual(board.CellCount, total, "모든 칸이 기본 보석 5종 중 하나여야 한다");
            Assert.AreEqual(0, board.Count(GemType.Special), "특수 보석은 무작위로 나오지 않는다");
        }

        [Test]
        public void Generate_SameSeedProducesSameBoard()
        {
            var generator = new BoardGenerator(EqualWeights);
            var first = generator.Generate(Rows, Columns, seed: 42);
            var second = generator.Generate(Rows, Columns, seed: 42);

            for (var i = 0; i < first.CellCount; i++)
            {
                Assert.AreEqual(first[first.FromIndex(i)], second[second.FromIndex(i)], $"칸 {i}가 다르다");
            }
        }

        [Test]
        public void Generate_DifferentSeedProducesDifferentBoard()
        {
            var generator = new BoardGenerator(EqualWeights);
            var first = generator.Generate(Rows, Columns, seed: 1);
            var second = generator.Generate(Rows, Columns, seed: 2);

            var differences = 0;
            for (var i = 0; i < first.CellCount; i++)
            {
                if (first[first.FromIndex(i)] != second[second.FromIndex(i)]) differences++;
            }

            Assert.Greater(differences, 0);
        }

        [Test]
        public void Generate_ZeroWeightGemNeverAppears()
        {
            var weights = new List<(GemType, int)>
            {
                (GemType.Physical, 20), (GemType.Magic, 20), (GemType.Heal, 20),
                (GemType.Chaos, 0), (GemType.Balance, 20), (GemType.Special, 0)
            };
            var generator = new BoardGenerator(weights);

            for (var seed = 0; seed < 50; seed++)
            {
                var board = generator.Generate(Rows, Columns, seed);
                Assert.AreEqual(0, board.Count(GemType.Chaos), $"시드 {seed}");
                Assert.AreEqual(0, board.Count(GemType.Special), $"시드 {seed}");
            }
        }

        [Test]
        public void Pick_DistributionFollowsWeights()
        {
            var generator = new BoardGenerator(new[]
            {
                (GemType.Physical, 10), (GemType.Magic, 20), (GemType.Heal, 30), (GemType.Chaos, 40)
            });
            var random = new System.Random(12345);
            const int samples = 100_000;
            var counts = new Dictionary<GemType, int>();

            for (var i = 0; i < samples; i++)
            {
                var type = generator.Pick(random);
                counts[type] = counts.TryGetValue(type, out var current) ? current + 1 : 1;
            }

            AssertShare(counts, GemType.Physical, 0.10, samples);
            AssertShare(counts, GemType.Magic, 0.20, samples);
            AssertShare(counts, GemType.Heal, 0.30, samples);
            AssertShare(counts, GemType.Chaos, 0.40, samples);
        }

        [Test]
        public void Constructor_RejectsInvalidWeights()
        {
            Assert.Throws<ArgumentException>(() => new BoardGenerator(new[] { (GemType.Physical, -1) }), "음수 가중치");
            Assert.Throws<ArgumentException>(() => new BoardGenerator(new[] { (GemType.Physical, 0) }), "가중치 합 0");
            Assert.Throws<ArgumentNullException>(() => new BoardGenerator(null));
        }

        private static void AssertShare(Dictionary<GemType, int> counts, GemType type, double expected, int samples)
        {
            counts.TryGetValue(type, out var count);
            Assert.AreEqual(expected, (double)count / samples, 0.015, $"{type} 비율");
        }
    }
}
