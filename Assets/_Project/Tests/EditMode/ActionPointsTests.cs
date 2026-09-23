using System;
using NUnit.Framework;
using ProjectP.Gameplay.Puzzle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>행동력 규칙 자동 테스트. 기획서 5.4</summary>
    public class ActionPointsTests
    {
        [Test]
        public void StartTurn_UsesBaseOfSix()
        {
            var points = new ActionPoints(6);
            Assert.AreEqual(0, points.Current, "첫 턴 시작 전에는 0");

            points.StartTurn();
            Assert.AreEqual(6, points.Current);
        }

        [Test]
        public void NextTurnBonus_AppliesOnceThenClears()
        {
            var points = new ActionPoints(6);
            points.StartTurn();
            points.AddNextTurnBonus(2);

            Assert.AreEqual(6, points.Current, "보너스는 이번 턴에 적용되지 않는다");

            points.StartTurn();
            Assert.AreEqual(8, points.Current, "다음 턴에 적용");
            Assert.AreEqual(2, points.AppliedBonus, "이번 턴 보너스 몫을 알 수 있다");
            Assert.AreEqual(0, points.NextTurnBonus);

            points.StartTurn();
            Assert.AreEqual(6, points.Current, "그다음 턴에는 사라진다");
            Assert.AreEqual(0, points.AppliedBonus);
        }

        [Test]
        public void NextTurnBonus_Accumulates()
        {
            var points = new ActionPoints(6);
            points.AddNextTurnBonus(1);
            points.AddNextTurnBonus(2);
            points.StartTurn();
            Assert.AreEqual(9, points.Current);
        }

        [Test]
        public void UnusedPoints_DoNotCarryOver()
        {
            var points = new ActionPoints(6);
            points.StartTurn();
            // 이번 턴에 2개만 이었다고 해도 남은 4는 넘어가지 않는다.
            points.StartTurn();
            Assert.AreEqual(6, points.Current);
        }

        [Test]
        public void SetBase_AppliesFromNextTurn()
        {
            var points = new ActionPoints(6);
            points.StartTurn();
            points.SetBase(8);

            Assert.AreEqual(6, points.Current, "이번 턴 도중에는 바뀌지 않는다");
            points.StartTurn();
            Assert.AreEqual(8, points.Current);
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ActionPoints(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ActionPoints(6).SetBase(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ActionPoints(6).AddNextTurnBonus(-1));
        }
    }
}
