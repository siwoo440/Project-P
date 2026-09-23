using System;
using System.Collections.Generic;

namespace ProjectP.Data
{
    /// <summary>
    /// 스테이지 1회 진행 동안만 존재하는 상태. 스테이지 종료 시 통째로 폐기한다.
    ///
    /// [핵심 규칙]
    /// 스테이지 진행 상태는 전부 이 클래스 안에만 존재한다.
    /// MonoBehaviour는 이 데이터를 읽고 쓰는 도구일 뿐, 상태를 소유하지 않는다.
    /// 이 규칙을 지켜야 "방 진입 직전 스냅샷 저장"과 "패배 재도전"이 같은 메커니즘으로 동작한다.
    ///
    /// 기획서 12.8(공통 데이터), 11.1(저장 규칙), 4.4(재도전)
    /// </summary>
    [Serializable]
    public class SessionData
    {
        // --- 스테이지 진행 ---
        public string stageId;
        public int currentRoomIndex;

        /// <summary>지나온 방 순서. 후퇴·재방문 금지 판정에 사용한다. 기획서 4.2</summary>
        public List<int> visitedPath = new List<int>();

        // --- 메인 캐릭터 상태 ---
        /// <summary>전투에서 감소한 체력은 스테이지 내 다음 방까지 유지된다. 기획서 4.4</summary>
        public int mainCurrentHp;

        // --- 스테이지 임시 자원 (스테이지 종료 시 소멸) ---
        /// <summary>상점 전용 임시 재화. 영구 재화와 절대 섞지 않는다. 기획서 8.3, 8.4</summary>
        public int tempCurrency;

        /// <summary>상점에서 구매한 임시 강화. SaveData에 직접 쓰지 않는다. 기획서 12.11</summary>
        public List<TempBuff> tempBuffs = new List<TempBuff>();

        // --- 편성 스냅샷 ---
        /// <summary>
        /// MainHub에서 '값 복사'로 전달된 편성. Gameplay에서 원본을 참조하지 않는다.
        /// 임시 강화가 원본 CharacterData/SaveData를 오염시키는 것을 막기 위함. 기획서 12.6
        /// </summary>
        public PartySnapshot party = new PartySnapshot();
    }

    /// <summary>메인 1명 + 패시브 3명. 기획서 7.2</summary>
    [Serializable]
    public class PartySnapshot
    {
        public CharacterSnapshot main;
        public List<CharacterSnapshot> passives = new List<CharacterSnapshot>();
    }

    /// <summary>
    /// 레벨·스킬 강화가 이미 반영된 최종 스탯 사본.
    /// 메인 캐릭터의 스탯만 전투·스테이지 기본 판정에 사용한다.
    /// 패시브 캐릭터의 스탯은 자신의 스킬·패시브 효과량 계산에만 사용한다. 기획서 6.1, 7.1
    /// </summary>
    [Serializable]
    public class CharacterSnapshot
    {
        public string characterId;

        /// <summary>편성 시 선택한 대표 스킬 1개. 기획서 6.5</summary>
        public string skillId;

        public int level;

        public int maxHp;
        public int physicalAttack;
        public int magicAttack;
        public int healPower;
    }

    /// <summary>스테이지 임시 강화 1건. 기획서 8.4</summary>
    [Serializable]
    public class TempBuff
    {
        public string buffId;
        public int value;

        /// <summary>남은 전투 수. -1이면 스테이지 종료까지 유지.</summary>
        public int remainingBattles = -1;
    }
}
