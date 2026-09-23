using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보드 마우스 입력. 기획서 5.3 / 9.6
    ///
    /// - 왼쪽 버튼을 누른 채 드래그해 경로를 만들고, 놓는 순간 확정한다(보드 밖에서 놓아도 확정).
    /// - 드래그 중 우클릭 또는 ESC로 취소한다.
    /// - 칸마다 버튼을 두지 않고 보드 영역 하나에서 마우스 위치를 계산해 칸을 찾는다.
    ///   칸 중심의 hitRatio 원 안에 들어가야 선택되므로 대각선으로 그을 때 옆 칸이 잘못 잡히지 않는다.
    /// 이 오브젝트에는 입력을 받을 투명 Graphic(raycastTarget)이 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BoardInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private BoardView boardView;
        [SerializeField, Range(0.5f, 1f)] private float hitRatio = 0.8f;

        private RectTransform rect;
        private bool dragging;
        private Vector2 lastPoint;

        private void Awake() => rect = (RectTransform)transform;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || boardView.Layout == null) return;
            if (!TryGetBoardPoint(eventData, out var point)) return;
            if (!boardView.Layout.TryHit(point.x, point.y, hitRatio, out var position)) return;

            dragging = puzzle.BeginConnection(position);
            lastPoint = point;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || eventData.button != PointerEventData.InputButton.Left) return;
            if (!TryGetBoardPoint(eventData, out var point)) return;

            // 한 프레임에 여러 칸을 건너뛰어도 지나간 칸을 순서대로 처리한다.
            foreach (var position in boardView.Layout.HitsAlong(lastPoint.x, lastPoint.y, point.x, point.y, hitRatio))
            {
                puzzle.ExtendConnection(position);
            }

            lastPoint = point;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!dragging || eventData.button != PointerEventData.InputButton.Left) return;

            dragging = false;
            puzzle.EndConnection();
        }

        private void Update()
        {
            if (!dragging) return;

            var rightClicked = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            var escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (!rightClicked && !escapePressed) return;

            dragging = false;
            puzzle.CancelConnection();
        }

        private void OnDisable()
        {
            if (!dragging) return;

            dragging = false;
            puzzle.CancelConnection();
        }

        /// <summary>화면 좌표를 보드 중심 기준 좌표(BoardLayout과 같은 기준)로 바꾼다.</summary>
        private bool TryGetBoardPoint(PointerEventData eventData, out Vector2 point)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local))
            {
                point = default;
                return false;
            }

            point = local - rect.rect.center;
            return true;
        }
    }
}
