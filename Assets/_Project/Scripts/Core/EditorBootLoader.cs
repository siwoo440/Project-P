using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ProjectP.Core
{
    /// <summary>
    /// 에디터 전용: 어느 Scene에서 Play를 눌러도 00_Boot를 먼저 실행한 뒤 원래 Scene으로 돌아온다.
    ///
    /// 00_Boot를 거치지 않으면 전역 서비스가 없어 Dev_PuzzleTest 등에서 바로 테스트할 수 없기 때문이다.
    /// DontDestroyOnLoad는 여전히 00_Boot 안에서만 호출되므로 CLAUDE.md 3장 규칙을 지킨다.
    /// 빌드에서는 00_Boot가 항상 첫 Scene이므로 이 동작이 컴파일되지 않는다.
    ///
    /// 주의: 원래 Scene은 디스크에서 다시 불러오므로, 저장하지 않은 Scene 변경은 Play에 반영되지 않는다.
    /// </summary>
    public static class EditorBootLoader
    {
#if UNITY_EDITOR
        private static string returnScenePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootFirst()
        {
            returnScenePath = null;

            var active = SceneManager.GetActiveScene();
            if (active.name == SceneNames.Boot) return;

            returnScenePath = active.path;

            // 원래 Scene의 오브젝트가 서비스 없이 Awake·Start를 실행하지 않도록 먼저 비활성화한다.
            if (active.isLoaded)
            {
                foreach (var root in active.GetRootGameObjects()) root.SetActive(false);
            }

            SceneManager.LoadScene(SceneNames.Boot);
        }
#endif

        /// <summary>
        /// Boot 완료 후 돌아갈 Scene이 있으면 불러오고 true를 반환한다.
        /// 빌드에서는 항상 false.
        /// </summary>
        public static bool TryLoadReturnScene()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(returnScenePath)) return false;

            var path = returnScenePath;
            returnScenePath = null;

            // Dev Scene은 빌드 목록에 없으므로 경로로 직접 불러온다.
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return true;
#else
            return false;
#endif
        }
    }
}
