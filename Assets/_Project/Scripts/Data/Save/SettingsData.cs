using System;
using UnityEngine;

namespace ProjectP.Data
{
    /// <summary>
    /// 사용자 설정. 게임 진행(SaveData)과 별도 파일로 저장한다.
    /// 새 게임으로 진행을 초기화해도 설정은 유지되어야 하기 때문이다.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        /// <summary>설정 포맷 버전. 필드 구조가 바뀌면 올리고 마이그레이션한다.</summary>
        public int settingsVersion = 1;

        // 볼륨 기본값은 레거시 옵션표 참고값이다. 기획서에서 확정되면 다시 맞춘다.
        [Range(0, 100)] public int masterVolume = 80;
        [Range(0, 100)] public int bgmVolume = 70;
        [Range(0, 100)] public int sfxVolume = 80;
    }
}
