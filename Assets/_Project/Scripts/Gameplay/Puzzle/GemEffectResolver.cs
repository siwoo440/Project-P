using System;
using System.Collections.Generic;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보석 효과 계산. 기획서 5.2 / 5.5 — 순수 C#.
    ///
    /// - 경로의 보석을 연결 순서대로 하나씩 계산해 결과표(EffectSummary)로 돌려준다. 대상에 적용하지 않는다.
    /// - 피해·회복 = 메인 스탯 × 보석 계수 × 배율. 보석마다 반올림(0.5는 올림)하고 최소 1.
    /// - 혼돈: 1개당 DelayPerGem, 한 번 연결에 최대 MaxDelay. 상한을 넘은 몫은 0.
    /// - 균형: 1개당 ActionPointsPerGem, 최대 MaxNextTurnActionPoints. 상한을 넘은 몫은 0.
    /// - 배율(multiplier)은 보석 순번별로 받는다. 9일차 동일 종류 연속 +20%, 10일차 피버 +50%가 들어갈 자리다.
    /// </summary>
    public static class GemEffectResolver
    {
        public static EffectSummary Resolve(IReadOnlyList<GemType> gems, CombatStats stats, GemEffectSettings settings,
            Func<int, float> multiplier = null)
        {
            if (gems == null) throw new ArgumentNullException(nameof(gems));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (gems.Count == 0) throw new ArgumentException("효과를 계산할 보석이 없습니다.", nameof(gems));

            var events = new List<GemEffectEvent>(gems.Count);
            var delay = 0;
            var actionPoints = 0;

            for (var i = 0; i < gems.Count; i++)
            {
                var gem = gems[i];
                var scale = multiplier?.Invoke(i) ?? 1f;
                if (scale < 0f) throw new ArgumentOutOfRangeException(nameof(multiplier), scale, "배율은 0 이상이어야 합니다.");

                switch (gem)
                {
                    case GemType.Physical:
                        events.Add(new GemEffectEvent(i, gem, GemEffectKind.PhysicalDamage, Scaled(stats.PhysicalAttack, settings.Coefficient(gem), scale), scale));
                        break;

                    case GemType.Magic:
                        events.Add(new GemEffectEvent(i, gem, GemEffectKind.MagicDamage, Scaled(stats.MagicAttack, settings.Coefficient(gem), scale), scale));
                        break;

                    case GemType.Heal:
                        events.Add(new GemEffectEvent(i, gem, GemEffectKind.Heal, Scaled(stats.HealPower, settings.Coefficient(gem), scale), scale));
                        break;

                    case GemType.Chaos:
                        var addDelay = Math.Min(settings.DelayPerGem, settings.MaxDelay - delay);
                        delay += addDelay;
                        events.Add(new GemEffectEvent(i, gem, GemEffectKind.Delay, addDelay, scale));
                        break;

                    case GemType.Balance:
                        var addPoints = Math.Min(settings.ActionPointsPerGem, settings.MaxNextTurnActionPoints - actionPoints);
                        actionPoints += addPoints;
                        events.Add(new GemEffectEvent(i, gem, GemEffectKind.NextTurnActionPoints, addPoints, scale));
                        break;

                    default:
                        events.Add(new GemEffectEvent(i, gem, GemEffectKind.None, 0, scale));
                        break;
                }
            }

            return new EffectSummary(events);
        }

        /// <summary>스탯 × 계수 × 배율을 반올림(0.5는 올림). 최소 1.</summary>
        public static int Scaled(int stat, float coefficient, float multiplier) =>
            Math.Max(1, (int)Math.Round(stat * coefficient * multiplier, MidpointRounding.AwayFromZero));
    }
}
