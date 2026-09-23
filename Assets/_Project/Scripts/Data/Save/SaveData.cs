using System;
using System.Collections.Generic;

namespace ProjectP.Data
{
    /// <summary>
    /// 디스크에 영구 저장되는 데이터. SaveManager만 읽고 쓴다.
    ///
    /// [저장 시점] 기획서 11.1
    ///   1. 스테이지 시작
    ///   2. 방 이동 완료 (= 방 진입 직후, 방 내용 처리 전)
    ///   3. 스테이지 클리어
    ///   4. 캐릭터 성장 변경
    /// 전투 중간 상태는 저장하지 않는다.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>저장 포맷 버전. 필드 구조가 바뀌면 올리고 마이그레이션한다.</summary>
        public int saveVersion = 1;

        // --- 영구 성장. 기획서 7.3 ---
        public List<CharacterProgress> characters = new List<CharacterProgress>();

        /// <summary>스킬 강화에 사용하는 영구 성장 재화. 스테이지 임시 재화와 분리한다.</summary>
        public int permanentCurrency;

        // --- 진행도 ---
        public List<string> clearedStageIds = new List<string>();

        /// <summary>캐릭터 해금은 스토리·챕터 진행으로만. 가챠·랜덤 해금 없음. 기획서 7.3</summary>
        public List<string> unlockedCharacterIds = new List<string>();

        public int currentChapter;

        // --- 이어하기용 스냅샷 ---
        /// <summary>
        /// 진행 중인 스테이지가 있는지 여부. 이어하기 판정은 반드시 이 값으로 한다.
        ///
        /// suspendedSession의 null로 판정하면 안 된다. JsonUtility(Unity 직렬화)는
        /// 커스텀 클래스 필드의 null을 보존하지 못하고, 불러올 때 빈 객체로 채운다.
        /// </summary>
        public bool hasSuspendedSession;

        /// <summary>
        /// 방 진입 직전 시점의 SessionData 사본.
        /// 게임 재실행 시 이 지점부터 재개한다. 기획서 11.1, 12.8
        /// hasSuspendedSession이 false면 내용을 무시한다.
        ///
        /// 주의: 패배 재도전(기획서 6.6)도 같은 스냅샷을 되감아 처리한다.
        ///       재도전용 사본은 Gameplay가 메모리에도 들고 있어야 한다.
        /// </summary>
        public SessionData suspendedSession;
    }

    /// <summary>캐릭터 1명의 영구 성장 상태. 기획서 7.3</summary>
    [Serializable]
    public class CharacterProgress
    {
        public string characterId;
        public int level;
        public int exp;

        public List<SkillProgress> skills = new List<SkillProgress>();

        /// <summary>전투에 사용할 대표 스킬 1개. Character Panel에서 설정한다. 기획서 12.5</summary>
        public string selectedSkillId;
    }

    /// <summary>스킬 1개의 강화 상태. 기획서 7.3</summary>
    [Serializable]
    public class SkillProgress
    {
        public string skillId;
        public int enhanceLevel;
    }
}
