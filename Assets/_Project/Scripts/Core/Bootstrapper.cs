using System;
using ProjectP.Data;
using ProjectP.UI;
using UnityEngine;

namespace ProjectP.Core
{
    /// <summary>
    /// 00_Boot의 유일한 진입점. 기획서 12.2 / CLAUDE.md 3장
    ///
    /// - 전역 서비스를 정해진 순서로 생성·초기화하고 GameServices에 등록한다.
    /// - DontDestroyOnLoad를 호출하는 유일한 지점이다.
    /// - 초기화에 실패하면 오류를 표시하고 진행을 멈춘다.
    /// - 전투·편성·스테이지 로직을 넣지 않는다.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;

        [Tooltip("Boot 화면. 비어 있으면 임시 글자 표시(OnGUI)로 대신한다.")]
        [SerializeField] private BootScreenView view;

        private string status = "시작";
        private string error;

        private void Start()
        {
            // 서비스가 이미 있으면(Boot 재진입) 새로 만들지 않고 타이틀로 보낸다.
            if (GameServices.IsReady)
            {
                GameServices.Scenes.Load(SceneNames.Title);
                return;
            }

            GameObject servicesRoot = null;
            try
            {
                servicesRoot = new GameObject("[GameServices]");
                DontDestroyOnLoad(servicesRoot);

                // 순서가 중요하다. 뒤 단계가 앞 단계의 결과를 사용한다.
                Report(0.1f, "데이터 불러오기");
                var data = servicesRoot.AddComponent<DataManager>();
                data.Initialize(database);

                Report(0.35f, "저장 데이터 확인");
                var save = servicesRoot.AddComponent<SaveManager>();
                save.Initialize();

                Report(0.6f, "사운드 준비"); // 설정의 볼륨을 쓰므로 SaveManager 다음이다.
                var audio = servicesRoot.AddComponent<AudioManager>();
                audio.Initialize(save.Settings);

                Report(0.85f, "서비스 등록");
                var game = servicesRoot.AddComponent<GameManager>();
                var scenes = servicesRoot.AddComponent<SceneFlow>();
                GameServices.Register(data, save, audio, game, scenes);
            }
            catch (Exception e)
            {
                if (servicesRoot != null) Destroy(servicesRoot);
                error = $"초기화 실패 — {status}\n{e.Message}";
                if (view != null) view.ShowError(error);
                Debug.LogException(e);
                return;
            }

            Report(1f, "완료");
            if (!EditorBootLoader.TryLoadReturnScene())
            {
                GameServices.Scenes.Load(SceneNames.Title);
            }
        }

        private void Report(float progress, string newStatus)
        {
            status = newStatus;
            if (view != null) view.ShowProgress(progress, $"{newStatus}...");
        }

        // Boot 화면(view)이 연결되지 않았을 때만 쓰는 임시 표시.
        private void OnGUI()
        {
            if (view != null) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(16, Screen.height / 30),
                wordWrap = true
            };
            style.normal.textColor = error == null ? Color.white : new Color(1f, 0.4f, 0.4f);

            var message = error ?? $"{status}...";
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), message, style);
        }
    }
}
