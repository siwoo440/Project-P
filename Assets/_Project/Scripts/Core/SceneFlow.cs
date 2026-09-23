using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectP.Core
{
    /// <summary>
    /// Scene 전환 요청을 처리한다. 기획서 12.3
    /// UI 버튼은 SceneManager를 직접 호출하지 않고 이 서비스에 요청만 한다.
    /// 전환 중에 들어온 요청은 무시해 중복 로드를 막는다.
    /// </summary>
    public class SceneFlow : MonoBehaviour
    {
        public bool IsLoading { get; private set; }

        /// <summary>Scene 전환이 끝나면 새 Scene 이름과 함께 호출된다.</summary>
        public event Action<string> SceneLoaded;

        /// <summary>Scene 전환을 요청한다. 이미 전환 중이면 무시하고 false를 반환한다.</summary>
        public bool Load(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneFlow] 전환 중이라 요청을 무시합니다: {sceneName}");
                return false;
            }

            StartCoroutine(LoadRoutine(sceneName));
            return true;
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                // 빌드 목록에 없는 Scene이면 null이 반환된다.
                Debug.LogError($"[SceneFlow] Scene을 불러올 수 없습니다. 빌드 목록을 확인하세요: {sceneName}");
                IsLoading = false;
                yield break;
            }

            while (!operation.isDone) yield return null;

            IsLoading = false;
            SceneLoaded?.Invoke(sceneName);
        }
    }
}
