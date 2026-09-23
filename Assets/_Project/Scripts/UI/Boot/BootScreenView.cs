using TMPro;
using UnityEngine;

namespace ProjectP.UI
{
    /// <summary>
    /// 00_Boot 화면 표시. 기획서 12.2 — 로고, 진행 상태, 로딩 표시만 제공한다.
    /// 초기화 로직은 Bootstrapper가 담당하고, 이 컴포넌트는 표시만 한다.
    /// </summary>
    public class BootScreenView : MonoBehaviour
    {
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private TMP_Text errorText;

        private void Awake() => errorPanel.SetActive(false);

        public void ShowProgress(float progress, string status)
        {
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            statusText.text = status;
        }

        public void ShowError(string message)
        {
            statusText.text = "초기화 실패";
            errorText.text = message;
            errorPanel.SetActive(true);
        }
    }
}
