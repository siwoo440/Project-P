using NUnit.Framework;
using ProjectP.Gameplay.Puzzle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>퍼즐 턴 규칙 자동 테스트. 기획서 5.4 + 사용자 결정(놓으면 턴 소모, 취소 영역에서만 취소)</summary>
    public class PuzzleTurnTests
    {
        [Test]
        public void StartTurn_IncrementsNumberAndResetsAction()
        {
            var turn = new PuzzleTurn(new ActionPoints(6));
            Assert.IsFalse(turn.CanAct, "첫 턴 시작 전에는 행동할 수 없다");

            turn.StartTurn();
            Assert.AreEqual(1, turn.Number);
            Assert.IsTrue(turn.CanAct);
            Assert.AreEqual(6, turn.ActionPoints.Current);
        }

        [Test]
        public void OnlyOneActionPerTurn()
        {
            var turn = new PuzzleTurn(new ActionPoints(6));
            turn.StartTurn();
            turn.MarkActed();
            Assert.IsFalse(turn.CanAct, "한 턴에 한 번만");

            turn.StartTurn();
            Assert.IsTrue(turn.CanAct, "다음 턴에는 다시 가능");
            Assert.AreEqual(2, turn.Number);
        }

        [Test]
        public void Release_OverCancelZone_IsCanceledRegardlessOfLength()
        {
            Assert.AreEqual(ReleaseOutcome.Canceled, PuzzleTurn.DecideRelease(1, 2, overCancelZone: true));
            Assert.AreEqual(ReleaseOutcome.Canceled, PuzzleTurn.DecideRelease(6, 2, overCancelZone: true));
        }

        [Test]
        public void Release_BelowMinimum_WastesTurn()
        {
            Assert.AreEqual(ReleaseOutcome.Wasted, PuzzleTurn.DecideRelease(1, 2, overCancelZone: false));
        }

        [Test]
        public void Release_AtOrAboveMinimum_Confirms()
        {
            Assert.AreEqual(ReleaseOutcome.Confirmed, PuzzleTurn.DecideRelease(2, 2, overCancelZone: false));
            Assert.AreEqual(ReleaseOutcome.Confirmed, PuzzleTurn.DecideRelease(6, 2, overCancelZone: false));
        }

        [Test]
        public void PathLimit_FollowsActionPoints()
        {
            var turn = new PuzzleTurn(new ActionPoints(6));
            turn.StartTurn();
            var path = new ConnectionPath(new Board(6, 12), 2, turn.ActionPoints.Current);

            path.Begin(new BoardPosition(0, 0));
            for (var column = 1; column < 6; column++) Assert.AreEqual(ConnectionResult.Added, path.TryAdd(new BoardPosition(0, column)));

            Assert.AreEqual(ConnectionResult.LimitReached, path.TryAdd(new BoardPosition(0, 6)), "행동력 6이면 7번째는 이어지지 않는다");
        }
    }
}
