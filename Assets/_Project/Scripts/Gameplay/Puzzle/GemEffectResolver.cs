using System;
using System.Collections.Generic;
using System.Linq;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보석 효과 계산. 기획서 5.2 / 5.5 / 5.6 — 순수 C#.
    ///
    /// - 경로의 보석을 연결 순서대로 하나씩 계산해 결과표(EffectSummary)로 돌려준다. 대상에 적용하지 않는다.
    /// - 피해·회복 = 메인 스탯 × 보석 계수 × 연속 배율 × 외부 배율. 보석마다 반올림(0.5는 올림)하고 최소 1.
    ///   연속 배율은 ChainBonus(같은 종류 2번째부터 +20% 누적), 외부 배율(multiplier)은 10일차 피버 +50% 자리다.
    /// - 혼돈: 1개당 DelayPerGem, 한 번 연결에 최대 MaxDelay. 상한을 넘은 몫은 0. 혼돈·균형 양은 배율을 받지 않는다.
    /// - 균형: 1개당 ActionPointsPerGem, 다음 턴 보너스는 최대 MaxNextTurnActionPoints. 상한을 넘은 몫은 0.
    /// - 4연속 이상 구간이 끝나는 자리에 연속 강화 효과를 한 건 더 넣는다:
    ///   물리·마법 = 강공격(구간 피해 합 × 비율), 회복 = 추가 회복(구간 회복 합 × 비율),
    ///   혼돈 = 지연 강화(+ComboDelay, 혼돈 상한과 별개), 균형 = 다음 턴 행동력 +ComboActionPoints(보너스 상한은 지킴).
    /// - 특수 보석: 물리 = 한 적에게 물리 스탯 × 배수, 마법 = 모든 적에게 마법 스탯 × 배수, 회복 = 회복력 × 배수,
    ///   혼돈 = 모든 적 행동 +N(혼돈 상한과 별개), 균형 = 다음 턴 행동력 +N(보너스 상한은 지킴). 연속 배율은 받지 않는다.
    /// - 5연속 이상 구간이 있으면 결과표에 만들 특수 보석 종류를 적는다. 실제 생성은 PuzzleManager가 한다.
    /// </summary>
    public static class GemEffectResolver
    {
        public static EffectSummary Resolve(IReadOnlyList<GemType> gems, CombatStats stats, GemEffectSettings settings,
            Func<int, float> multiplier = null)
        {
            if (gems == null) throw new ArgumentNullException(nameof(gems));
            return Resolve(gems.Select(gem => new PathGem(gem)).ToList(), stats, settings, multiplier);
        }

        public static EffectSummary Resolve(IReadOnlyList<PathGem> gems, CombatStats stats, GemEffectSettings settings,
            Func<int, float> multiplier = null)
        {
            if (gems == null) throw new ArgumentNullException(nameof(gems));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (gems.Count == 0) throw new ArgumentException("효과를 계산할 보석이 없습니다.", nameof(gems));

            var chain = settings.Chain;
            var chainMultipliers = ChainBonus.Multipliers(gems, chain.BonusPerGem);
            var combos = new Dictionary<int, ChainRun>();
            foreach (var run in ChainBonus.FindRuns(gems))
            {
                if (run.Length >= chain.ComboLength) combos[run.End] = run;
            }

            var events = new List<GemEffectEvent>(gems.Count + combos.Count);
            var delay = 0;
            var actionPoints = 0;

            int AddActionPoints(int amount)
            {
                var added = Math.Max(0, Math.Min(amount, settings.MaxNextTurnActionPoints - actionPoints));
                actionPoints += added;
                return added;
            }

            for (var i = 0; i < gems.Count; i++)
            {
                var gem = gems[i];
                var extra = multiplier?.Invoke(i) ?? 1f;
                if (extra < 0f) throw new ArgumentOutOfRangeException(nameof(multiplier), extra, "배율은 0 이상이어야 합니다.");

                if (gem.IsSpecial)
                {
                    events.Add(SpecialEffect(i, gem.Type, stats, chain, extra, AddActionPoints));
                }
                else
                {
                    var scale = chainMultipliers[i] * extra;
                    switch (gem.Type)
                    {
                        case GemType.Physical:
                            events.Add(new GemEffectEvent(i, gem.Type, GemEffectKind.PhysicalDamage, Scaled(stats.PhysicalAttack, settings.Coefficient(gem.Type), scale), scale));
                            break;

                        case GemType.Magic:
                            events.Add(new GemEffectEvent(i, gem.Type, GemEffectKind.MagicDamage, Scaled(stats.MagicAttack, settings.Coefficient(gem.Type), scale), scale));
                            break;

                        case GemType.Heal:
                            events.Add(new GemEffectEvent(i, gem.Type, GemEffectKind.Heal, Scaled(stats.HealPower, settings.Coefficient(gem.Type), scale), scale));
                            break;

                        case GemType.Chaos:
                            var addDelay = Math.Max(0, Math.Min(settings.DelayPerGem, settings.MaxDelay - delay));
                            delay += addDelay;
                            events.Add(new GemEffectEvent(i, gem.Type, GemEffectKind.Delay, addDelay, scale));
                            break;

                        case GemType.Balance:
                            events.Add(new GemEffectEvent(i, gem.Type, GemEffectKind.NextTurnActionPoints, AddActionPoints(settings.ActionPointsPerGem), scale));
                            break;

                        default:
                            events.Add(new GemEffectEvent(i, gem.Type, GemEffectKind.None, 0, scale));
                            break;
                    }
                }

                if (combos.TryGetValue(i, out var combo)) events.Add(ComboEffect(combo, events, chain, AddActionPoints));
            }

            return new EffectSummary(events, chainMultipliers, ChainBonus.SpecialToCreate(gems, chain.SpecialLength));
        }

        /// <summary>스탯 × 계수 × 배율을 반올림(0.5는 올림). 최소 1.</summary>
        public static int Scaled(int stat, float coefficient, float multiplier) =>
            Math.Max(1, (int)Math.Round(stat * coefficient * multiplier, MidpointRounding.AwayFromZero));

        /// <summary>연속 강화 한 건. 구간 안 일반 보석 효과 합을 기준으로 한다.</summary>
        private static GemEffectEvent ComboEffect(ChainRun run, List<GemEffectEvent> events, ChainSettings chain, Func<int, int> addActionPoints)
        {
            var runTotal = events
                .Where(effect => effect.Source == EffectSource.Gem && effect.Index >= run.Start && effect.Index <= run.End)
                .Sum(effect => effect.Amount);

            switch (run.Type)
            {
                case GemType.Physical:
                    return new GemEffectEvent(run.End, run.Type, GemEffectKind.PhysicalDamage, Scaled(runTotal, 1f, chain.ComboDamageRatio), chain.ComboDamageRatio, EffectSource.Combo);

                case GemType.Magic:
                    return new GemEffectEvent(run.End, run.Type, GemEffectKind.MagicDamage, Scaled(runTotal, 1f, chain.ComboDamageRatio), chain.ComboDamageRatio, EffectSource.Combo);

                case GemType.Heal:
                    return new GemEffectEvent(run.End, run.Type, GemEffectKind.Heal, Scaled(runTotal, 1f, chain.ComboHealRatio), chain.ComboHealRatio, EffectSource.Combo);

                case GemType.Chaos:
                    return new GemEffectEvent(run.End, run.Type, GemEffectKind.Delay, chain.ComboDelay, 1f, EffectSource.Combo);

                case GemType.Balance:
                    return new GemEffectEvent(run.End, run.Type, GemEffectKind.NextTurnActionPoints, addActionPoints(chain.ComboActionPoints), 1f, EffectSource.Combo);

                default:
                    return new GemEffectEvent(run.End, run.Type, GemEffectKind.None, 0, 1f, EffectSource.Combo);
            }
        }

        private static GemEffectEvent SpecialEffect(int index, GemType type, CombatStats stats, ChainSettings chain, float extra, Func<int, int> addActionPoints)
        {
            var power = chain.SpecialPower(type);
            switch (type)
            {
                case GemType.Physical:
                    return new GemEffectEvent(index, type, GemEffectKind.PhysicalDamage, Scaled(stats.PhysicalAttack, power, extra), extra, EffectSource.Special);

                case GemType.Magic:
                    return new GemEffectEvent(index, type, GemEffectKind.MagicDamage, Scaled(stats.MagicAttack, power, extra), extra, EffectSource.Special, EffectTarget.AllEnemies);

                case GemType.Heal:
                    return new GemEffectEvent(index, type, GemEffectKind.Heal, Scaled(stats.HealPower, power, extra), extra, EffectSource.Special);

                case GemType.Chaos:
                    return new GemEffectEvent(index, type, GemEffectKind.Delay, Round(power), 1f, EffectSource.Special, EffectTarget.AllEnemies);

                case GemType.Balance:
                    return new GemEffectEvent(index, type, GemEffectKind.NextTurnActionPoints, addActionPoints(Round(power)), 1f, EffectSource.Special);

                default:
                    return new GemEffectEvent(index, type, GemEffectKind.None, 0, 1f, EffectSource.Special);
            }
        }

        private static int Round(float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
