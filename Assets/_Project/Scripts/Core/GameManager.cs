using System;
using ProjectP.Data;
using UnityEngine;

namespace ProjectP.Core
{
    /// <summary>
    /// 전역 흐름 조율. 게임 규칙 로직을 넣지 않는다.
    ///
    /// Scene 간 SessionData 전달: MainHub가 만든 세션을 이 슬롯에 넣고, 03_Gameplay가 진입할 때 1회 꺼내 간다.
    /// GameManager는 전달만 할 뿐 세션 상태를 소유하지 않는다. 기획서 12.4, 12.7 / CLAUDE.md 2장
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private SessionData pendingSession;

        public bool HasPendingSession => pendingSession != null;

        public void SetPendingSession(SessionData session)
        {
            pendingSession = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>전달된 세션을 꺼낸다. 꺼낸 뒤 슬롯은 비운다. 없으면 null.</summary>
        public SessionData TakePendingSession()
        {
            var session = pendingSession;
            pendingSession = null;
            return session;
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
