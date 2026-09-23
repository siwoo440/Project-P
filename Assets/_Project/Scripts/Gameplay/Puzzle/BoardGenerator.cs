using System;
using System.Collections.Generic;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보석 생성 가중치에 따라 보드를 무작위로 채운다. 기획서 5.1
    ///
    /// - 시드를 받는 System.Random을 쓴다. 같은 시드는 항상 같은 보드를 만든다.
    /// - 가중치 0인 보석은 나오지 않는다(특수 보석은 콤보로만 생성).
    /// - 모든 일반 보석이 서로 연결 가능하므로 자동 셔플 규칙은 두지 않는다.
    /// </summary>
    public sealed class BoardGenerator
    {
        private readonly GemType[] types;
        private readonly int[] cumulativeWeights;
        private readonly int totalWeight;

        public BoardGenerator(IEnumerable<(GemType type, int weight)> weights)
        {
            if (weights == null) throw new ArgumentNullException(nameof(weights));

            var typeList = new List<GemType>();
            var cumulativeList = new List<int>();
            var total = 0;

            foreach (var (type, weight) in weights)
            {
                if (weight < 0) throw new ArgumentException($"{type}의 생성 가중치가 음수입니다: {weight}", nameof(weights));
                if (weight == 0) continue;

                total += weight;
                typeList.Add(type);
                cumulativeList.Add(total);
            }

            if (total == 0) throw new ArgumentException("생성할 수 있는 보석이 없습니다. 가중치 합이 0입니다.", nameof(weights));

            types = typeList.ToArray();
            cumulativeWeights = cumulativeList.ToArray();
            totalWeight = total;
        }

        public Board Generate(int rows, int columns, int seed) => Generate(rows, columns, new Random(seed));

        public Board Generate(int rows, int columns, Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));

            var board = new Board(rows, columns);
            for (var i = 0; i < board.CellCount; i++)
            {
                board[board.FromIndex(i)] = Pick(random);
            }

            return board;
        }

        public GemType Pick(Random random)
        {
            var roll = random.Next(totalWeight);
            for (var i = 0; i < cumulativeWeights.Length; i++)
            {
                if (roll < cumulativeWeights[i]) return types[i];
            }

            return types[types.Length - 1];
        }
    }
}
