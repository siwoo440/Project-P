using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectP.Data;
using ProjectP.Gameplay.Puzzle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>
    /// 보석 효과 계산 자동 테스트. 기획서 5.2 / 5.5 / 5.6
    /// + 제안값(계수 1.0, 혼돈 최대 +2, 균형 최대 +3, 보석마다 반올림·최소 1,
    ///   연속 +20%, 4연속 강공격·추가 회복 +50%, 지연 강화 +1, 균형 +1, 특수 보석 물리 ×3·마법 ×1.5 모든 적·회복 ×3·혼돈 +1 모든 적·균형 +2)
    /// </summary>
    public class GemEffectResolverTests
    {
        private static readonly CombatStats Stats = new CombatStats(physicalAttack: 10, magicAttack: 12, healPower: 8);

        private static GemEffectSettings Settings(float physical = 1f, float magic = 1f, float heal = 1f, int maxNextTurn = 3) =>
            new GemEffectSettings(
                new Dictionary<GemType, float> { { GemType.Physical, physical }, { GemType.Magic, magic }, { GemType.Heal, heal } },
                delayPerGem: 1, maxDelay: 2, actionPointsPerGem: 1, maxNextTurnActionPoints: maxNextTurn);

        private static EffectSummary Resolve(params GemType[] gems) => GemEffectResolver.Resolve(gems, Stats, Settings());

        private static EffectSummary Resolve(params PathGem[] gems) => GemEffectResolver.Resolve(gems, Stats, Settings());

        private static PathGem Special(GemType type) => new PathGem(type, isSpecial: true);

        private static int[] Amounts(EffectSummary summary) => summary.Events.Select(effect => effect.Amount).ToArray();

        // ---------------------------------------------------------------- 기본 효과 (8일차)

        [Test]
        public void EachGem_UsesMatchingStatTimesCoefficient()
        {
            var summary = GemEffectResolver.Resolve(new[] { GemType.Physical, GemType.Magic, GemType.Heal }, Stats,
                Settings(physical: 1f, magic: 1.5f, heal: 2f));

            Assert.AreEqual(10, summary.PhysicalDamage, "물리 10 × 1.0");
            Assert.AreEqual(18, summary.MagicDamage, "마법 12 × 1.5");
            Assert.AreEqual(16, summary.Heal, "회복력 8 × 2.0");
            Assert.AreEqual(28, summary.TotalDamage);
        }

        [Test]
        public void Events_FollowConnectionOrder()
        {
            var summary = Resolve(GemType.Heal, GemType.Physical, GemType.Chaos, GemType.Physical, GemType.Balance);

            CollectionAssert.AreEqual(
                new[] { GemEffectKind.Heal, GemEffectKind.PhysicalDamage, GemEffectKind.Delay, GemEffectKind.PhysicalDamage, GemEffectKind.NextTurnActionPoints },
                summary.Events.Select(effect => effect.Kind).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, summary.Events.Select(effect => effect.Index).ToArray());
        }

        [Test]
        public void MixedPath_TotalsPerKind()
        {
            var summary = Resolve(GemType.Physical, GemType.Physical, GemType.Magic, GemType.Heal, GemType.Chaos, GemType.Balance);

            Assert.AreEqual(22, summary.PhysicalDamage, "10 + 10×1.2 (2연속)");
            Assert.AreEqual(12, summary.MagicDamage);
            Assert.AreEqual(8, summary.Heal);
            Assert.AreEqual(1, summary.Delay);
            Assert.AreEqual(1, summary.NextTurnActionPoints);
        }

        [Test]
        public void Chaos_StopsAtMaxDelay()
        {
            var summary = Resolve(GemType.Chaos, GemType.Chaos, GemType.Chaos);

            Assert.AreEqual(2, summary.Delay, "혼돈 3개라도 한 번 연결에 최대 +2");
            CollectionAssert.AreEqual(new[] { 1, 1, 0 }, Amounts(summary), "상한을 넘은 몫은 0");
        }

        [Test]
        public void Balance_StopsAtMaxNextTurnBonus()
        {
            var summary = Resolve(GemType.Balance, GemType.Balance, GemType.Balance, GemType.Balance, GemType.Balance, GemType.Balance);
            Assert.AreEqual(3, summary.NextTurnActionPoints, "균형 6개(4연속 +1 포함)라도 최대 +3");
        }

        [Test]
        public void Rounding_IsPerGemHalfUp_AndAtLeastOne()
        {
            Assert.AreEqual(13, GemEffectResolver.Scaled(10, 1f, 1.25f), "12.5 → 13 (0.5는 올림)");
            Assert.AreEqual(12, GemEffectResolver.Scaled(10, 1f, 1.24f), "12.4 → 12");
            Assert.AreEqual(1, GemEffectResolver.Scaled(1, 0.1f, 1f), "0.1이어도 최소 1");
            Assert.AreEqual(1, GemEffectResolver.Scaled(0, 1f, 1f), "스탯 0이어도 최소 1");
        }

        [Test]
        public void ExternalMultiplier_IsAppliedPerGem_AndStacksWithChain()
        {
            // 10일차 피버 자리. 끊긴 경로는 외부 배율만, 이어진 경로는 연속 배율 × 외부 배율.
            var extra = new[] { 1f, 1.5f, 2f };
            var mixed = GemEffectResolver.Resolve(new[] { GemType.Physical, GemType.Magic, GemType.Physical }, Stats, Settings(), i => extra[i]);
            CollectionAssert.AreEqual(new[] { 10, 18, 20 }, Amounts(mixed));

            var chained = GemEffectResolver.Resolve(new[] { GemType.Physical, GemType.Physical }, Stats, Settings(), _ => 1.5f);
            CollectionAssert.AreEqual(new[] { 15, 18 }, Amounts(chained), "10×1.5 / 10×1.2×1.5");
            Assert.AreEqual(1.8f, chained.Events[1].Multiplier, 1e-4f, "적용 배율 = 연속 1.2 × 외부 1.5");
        }

        [Test]
        public void UndefinedGem_HasNoEffect()
        {
            var summary = Resolve(GemType.Special, GemType.Physical);
            Assert.AreEqual(GemEffectKind.None, summary.Events[0].Kind);
            Assert.AreEqual(10, summary.TotalDamage);
        }

        [Test]
        public void InvalidInput_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => GemEffectResolver.Resolve(Array.Empty<GemType>(), Stats, Settings()));
            Assert.Throws<ArgumentNullException>(() => GemEffectResolver.Resolve((IReadOnlyList<GemType>)null, Stats, Settings()));
            Assert.Throws<ArgumentNullException>(() => GemEffectResolver.Resolve((IReadOnlyList<PathGem>)null, Stats, Settings()));
            Assert.Throws<ArgumentNullException>(() => GemEffectResolver.Resolve(new[] { GemType.Physical }, Stats, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GemEffectResolver.Resolve(new[] { GemType.Physical }, Stats, Settings(), _ => -1f));
        }

        // ---------------------------------------------------------------- 동일 종류 연속 (9일차)

        [Test]
        public void SameTypeChain_AddsTwentyPercentPerGem()
        {
            var summary = Resolve(GemType.Physical, GemType.Physical, GemType.Physical);

            CollectionAssert.AreEqual(new[] { 10, 12, 14 }, Amounts(summary), "기획서 5.5 — 100% / 120% / 140%");
            CollectionAssert.AreEqual(new[] { 1f, 1.2f, 1.4f }, summary.ChainMultipliers.ToArray());
            Assert.AreEqual(0, summary.ComboCount, "3연속은 연속 강화 없음");
        }

        [Test]
        public void FourChain_Physical_AddsStrongAttack()
        {
            var summary = Resolve(GemType.Physical, GemType.Physical, GemType.Physical, GemType.Physical);

            // 10 + 12 + 14 + 16 = 52 → 강공격 52 × 0.5 = 26
            Assert.AreEqual(5, summary.Events.Count);
            var combo = summary.Events[4];
            Assert.AreEqual(EffectSource.Combo, combo.Source);
            Assert.AreEqual(GemEffectKind.PhysicalDamage, combo.Kind);
            Assert.AreEqual(3, combo.Index, "구간 마지막 보석 바로 뒤에 처리");
            Assert.AreEqual(26, combo.Amount);
            Assert.AreEqual(78, summary.PhysicalDamage);
            Assert.AreEqual(1, summary.ComboCount);
            Assert.IsNull(summary.CreatedSpecial, "4연속은 특수 보석을 만들지 않는다");
        }

        [Test]
        public void FourChain_Heal_AddsBonusHeal()
        {
            var summary = Resolve(GemType.Heal, GemType.Heal, GemType.Heal, GemType.Heal);

            // 8 + 9.6→10 + 11.2→11 + 12.8→13 = 42 → 추가 회복 21
            CollectionAssert.AreEqual(new[] { 8, 10, 11, 13, 21 }, Amounts(summary));
            Assert.AreEqual(63, summary.Heal);
        }

        [Test]
        public void FourChain_Chaos_AddsDelayBeyondNormalCap()
        {
            var summary = Resolve(GemType.Chaos, GemType.Chaos, GemType.Chaos, GemType.Chaos);

            CollectionAssert.AreEqual(new[] { 1, 1, 0, 0, 1 }, Amounts(summary), "일반 혼돈은 +2에서 멈추고 지연 강화 +1은 따로");
            Assert.AreEqual(3, summary.Delay);
        }

        [Test]
        public void FourChain_Balance_AddsOne_WithinNextTurnCap()
        {
            var roomy = GemEffectResolver.Resolve(Enumerable.Repeat(GemType.Balance, 4).ToArray(), Stats, Settings(maxNextTurn: 10));
            Assert.AreEqual(5, roomy.NextTurnActionPoints, "균형 4개 + 4연속 +1");

            var capped = Resolve(GemType.Balance, GemType.Balance, GemType.Balance, GemType.Balance);
            Assert.AreEqual(3, capped.NextTurnActionPoints, "다음 턴 보너스 상한 +3");
        }

        [Test]
        public void FiveChain_CreatesSpecialOfThatType()
        {
            var summary = Resolve(Enumerable.Repeat(GemType.Magic, 5).ToArray());

            Assert.AreEqual(GemType.Magic, summary.CreatedSpecial);
            Assert.AreEqual(1, summary.ComboCount, "5연속도 연속 강화는 한 번");
            // 12 + 14.4→14 + 16.8→17 + 19.2→19 + 21.6→22 = 84 → 강공격 42
            Assert.AreEqual(126, summary.MagicDamage);
        }

        [Test]
        public void BrokenChain_DoesNotTriggerCombo()
        {
            var summary = Resolve(GemType.Physical, GemType.Physical, GemType.Magic, GemType.Physical, GemType.Physical);

            Assert.AreEqual(0, summary.ComboCount);
            CollectionAssert.AreEqual(new[] { 10, 12, 12, 10, 12 }, Amounts(summary), "끊기면 다시 100%부터");
        }

        // ---------------------------------------------------------------- 특수 보석 (9일차)

        [Test]
        public void SpecialGems_HaveTheirOwnEffects()
        {
            var summary = Resolve(Special(GemType.Physical), Special(GemType.Magic), Special(GemType.Heal), Special(GemType.Chaos), Special(GemType.Balance));
            var events = summary.Events;

            Assert.IsTrue(events.All(effect => effect.Source == EffectSource.Special));
            Assert.AreEqual((GemEffectKind.PhysicalDamage, 30, EffectTarget.Single), (events[0].Kind, events[0].Amount, events[0].Target), "물리 10 × 3, 한 적");
            Assert.AreEqual((GemEffectKind.MagicDamage, 18, EffectTarget.AllEnemies), (events[1].Kind, events[1].Amount, events[1].Target), "마법 12 × 1.5, 모든 적");
            Assert.AreEqual((GemEffectKind.Heal, 24), (events[2].Kind, events[2].Amount), "회복력 8 × 3");
            Assert.AreEqual((GemEffectKind.Delay, 1, EffectTarget.AllEnemies), (events[3].Kind, events[3].Amount, events[3].Target), "모든 적 행동 +1");
            Assert.AreEqual((GemEffectKind.NextTurnActionPoints, 2), (events[4].Kind, events[4].Amount), "다음 턴 행동력 +2");
        }

        [Test]
        public void SpecialGem_BreaksChain_AndGetsNoChainBonus()
        {
            var summary = Resolve(GemType.Physical, Special(GemType.Physical), GemType.Physical);

            CollectionAssert.AreEqual(new[] { 10, 30, 10 }, Amounts(summary), "특수 보석을 사이에 두면 연속이 끊긴다");
            CollectionAssert.AreEqual(new[] { 1f, 1f, 1f }, summary.ChainMultipliers.ToArray());
        }

        [Test]
        public void SpecialGem_GetsExternalMultiplier()
        {
            var summary = GemEffectResolver.Resolve(new[] { Special(GemType.Physical) }, Stats, Settings(), _ => 1.5f);
            Assert.AreEqual(45, summary.PhysicalDamage, "10 × 3 × 1.5 (피버 자리)");
        }

        [Test]
        public void SpecialChaos_IsOutsideNormalCap_SpecialBalance_RespectsNextTurnCap()
        {
            var chaos = Resolve(GemType.Chaos, GemType.Chaos, Special(GemType.Chaos));
            Assert.AreEqual(3, chaos.Delay, "일반 +2 + 특수 +1");

            var balance = Resolve(GemType.Balance, GemType.Balance, Special(GemType.Balance));
            Assert.AreEqual(3, balance.NextTurnActionPoints, "1 + 1 + (2 중 상한까지 1)");
        }
    }
}
