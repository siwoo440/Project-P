namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>연결 경로에 칸을 추가하려 한 결과. 화면은 결과별로 다르게 반응할 수 있다.</summary>
    public enum ConnectionResult
    {
        /// <summary>시작하지 않은 경로에 추가하려 함.</summary>
        NotStarted,

        /// <summary>경로에 추가됨.</summary>
        Added,

        /// <summary>마지막 칸과 같은 칸. 드래그 중 같은 칸 위에 머무를 때 자주 발생한다.</summary>
        Unchanged,

        /// <summary>직전 칸으로 되돌아가 마지막 선택을 취소함.</summary>
        Backtracked,

        /// <summary>마지막 칸과 8방향으로 인접하지 않음.</summary>
        NotAdjacent,

        /// <summary>이미 경로에 있는 칸. 기획서 5.3 — 재선택 금지.</summary>
        AlreadySelected,

        /// <summary>보드 밖 좌표.</summary>
        OutOfBoard,

        /// <summary>최대 개수(행동력)에 도달함. 기획서 5.4 — 7일차부터 행동력이 최대 개수가 된다.</summary>
        LimitReached
    }
}
