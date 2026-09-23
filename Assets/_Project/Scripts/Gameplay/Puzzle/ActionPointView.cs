using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 행동력 표시. 기획서 9.5 — 남은 행동력을 보드 주변에서 즉시 확인할 수 있게 한다.
    ///
    /// 칩 하나 = 행동력 1. 왼쪽부터 남은 몫(금색), 이번 경로가 쓸 몫(흐림), 보너스로 늘어난 몫(청록).
    /// 턴을 쓰고 다음 턴을 기다리는 동안에는 모두 어둡게 표시한다.
    /// 03_Gameplay HUD에서도 같은 부품을 쓴다.
    /// </summary>
    public class ActionPointView : MonoBehaviour
    {
        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private RectTransform chipRoot;
        [SerializeField] private Sprite chipSprite;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text bonusText;
        [SerializeField] private float chipSize = 22f;
        [SerializeField] private Color availableColor = new Color32(242, 193, 78, 255);
        [SerializeField] private Color bonusColor = new Color32(79, 195, 247, 255);
        [SerializeField] private Color previewColor = new Color32(242, 193, 78, 70);
        [SerializeField] private Color spentColor = new Color32(66, 76, 115, 255);

        private readonly List<Image> chips = new List<Image>();
        private int pathLength;

        private void OnEnable()
        {
            puzzle.TurnStateChanged += Refresh;
            puzzle.ConnectionChanged += OnConnectionChanged;
            puzzle.ConnectionConfirmed += OnConnectionEnded;
            puzzle.ConnectionWasted += OnConnectionEnded;
            puzzle.ConnectionCanceled += OnConnectionCanceled;
            Refresh();
        }

        private void OnDisable()
        {
            puzzle.TurnStateChanged -= Refresh;
            puzzle.ConnectionChanged -= OnConnectionChanged;
            puzzle.ConnectionConfirmed -= OnConnectionEnded;
            puzzle.ConnectionWasted -= OnConnectionEnded;
            puzzle.ConnectionCanceled -= OnConnectionCanceled;
        }

        private void OnConnectionChanged(IReadOnlyList<BoardPosition> positions)
        {
            pathLength = positions.Count;
            Refresh();
        }

        private void OnConnectionEnded(IReadOnlyList<BoardPosition> positions)
        {
            pathLength = 0;
            Refresh();
        }

        private void OnConnectionCanceled()
        {
            pathLength = 0;
            Refresh();
        }

        private void Refresh()
        {
            var limit = puzzle.ActionPointLimit;
            var bonus = puzzle.ActionPointBonus;
            var spent = !puzzle.CanAct;
            var remaining = Mathf.Max(0, limit - pathLength);

            EnsureChips(limit);
            for (var i = 0; i < limit; i++)
            {
                chips[i].color = spent ? spentColor
                    : i >= remaining ? previewColor
                    : i >= limit - bonus ? bonusColor
                    : availableColor;
            }

            valueText.text = spent ? "사용함" : $"{remaining} / {limit}";

            var next = puzzle.NextTurnBonus;
            bonusText.gameObject.SetActive(next > 0);
            bonusText.text = $"다음 턴 +{next}";
        }

        private void EnsureChips(int count)
        {
            while (chips.Count < count)
            {
                var go = new GameObject($"Chip {chips.Count}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(chipRoot, false);
                var element = go.GetComponent<LayoutElement>();
                element.preferredWidth = chipSize;
                element.preferredHeight = chipSize;

                var image = go.GetComponent<Image>();
                image.sprite = chipSprite;
                image.raycastTarget = false;
                chips.Add(image);
            }

            for (var i = 0; i < chips.Count; i++) chips[i].gameObject.SetActive(i < count);
        }
    }
}
