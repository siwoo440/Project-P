namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>손을 뗐을 때 처리 결과.</summary>
    public enum ReleaseOutcome
    {
        /// <summary>취소 영역에서 놓음. 턴을 쓰지 않는다.</summary>
        Canceled,

        /// <summary>최소 개수 미만으로 놓음. 효과 없이 턴만 소모한다(사용자 결정).</summary>
        Wasted,

        /// <summary>최소 개수 이상으로 놓음. 보석을 사용하고 턴을 소모한다.</summary>
        Confirmed
    }

    /// <summary>
    /// 퍼즐 턴 규칙. 기획서 5.4 — 한 턴에는 하나의 연결 경로만 확정할 수 있다. 순수 C#.
    ///
    /// - 취소 영역에서 놓았을 때만 턴을 쓰지 않는다. 그 밖에서 놓으면 개수와 관계없이 턴을 소모한다(사용자 결정).
    ///   우클릭·ESC 취소(기획서 5.3)도 턴을 쓰지 않는다.
    /// - 적 행동을 포함한 실제 턴 순서는 13일차 TurnManager가 맡고, 이 클래스는 퍼즐 쪽 규칙만 가진다.
    /// </summary>
    public sealed class PuzzleTurn
    {
        public PuzzleTurn(ActionPoints actionPoints)
        {
            ActionPoints = actionPoints;
        }

        public ActionPoints ActionPoints { get; }

        /// <summary>턴 번호. 첫 StartTurn 전에는 0.</summary>
        public int Number { get; private set; }

        public bool HasActed { get; private set; }

        public bool CanAct => Number > 0 && !HasActed;

        public void StartTurn()
        {
            Number++;
            HasActed = false;
            ActionPoints.StartTurn();
        }

        public void MarkActed() => HasActed = true;

        public static ReleaseOutcome DecideRelease(int pathLength, int minLength, bool overCancelZone)
        {
            if (overCancelZone) return ReleaseOutcome.Canceled;
            return pathLength >= minLength ? ReleaseOutcome.Confirmed : ReleaseOutcome.Wasted;
        }
    }
}
