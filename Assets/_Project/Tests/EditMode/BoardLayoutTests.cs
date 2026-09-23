using NUnit.Framework;
using ProjectP.Gameplay.Puzzle;

namespace ProjectP.Tests.EditMode
{
    /// <summary>보드 배치·마우스 판정 자동 테스트. 칸 92 + 간격 4 (Dev_PuzzleTest와 같은 값).</summary>
    public class BoardLayoutTests
    {
        private const float HitRatio = 0.8f;
        private static readonly BoardLayout Layout = new BoardLayout(6, 12, 92f, 4f);

        private static BoardPosition P(int row, int column) => new BoardPosition(row, column);

        [Test]
        public void Size_MatchesSixByTwelveBoard()
        {
            Assert.AreEqual(1148f, Layout.Width, 0.001f);
            Assert.AreEqual(572f, Layout.Height, 0.001f);
        }

        [Test]
        public void CellCenter_BottomLeftAndTopRight()
        {
            var (x0, y0) = Layout.CellCenter(P(0, 0));
            Assert.AreEqual(-528f, x0, 0.001f, "맨 왼쪽 칸 중심");
            Assert.AreEqual(-240f, y0, 0.001f, "맨 아래 칸 중심 (행 0 = 아래)");

            var (x1, y1) = Layout.CellCenter(P(5, 11));
            Assert.AreEqual(528f, x1, 0.001f);
            Assert.AreEqual(240f, y1, 0.001f);
        }

        [Test]
        public void TryHit_CellCenter_Hits()
        {
            for (var row = 0; row < 6; row++)
            for (var column = 0; column < 12; column++)
            {
                var (x, y) = Layout.CellCenter(P(row, column));
                Assert.IsTrue(Layout.TryHit(x, y, HitRatio, out var hit));
                Assert.AreEqual(P(row, column), hit);
            }
        }

        [Test]
        public void TryHit_RespectsHitRadius()
        {
            var (x, y) = Layout.CellCenter(P(2, 3));
            var radius = 92f / 2f * HitRatio;

            Assert.IsTrue(Layout.TryHit(x + radius * 0.98f, y, HitRatio, out _), "원 안쪽 경계");
            Assert.IsFalse(Layout.TryHit(x + radius * 1.02f, y, HitRatio, out _), "원 바깥");
        }

        [Test]
        public void TryHit_CornerBetweenFourCells_Misses()
        {
            var (x, y) = Layout.CellCenter(P(2, 3));
            var half = Layout.Pitch / 2f;
            Assert.IsFalse(Layout.TryHit(x + half, y + half, HitRatio, out _), "네 칸이 만나는 모서리는 선택되지 않는다");
        }

        [Test]
        public void TryHit_OutsideBoard_Misses()
        {
            Assert.IsFalse(Layout.TryHit(-600f, 0f, HitRatio, out _));
            Assert.IsFalse(Layout.TryHit(0f, 300f, HitRatio, out _));
        }

        [Test]
        public void HitsAlong_FastDiagonalDrag_HitsOnlyDiagonalCells()
        {
            var (x0, y0) = Layout.CellCenter(P(0, 0));
            var (x1, y1) = Layout.CellCenter(P(3, 3));

            var hits = Layout.HitsAlong(x0, y0, x1, y1, HitRatio);

            CollectionAssert.AreEqual(new[] { P(0, 0), P(1, 1), P(2, 2), P(3, 3) }, hits,
                "대각선으로 빠르게 그어도 옆 칸이 끼어들지 않아야 한다");
        }

        [Test]
        public void HitsAlong_FastHorizontalDrag_HitsEveryCellInOrder()
        {
            var (x0, y0) = Layout.CellCenter(P(1, 2));
            var (x1, y1) = Layout.CellCenter(P(1, 7));

            var hits = Layout.HitsAlong(x0, y0, x1, y1, HitRatio);

            CollectionAssert.AreEqual(new[] { P(1, 2), P(1, 3), P(1, 4), P(1, 5), P(1, 6), P(1, 7) }, hits);
        }
    }
}
