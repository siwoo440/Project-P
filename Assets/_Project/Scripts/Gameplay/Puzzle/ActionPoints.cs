using System;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 행동력 규칙. 기획서 5.4 — Unity에 의존하지 않는 순수 C#.
    ///
    /// - 이번 턴 행동력 = 기본 행동력 + 다음 턴 보너스. 보석 1개를 이을 때마다 1을 쓰므로 곧 경로 최대 길이다.
    /// - 쓰고 남은 행동력은 다음 턴으로 이월하지 않는다(사용자 결정). 다음 턴을 늘리는 것은 균형 보석·스킬의 역할이다.
    /// - 기본 행동력을 바꾸면 다음 턴부터 적용한다.
    /// - 피버(+3, 10일차)는 StartTurn 계산에 더할 자리다.
    /// </summary>
    public sealed class ActionPoints
    {
        public ActionPoints(int baseValue, int maxNextTurnBonus = int.MaxValue)
        {
            if (maxNextTurnBonus < 0) throw new ArgumentOutOfRangeException(nameof(maxNextTurnBonus), maxNextTurnBonus, "보너스 상한은 0 이상이어야 합니다.");

            SetBase(baseValue);
            MaxNextTurnBonus = maxNextTurnBonus;
        }

        /// <summary>다음 턴 보너스 최대치. 균형 보석·스킬이 아무리 쌓아도 이 값을 넘지 않는다. 기획서 5.2 공란 → +3</summary>
        public int MaxNextTurnBonus { get; }

        /// <summary>기본 행동력. 기획서 5.4 — 6.</summary>
        public int Base { get; private set; }

        /// <summary>이번 턴 행동력(경로 최대 길이). 첫 StartTurn 전에는 0.</summary>
        public int Current { get; private set; }

        /// <summary>다음 턴에 더해질 보너스. 균형 보석(8일차)·스킬이 쌓는다.</summary>
        public int NextTurnBonus { get; private set; }

        /// <summary>이번 턴에 반영된 보너스. 화면에서 보너스 몫을 구분해 보여줄 때 쓴다.</summary>
        public int AppliedBonus { get; private set; }

        public void SetBase(int value)
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), value, "기본 행동력은 1 이상이어야 합니다.");
            Base = value;
        }

        public void AddNextTurnBonus(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "보너스는 0 이상이어야 합니다.");
            NextTurnBonus = Math.Min(MaxNextTurnBonus, NextTurnBonus + amount);
        }

        /// <summary>새 턴 행동력을 계산한다. 반영한 보너스는 비운다. 남은 행동력은 버린다(이월 없음).</summary>
        public void StartTurn()
        {
            AppliedBonus = NextTurnBonus;
            Current = Base + NextTurnBonus;
            NextTurnBonus = 0;
        }
    }
}
