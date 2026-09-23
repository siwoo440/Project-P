using System.Collections.Generic;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>보석 효과 종류. 기획서 5.2</summary>
    public enum GemEffectKind
    {
        /// <summary>효과 없음 (특수 보석 — 9일차).</summary>
        None,
        PhysicalDamage,
        MagicDamage,
        Heal,

        /// <summary>대상 적의 행동 카운트 증가(행동 지연).</summary>
        Delay,

        /// <summary>다음 턴 행동력 증가.</summary>
        NextTurnActionPoints
    }

    /// <summary>경로 안 보석 하나의 효과.</summary>
    public readonly struct GemEffectEvent
    {
        public GemEffectEvent(int index, GemType gem, GemEffectKind kind, int amount, float multiplier)
        {
            Index = index;
            Gem = gem;
            Kind = kind;
            Amount = amount;
            Multiplier = multiplier;
        }

        /// <summary>경로에서 몇 번째 보석인지 (0부터).</summary>
        public int Index { get; }

        public GemType Gem { get; }
        public GemEffectKind Kind { get; }

        /// <summary>효과량. 혼돈·균형이 상한에 닿으면 0일 수 있다.</summary>
        public int Amount { get; }

        /// <summary>적용된 배율. 9일차 연속 보너스·10일차 피버가 여기에 곱해진다.</summary>
        public float Multiplier { get; }
    }

    /// <summary>
    /// 한 번의 연결이 낸 효과 전체. 순서 목록(기획서 5.5 — 연결 순서대로 처리)과 종류별 합계.
    /// 계산 결과일 뿐 아무 대상에도 적용되지 않았다. 적용은 받는 쪽(전투·퍼즐 테스트)이 한다.
    /// </summary>
    public sealed class EffectSummary
    {
        public EffectSummary(IReadOnlyList<GemEffectEvent> events)
        {
            Events = events;
            foreach (var effect in events)
            {
                switch (effect.Kind)
                {
                    case GemEffectKind.PhysicalDamage: PhysicalDamage += effect.Amount; break;
                    case GemEffectKind.MagicDamage: MagicDamage += effect.Amount; break;
                    case GemEffectKind.Heal: Heal += effect.Amount; break;
                    case GemEffectKind.Delay: Delay += effect.Amount; break;
                    case GemEffectKind.NextTurnActionPoints: NextTurnActionPoints += effect.Amount; break;
                }
            }
        }

        public IReadOnlyList<GemEffectEvent> Events { get; }
        public int PhysicalDamage { get; }
        public int MagicDamage { get; }
        public int TotalDamage => PhysicalDamage + MagicDamage;
        public int Heal { get; }
        public int Delay { get; }
        public int NextTurnActionPoints { get; }
    }
}
