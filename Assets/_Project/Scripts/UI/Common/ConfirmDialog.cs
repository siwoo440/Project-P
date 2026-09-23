using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.UI
{
    /// <summary>
    /// 예/아니오 확인창. 여러 화면에서 재사용한다.
    /// 전체 화면 배경이 클릭을 받아, 열려 있는 동안 뒤쪽 버튼은 눌리지 않는다.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action onYes;
        private Action onNo;

        private void Awake()
        {
            yesButton.onClick.AddListener(() => Close(onYes));
            noButton.onClick.AddListener(() => Close(onNo));
        }

        public void Show(string message, Action onYes, Action onNo = null)
        {
            messageText.text = message;
            this.onYes = onYes;
            this.onNo = onNo;
            gameObject.SetActive(true);
        }

        private void Close(Action callback)
        {
            gameObject.SetActive(false);
            onYes = null;
            onNo = null;
            callback?.Invoke();
        }
    }
}
