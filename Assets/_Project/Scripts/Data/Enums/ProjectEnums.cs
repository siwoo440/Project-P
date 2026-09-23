namespace ProjectP.Data
{
    /// <summary>보석 종류. 기획서 5.2</summary>
    public enum GemType
    {
        Physical,   // 물리 - 적에게 물리 피해
        Magic,      // 마법 - 적에게 마법 피해
        Heal,       // 회복 - 메인 캐릭터 체력 회복
        Chaos,      // 혼돈 - 대상 적의 행동 카운트 +1 (행동 지연)
        Balance,    // 균형 - 다음 플레이어 턴 행동력 증가
        Special     // 강화·특수 - 콤보 또는 스킬로 생성되는 확장 요소
    }

    /// <summary>방 종류. 기획서 4.3</summary>
    public enum RoomType
    {
        Battle,
        EliteBattle,
        Boss,
        Shop,
        Recovery,
        Event,
        Reward,
        Story
    }

    /// <summary>
    /// 03_Gameplay 내부 상태. 기획서 12.8
    /// 별도 Scene으로 만들지 않는다.
    /// </summary>
    public enum GameplayStateType
    {
        Exploration,
        Battle,
        Shop,
        Event,
        Reward,
        Recovery,   // 확장 예정
        Story,      // 확장 예정
        Boss        // 확장 예정
    }

    /// <summary>파티 내 역할. 기획서 7.2. 전투 중 교체하지 않는다.</summary>
    public enum PartyRole
    {
        Main,
        Passive
    }

    /// <summary>이동 화면의 방향 선택. 기획서 4.2</summary>
    public enum MoveDirection
    {
        Left,
        Forward,
        Right
    }
}
