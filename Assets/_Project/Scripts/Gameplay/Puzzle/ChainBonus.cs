using System;
using System.Collections.Generic;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>경로 안에서 같은 종류 일반 보석이 끊기지 않고 이어진 구간.</summary>
    public readonly struct ChainRun
    {
        public ChainRun(GemType type, int start, int length)
        {
            Type = type;
            Start = start;
            Length = length;
        }

        public GemType Type { get; }

        /// <summary>경로에서 구간이 시작하는 순번(0부터).</summary>
        public int Start { get; }

        public int Length { get; }

        /// <summary>구간 마지막 보석의 순번.</summary>
        public int End => Start + Length - 1;

        public override string ToString() => $"{Type} ×{Length} ({Start}~{End})";
    }

    /// <summary>
    /// 동일 종류 연속 규칙. 기획서 5.5 / 5.6 — 순수 C#.
    ///
    /// - 같은 종류 일반 보석이 경로에서 바로 이어지면 한 구간이다. 다른 종류나 특수 보석이 끼면 끊긴다.
    /// - 구간 안 k번째(0부터) 보석의 배율 = 1 + BonusPerGem × k → 3연속이면 100% / 120% / 140%. 상한은 두지 않는다(행동력이 길이를 막는다).
    /// - 특수 보석은 연속을 끊고 연속 배율도 받지 않는다(1.0).
    /// - ComboLength(4) 이상 구간은 연속 강화 효과, SpecialLength(5) 이상 구간은 특수 보석 생성 조건.
    /// </summary>
    public static class ChainBonus
    {
        public static List<ChainRun> FindRuns(IReadOnlyList<PathGem> gems)
        {
            if (gems == null) throw new ArgumentNullException(nameof(gems));

            var runs = new List<ChainRun>();
            var i = 0;
            while (i < gems.Count)
            {
                if (gems[i].IsSpecial)
                {
                    i++;
                    continue;
                }

                var start = i;
                while (i + 1 < gems.Count && !gems[i + 1].IsSpecial && gems[i + 1].Type == gems[start].Type) i++;
                runs.Add(new ChainRun(gems[start].Type, start, i - start + 1));
                i++;
            }

            return runs;
        }

        /// <summary>보석 순번별 연속 배율.</summary>
        public static float[] Multipliers(IReadOnlyList<PathGem> gems, float bonusPerGem)
        {
            if (bonusPerGem < 0f) throw new ArgumentOutOfRangeException(nameof(bonusPerGem), bonusPerGem, "연속 보너스는 0 이상이어야 합니다.");

            var multipliers = new float[gems?.Count ?? 0];
            for (var i = 0; i < multipliers.Length; i++) multipliers[i] = 1f;

            foreach (var run in FindRuns(gems))
            {
                for (var k = 0; k < run.Length; k++) multipliers[run.Start + k] = 1f + bonusPerGem * k;
            }

            return multipliers;
        }

        /// <summary>
        /// 이 경로로 만들어질 특수 보석 종류. 한 번 연결에 1개까지 — 조건을 넘긴 구간이 여럿이면 마지막 구간의 종류.
        /// 조건을 넘긴 구간이 없으면 null.
        /// </summary>
        public static GemType? SpecialToCreate(IReadOnlyList<PathGem> gems, int specialLength)
        {
            GemType? created = null;
            foreach (var run in FindRuns(gems))
            {
                if (run.Length >= specialLength) created = run.Type;
            }

            return created;
        }
    }
}
