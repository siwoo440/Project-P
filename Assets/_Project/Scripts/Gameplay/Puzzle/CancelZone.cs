using UnityEngine;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보이지 않는 연결 취소 영역. 드래그 중 여기에서 손을 떼면 턴을 쓰지 않고 취소한다(사용자 결정).
    ///
    /// 영역 자체는 그리지 않는다. 포인터가 들어오면 BoardInput이 CancelOverlay로 화면을 살짝 붉게 물들여
    /// 지금 놓으면 취소된다는 것을 알린다. 위치 판정만 한다(클릭을 막지 않는다).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CancelZone : MonoBehaviour
    {
        public bool Contains(Vector2 screenPoint, Camera eventCamera) =>
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPoint, eventCamera);
    }
}
