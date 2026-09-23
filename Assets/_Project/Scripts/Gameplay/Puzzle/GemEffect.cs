using System.Collections.Generic;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>보석 효과 종류. 기획서 5.2</summary>
    public enum GemEffectKind
    {
        /// <summary>효과 없음 (정의되지 않은 보석).</summary>
        None,
        PhysicalDamage,
        MagicDamage,
        Heal,

        /// <summary>대상 적의 행동 카운트 증가(행동 지연).</summary>
        Delay,

        /// <summary>다음 턴 행동력 증가.</summary>
        NextTurnActionPoints
    }

    /// <summary>효과가 어디서 나왔는지. 연출·기록이 구분해 보여준다.</summary>
    public enum EffectSource
    {
        /// <summary>일반 보석 1개.</summary>
        Gem,

        /// <summary>4연속 연속 강화(강공격·추가 회복·지연 강화·행동력 +1). 기획서 5.6</summary>
        Combo,

        /// <summary>특수 보석 발동. 기획서 5.6</summary>
        Special
    }

    /// <summary>적에게 주는 효과(피해·지연)를 누구에게 주는지. 회복·행동력은 대상이 없다.</summary>
    public enum EffectTarget
    {
        /// <summary>현재 공격 대상 한 명.</summary>
        Single,

        /// <summary>살아 있는 모든 적 (마법·혼돈 특수 보석).</summary>
        AllEnemies
    }

    /// <summary>경로 안 보석 하나(또는 연속 강화 한 번)의 효과.</summary>
    public readonly struct GemEffectEvent
    {
        public GemEffectEvent(int index, GemType gem, GemEffectKind kind, int amount, float multiplier,
            EffectSource source = EffectSource.Gem, EffectTarget target = EffectTarget.Single)
        {
            Index = index;
            Gem = gem;
            Kind = kind;
            Amount = amount;
            Multiplier = multiplier;
            Source = source;
            Target = target;
        }

        /// <summary>경로에서 몇 번째 보석인지 (0부터). 연속 강화는 그 구간 마지막 보석 순번.</summary>
        public int Index { get; }

        public GemType Gem { get; }
        public GemEffectKind Kind { get; }

        /// <summary>효과량. 혼돈·균형이 상한에 닿으면 0일 수 있다.</summary>
        public int Amount { get; }

        /// <summary>적용된 배율 = 연속 배율 × 외부 배율(10일차 피버). 특수 보석·연속 강화는 외부 배율만.</summary>
        public float Multiplier { get; }

        public EffectSource Source { get; }
        public EffectTarget Target { get; }
    }

    /// <summary>
    /// 한 번의 연결이 낸 효과 전체. 순서 목록(기획서 5.5 — 연결 순서대로 처리)과 종류별 합계.
    /// 계산 결과일 뿐 아무 대상에도 적용되지 않았다. 적용은 받는 쪽(전투·퍼즐 테스트)이 한다.
    /// 모든 적 대상 피해는 적 한 명 기준으로 한 번만 더한다.
    /// </summary>
    public sealed class EffectSummary
    {
        public EffectSummary(IReadOnlyList<GemEffectEvent> events, IReadOnlyList<float> chainMultipliers = null, GemType? createdSpecial = null)
        {
            Events = events;
            ChainMultipliers = chainMultipliers ?? new float[0];
            CreatedSpecial = createdSpecial;

            foreach (var effect in events)
            {
                if (effect.Source == EffectSource.Combo) ComboCount++;

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

        /// <summary>경로 순번별 동일 종류 연속 배율(1.0 / 1.2 / 1.4 …). 특수 보석은 1.0.</summary>
        public IReadOnlyList<float> ChainMultipliers { get; }

        /// <summary>이 연결로 경로 마지막 칸에 만들어질 특수 보석 종류. 없으면 null.</summary>
        public GemType? CreatedSpecial { get; }

        /// <summary>연속 강화가 발동한 횟수.</summary>
        public int ComboCount { get; }

        public int PhysicalDamage { get; }
        public int MagicDamage { get; }
        public int TotalDamage => PhysicalDamage + MagicDamage;
        public int Heal { get; }
        public int Delay { get; }
        public int NextTurnActionPoints { get; }
    }
}
