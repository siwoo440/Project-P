using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectP.Data;
using ProjectP.Gameplay.Puzzle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>보석 효과 계산 자동 테스트. 기획서 5.2 / 5.5 + 제안값(계수 1.0, 혼돈 최대 +2, 균형 최대 +3, 보석마다 반올림·최소 1)</summary>
    public class GemEffectResolverTests
    {
        private static readonly CombatStats Stats = new CombatStats(physicalAttack: 10, magicAttack: 12, healPower: 8);

        private static GemEffectSettings Settings(float physical = 1f, float magic = 1f, float heal = 1f) =>
            new GemEffectSettings(
                new Dictionary<GemType, float> { { GemType.Physical, physical }, { GemType.Magic, magic }, { GemType.Heal, heal } },
                delayPerGem: 1, maxDelay: 2, actionPointsPerGem: 1, maxNextTurnActionPoints: 3);

        private static EffectSummary Resolve(params GemType[] gems) => GemEffectResolver.Resolve(gems, Stats, Settings());

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

            Assert.AreEqual(20, summary.PhysicalDamage);
            Assert.AreEqual(12, summary.MagicDamage);
            Assert.AreEqual(8, summary.Heal);
            Assert.AreEqual(1, summary.Delay);
            Assert.AreEqual(1, summary.NextTurnActionPoints);
        }

        [Test]
        public void Chaos_StopsAtMaxDelay()
        {
            var summary = Resolve(GemType.Chaos, GemType.Chaos, GemType.Chaos, GemType.Chaos);

            Assert.AreEqual(2, summary.Delay, "혼돈 4개라도 한 번 연결에 최대 +2");
            CollectionAssert.AreEqual(new[] { 1, 1, 0, 0 }, summary.Events.Select(effect => effect.Amount).ToArray(), "상한을 넘은 몫은 0");
        }

        [Test]
        public void Balance_StopsAtMaxNextTurnBonus()
        {
            var summary = Resolve(GemType.Balance, GemType.Balance, GemType.Balance, GemType.Balance, GemType.Balance, GemType.Balance);
            Assert.AreEqual(3, summary.NextTurnActionPoints, "균형 6개라도 최대 +3");
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
        public void Multiplier_IsAppliedPerGem()
        {
            // 9일차 연속 보너스 자리: 두 번째 보석부터 1.2, 1.4배
            var multipliers = new[] { 1f, 1.2f, 1.4f };
            var summary = GemEffectResolver.Resolve(new[] { GemType.Physical, GemType.Physical, GemType.Physical }, Stats, Settings(),
                index => multipliers[index]);

            CollectionAssert.AreEqual(new[] { 10, 12, 14 }, summary.Events.Select(effect => effect.Amount).ToArray());
            Assert.AreEqual(36, summary.PhysicalDamage);
        }

        [Test]
        public void SpecialGem_HasNoEffectYet()
        {
            var summary = Resolve(GemType.Special, GemType.Physical);
            Assert.AreEqual(GemEffectKind.None, summary.Events[0].Kind);
            Assert.AreEqual(10, summary.TotalDamage);
        }

        [Test]
        public void InvalidInput_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => GemEffectResolver.Resolve(Array.Empty<GemType>(), Stats, Settings()));
            Assert.Throws<ArgumentNullException>(() => GemEffectResolver.Resolve(null, Stats, Settings()));
            Assert.Throws<ArgumentNullException>(() => GemEffectResolver.Resolve(new[] { GemType.Physical }, Stats, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GemEffectResolver.Resolve(new[] { GemType.Physical }, Stats, Settings(), _ => -1f));
        }
    }
}
