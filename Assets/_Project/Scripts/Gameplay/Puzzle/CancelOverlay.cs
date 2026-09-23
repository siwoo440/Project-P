using UnityEngine;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 취소 상태 표시. 드래그 중 포인터가 보이지 않는 취소 영역에 들어가면 화면을 살짝 붉게 물들인다.
    /// 화면 전체를 덮지만 입력은 막지 않는다. 부드럽게 나타나고 사라진다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CancelOverlay : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.12f;

        private CanvasGroup group;
        private float target;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public void SetVisible(bool visible) => target = visible ? 1f : 0f;

        private void Update()
        {
            if (Mathf.Approximately(group.alpha, target)) return;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / fadeDuration);
        }
    }
}
