using System;
using System.Collections.Generic;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보석 효과 계산 수치. 순수 C# — GemData·PuzzleConfig 에셋에서 만들어 계산기에 넘긴다.
    /// 기획서 5.2 공란 → 제안값으로 시작(계수 1.0, 혼돈 1개당 +1·최대 +2, 균형 1개당 +1·최대 +3).
    /// 연속·연속 강화·특수 보석 수치는 Chain(9일차)에 있다.
    /// </summary>
    public sealed class GemEffectSettings
    {
        private readonly Dictionary<GemType, float> coefficients;

        public GemEffectSettings(IDictionary<GemType, float> coefficients, int delayPerGem, int maxDelay, int actionPointsPerGem, int maxNextTurnActionPoints,
            ChainSettings chain = null)
        {
            if (coefficients == null) throw new ArgumentNullException(nameof(coefficients));
            if (delayPerGem < 0 || maxDelay < 0 || actionPointsPerGem < 0 || maxNextTurnActionPoints < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayPerGem), "혼돈·균형 수치는 0 이상이어야 합니다.");
            }

            this.coefficients = new Dictionary<GemType, float>(coefficients);
            DelayPerGem = delayPerGem;
            MaxDelay = maxDelay;
            ActionPointsPerGem = actionPointsPerGem;
            MaxNextTurnActionPoints = maxNextTurnActionPoints;
            Chain = chain ?? ChainSettings.Default;
        }

        /// <summary>혼돈 보석 1개당 행동 지연.</summary>
        public int DelayPerGem { get; }

        /// <summary>한 번 연결로 혼돈 일반 보석이 줄 수 있는 행동 지연 최대치. 연속 강화·특수 보석 지연은 따로 센다.</summary>
        public int MaxDelay { get; }

        /// <summary>균형 보석 1개당 다음 턴 행동력.</summary>
        public int ActionPointsPerGem { get; }

        /// <summary>다음 턴 행동력 보너스 최대치.</summary>
        public int MaxNextTurnActionPoints { get; }

        /// <summary>동일 종류 연속·4연속 강화·특수 보석 수치.</summary>
        public ChainSettings Chain { get; }

        /// <summary>피해·회복 계수. 정해지지 않은 종류는 1.0.</summary>
        public float Coefficient(GemType gem) => coefficients.TryGetValue(gem, out var value) ? value : 1f;
    }
}
