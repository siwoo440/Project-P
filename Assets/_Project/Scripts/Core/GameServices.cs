using UnityEngine;

namespace ProjectP.Core
{
    /// <summary>
    /// 전역 서비스 접근 창구. CLAUDE.md 3장
    ///
    /// 등록은 Bootstrapper만 한다. 다른 코드는 GameServices.Save 처럼 읽기만 한다.
    /// 매니저가 각자 static Instance를 갖거나 DontDestroyOnLoad를 호출하지 않도록
    /// 모든 전역 접근을 이 클래스 한 곳으로 모은다.
    /// </summary>
    public static class GameServices
    {
        public static DataManager Data { get; private set; }
        public static SaveManager Save { get; private set; }
        public static AudioManager Audio { get; private set; }
        public static GameManager Game { get; private set; }
        public static SceneFlow Scenes { get; private set; }

        /// <summary>모든 서비스가 초기화를 마쳤는지 여부.</summary>
        public static bool IsReady { get; private set; }

        internal static void Register(DataManager data, SaveManager save, AudioManager audio, GameManager game, SceneFlow scenes)
        {
            Data = data;
            Save = save;
            Audio = audio;
            Game = game;
            Scenes = scenes;
            IsReady = true;
        }

        /// <summary>
        /// Play 모드 진입마다 정적 상태를 비운다.
        /// 도메인 리로드를 끈 설정에서도 이전 Play의 참조가 남지 않게 하기 위함이다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Data = null;
            Save = null;
            Audio = null;
            Game = null;
            Scenes = null;
            IsReady = false;
        }
    }
}
