using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectP.Gameplay.Battle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>공격 대상 선택 자동 테스트. 기획서 6.3 + 사용자 결정(대상이 쓰러지면 살아 있는 적 중 무작위)</summary>
    public class TargetSelectionTests
    {
        private static TargetSelection New(int count, int seed = 1) => new TargetSelection(count, new Random(seed));

        [Test]
        public void StartsWithLeftmostEnemy()
        {
            Assert.AreEqual(0, New(3).Current);
            Assert.AreEqual(-1, New(0).Current, "적이 없으면 대상 없음");
        }

        [Test]
        public void Select_ChangesToLivingEnemy()
        {
            var targets = New(3);
            Assert.IsTrue(targets.Select(2));
            Assert.AreEqual(2, targets.Current);
        }

        [Test]
        public void Select_RejectsDefeatedOrMissingEnemy()
        {
            var targets = New(3);
            targets.MarkDefeated(1);

            Assert.IsFalse(targets.Select(1), "쓰러진 적은 고를 수 없다");
            Assert.IsFalse(targets.Select(5), "없는 적");
            Assert.IsFalse(targets.Select(-1));
            Assert.AreEqual(0, targets.Current, "대상은 그대로");
        }

        [Test]
        public void DefeatingOtherEnemy_KeepsTarget()
        {
            var targets = New(3);
            targets.Select(2);
            targets.MarkDefeated(0);
            Assert.AreEqual(2, targets.Current);
        }

        [Test]
        public void DefeatingTarget_PicksLivingEnemy()
        {
            var targets = New(3);
            var next = targets.MarkDefeated(0);

            Assert.That(next, Is.EqualTo(1).Or.EqualTo(2), "살아 있는 적 중 하나");
            Assert.IsTrue(targets.IsAlive(next));
        }

        [Test]
        public void DefeatingTarget_ChoiceIsRandomAcrossSeeds()
        {
            var picked = new HashSet<int>();
            for (var seed = 0; seed < 50; seed++)
            {
                var targets = New(4, seed);
                picked.Add(targets.MarkDefeated(0));
            }

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, picked, "시드에 따라 남은 적이 골고루 뽑힌다");
        }

        [Test]
        public void SameSeed_PicksSameTarget()
        {
            Assert.AreEqual(New(5, 42).MarkDefeated(0), New(5, 42).MarkDefeated(0));
        }

        [Test]
        public void DefeatingEveryone_LeavesNoTarget()
        {
            var targets = New(2);
            targets.MarkDefeated(0);
            targets.MarkDefeated(targets.Current);

            Assert.IsFalse(targets.HasTarget);
            Assert.AreEqual(-1, targets.Current);
        }

        [Test]
        public void Revive_WhenNoTarget_BecomesTarget()
        {
            var targets = New(2);
            targets.MarkDefeated(0);
            targets.MarkDefeated(1);

            targets.Revive(1);
            Assert.AreEqual(1, targets.Current);
            Assert.IsTrue(targets.Select(1));
        }

        [Test]
        public void InvalidInput_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TargetSelection(-1, new Random(1)));
            Assert.Throws<ArgumentNullException>(() => new TargetSelection(3, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => New(3).MarkDefeated(3));
        }
    }
}
