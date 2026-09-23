using System;
using System.Collections.Generic;

namespace ProjectP.Gameplay.Battle
{
    /// <summary>
    /// 공격 대상 선택 규칙. 기획서 6.3 — Unity에 의존하지 않는 순수 C#.
    ///
    /// - 처음 대상은 화면 왼쪽(순번 0)부터 살아 있는 첫 적이다.
    /// - 플레이어가 살아 있는 적을 골라 대상을 바꿀 수 있다. 쓰러진 적은 고를 수 없다.
    /// - 현재 대상이 쓰러지면 살아 있는 적 중 하나로 **무작위** 변경한다(사용자 결정). 남은 적이 없으면 대상 없음(-1).
    /// - 무작위는 시드를 받는 System.Random을 쓴다(같은 시드 = 같은 결과).
    /// 다중 공격 스킬의 대상 규칙(기획서 6.3)은 스킬 단계에서 따로 둔다.
    /// </summary>
    public sealed class TargetSelection
    {
        private readonly bool[] alive;
        private readonly Random random;

        public TargetSelection(int count, Random random)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "적 수는 0 이상이어야 합니다.");

            this.random = random ?? throw new ArgumentNullException(nameof(random));
            alive = new bool[count];
            for (var i = 0; i < count; i++) alive[i] = true;
            Current = count > 0 ? 0 : -1;
        }

        public int Count => alive.Length;

        /// <summary>현재 공격 대상 순번. 대상이 없으면 -1.</summary>
        public int Current { get; private set; }

        public bool HasTarget => Current >= 0;

        public bool IsAlive(int index) => index >= 0 && index < alive.Length && alive[index];

        /// <summary>플레이어가 대상을 고른다. 살아 있는 적만 고를 수 있다.</summary>
        public bool Select(int index)
        {
            if (!IsAlive(index)) return false;

            Current = index;
            return true;
        }

        /// <summary>적이 쓰러졌다. 현재 대상이었다면 살아 있는 적 중 무작위로 바꾼다. 바뀐(또는 그대로인) 대상을 돌려준다.</summary>
        public int MarkDefeated(int index)
        {
            if (index < 0 || index >= alive.Length) throw new ArgumentOutOfRangeException(nameof(index), index, "없는 적입니다.");

            alive[index] = false;
            if (index == Current) Current = PickRandomAlive();
            return Current;
        }

        /// <summary>적이 되살아났다. 대상이 없던 상태면 이 적을 대상으로 삼는다.</summary>
        public void Revive(int index)
        {
            if (index < 0 || index >= alive.Length) throw new ArgumentOutOfRangeException(nameof(index), index, "없는 적입니다.");

            alive[index] = true;
            if (Current < 0) Current = index;
        }

        private int PickRandomAlive()
        {
            var candidates = new List<int>();
            for (var i = 0; i < alive.Length; i++)
            {
                if (alive[i]) candidates.Add(i);
            }

            return candidates.Count == 0 ? -1 : candidates[random.Next(candidates.Count)];
        }
    }
}
