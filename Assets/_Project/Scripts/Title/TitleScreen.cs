using System;
using ProjectP.Core;
using ProjectP.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Title
{
    /// <summary>
    /// 01_Title 화면. 기획서 12.3
    ///
    /// - 장기 게임 상태를 보유하지 않는다. 판정과 전환은 전역 서비스에 요청만 한다.
    /// - 이어하기는 저장 파일이 있을 때만 활성화한다.
    /// - 버튼을 누르는 순간 모든 버튼을 잠가 중복 입력을 막는다.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        private const string NewGameConfirmMessage = "기존 진행을 지우고 새로 시작할까요?";

        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private ConfirmDialog confirmDialog;
        [SerializeField] private SettingsPanel settingsPanel;

        private void Start()
        {
            confirmDialog.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);

            if (!GameServices.IsReady)
            {
                Debug.LogError("[TitleScreen] 전역 서비스가 없습니다. 00_Boot를 거쳤는지 확인하세요.");
                SetButtonsInteractable(false);
                return;
            }

            newGameButton.onClick.AddListener(OnNewGame);
            continueButton.onClick.AddListener(OnContinue);
            settingsButton.onClick.AddListener(settingsPanel.Open);
            quitButton.onClick.AddListener(OnQuit);

            SetButtonsInteractable(true);
        }

        private void OnNewGame()
        {
            if (GameServices.Save.HasSaveFile) confirmDialog.Show(NewGameConfirmMessage, StartNewGame);
            else StartNewGame();
        }

        private void StartNewGame()
        {
            SetButtonsInteractable(false);
            try
            {
                GameServices.Save.CreateNewSave();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                SetButtonsInteractable(true);
                return;
            }

            GameServices.Scenes.Load(SceneNames.MainHub);
        }

        private void OnContinue()
        {
            SetButtonsInteractable(false);
            GameServices.Scenes.Load(SceneNames.MainHub);
        }

        private void OnQuit()
        {
            SetButtonsInteractable(false);
            GameServices.Game.QuitGame();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            newGameButton.interactable = interactable;
            continueButton.interactable = interactable && GameServices.Save.HasSaveFile;
            settingsButton.interactable = interactable;
            quitButton.interactable = interactable;
        }
    }
}
