using System;
using System.Linq;
using NUnit.Framework;
using ProjectP.Data;
using ProjectP.Gameplay.Puzzle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>동일 종류 연속 규칙 자동 테스트. 기획서 5.5 / 5.6 + 제안값(상한 없음, 특수 보석은 연속을 끊음, 한 번 연결에 특수 보석 1개)</summary>
    public class ChainBonusTests
    {
        private const float Step = 0.2f;

        private static PathGem[] Path(params GemType[] types) => types.Select(type => new PathGem(type)).ToArray();

        private static PathGem Special(GemType type) => new PathGem(type, isSpecial: true);

        [Test]
        public void Runs_SplitWhenTypeChanges()
        {
            var runs = ChainBonus.FindRuns(Path(GemType.Physical, GemType.Physical, GemType.Magic, GemType.Physical));

            CollectionAssert.AreEqual(
                new[] { (GemType.Physical, 0, 2), (GemType.Magic, 2, 1), (GemType.Physical, 3, 1) },
                runs.Select(run => (run.Type, run.Start, run.Length)).ToArray());
            Assert.AreEqual(1, runs[0].End);
        }

        [Test]
        public void Multipliers_StackFromSecondGem()
        {
            CollectionAssert.AreEqual(new[] { 1f, 1.2f, 1.4f },
                ChainBonus.Multipliers(Path(GemType.Heal, GemType.Heal, GemType.Heal), Step));
        }

        [Test]
        public void Multipliers_ResetAfterBreak()
        {
            CollectionAssert.AreEqual(new[] { 1f, 1.2f, 1f, 1f, 1.2f },
                ChainBonus.Multipliers(Path(GemType.Physical, GemType.Physical, GemType.Magic, GemType.Physical, GemType.Physical), Step));
        }

        [Test]
        public void Multipliers_HaveNoCap()
        {
            var multipliers = ChainBonus.Multipliers(Path(Enumerable.Repeat(GemType.Magic, 9).ToArray()), Step);
            Assert.AreEqual(2.6f, multipliers[8], 1e-5f, "9연속이면 260% — 길이는 행동력이 막는다");
        }

        [Test]
        public void SpecialGem_BreaksRun_AndHasNoBonus()
        {
            var gems = new[] { new PathGem(GemType.Physical), Special(GemType.Physical), new PathGem(GemType.Physical) };

            Assert.AreEqual(2, ChainBonus.FindRuns(gems).Count, "특수 보석은 구간에 들어가지 않는다");
            CollectionAssert.AreEqual(new[] { 1f, 1f, 1f }, ChainBonus.Multipliers(gems, Step));
        }

        [Test]
        public void SpecialToCreate_NeedsFiveInARow()
        {
            Assert.IsNull(ChainBonus.SpecialToCreate(Path(Enumerable.Repeat(GemType.Chaos, 4).ToArray()), 5));
            Assert.AreEqual(GemType.Chaos, ChainBonus.SpecialToCreate(Path(Enumerable.Repeat(GemType.Chaos, 5).ToArray()), 5));
            Assert.IsNull(ChainBonus.SpecialToCreate(Path(GemType.Chaos, GemType.Chaos, GemType.Chaos, GemType.Magic, GemType.Chaos, GemType.Chaos), 5),
                "합쳐서 5개여도 끊기면 안 된다");
        }

        [Test]
        public void SpecialToCreate_OnlyOnePerConnection_LastQualifyingRun()
        {
            var gems = Path(Enumerable.Repeat(GemType.Physical, 5).Concat(Enumerable.Repeat(GemType.Magic, 5)).ToArray());
            Assert.AreEqual(GemType.Magic, ChainBonus.SpecialToCreate(gems, 5));
        }

        [Test]
        public void InvalidInput_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ChainBonus.FindRuns(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChainBonus.Multipliers(Path(GemType.Physical), -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChainSettings(comboLength: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChainSettings(bonusPerGem: -1f));
        }
    }
}
