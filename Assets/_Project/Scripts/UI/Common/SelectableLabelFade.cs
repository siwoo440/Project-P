using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.UI
{
    /// <summary>
    /// 버튼이 비활성일 때 글자도 흐리게 만든다.
    /// Unity 버튼은 배경(targetGraphic) 색만 바꾸므로 글자는 이 컴포넌트가 따로 처리한다.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class SelectableLabelFade : MonoBehaviour
    {
        [SerializeField] private Graphic label;
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.35f;

        private Selectable selectable;
        private bool? lastInteractable;

        private void Awake() => selectable = GetComponent<Selectable>();

        private void LateUpdate()
        {
            var interactable = selectable.IsInteractable();
            if (lastInteractable == interactable || label == null) return;

            lastInteractable = interactable;
            var color = label.color;
            color.a = interactable ? 1f : disabledAlpha;
            label.color = color;
        }
    }
}
