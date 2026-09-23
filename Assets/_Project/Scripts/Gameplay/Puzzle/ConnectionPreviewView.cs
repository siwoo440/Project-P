using System.Collections.Generic;
using ProjectP.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 연결 효과 실시간 표시(9일차, 사용자 요청). 드래그하는 동안 지금 경로를 놓으면 받을 효과를 한 칸에 모아 보여준다.
    ///
    /// - 마지막으로 이은 보석과 그 보석이 받는 연속 배율(×1.0 / ×1.2 …), "물리 3연속"
    /// - 다음 한 개를 더 이으면: 다음 배율, 연속 강화·특수 보석까지 남은 개수
    /// - 지금까지의 합계: 피해 · 회복 · 지연 · 다음 턴 행동력, 발동한 연속 강화·만들어질 특수 보석
    /// 계산은 PuzzleManager.PreviewEffects(= 실제 확정과 같은 GemEffectResolver)를 읽기만 한다.
    /// </summary>
    public class ConnectionPreviewView : MonoBehaviour
    {
        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private GameObject idleGroup;
        [SerializeField] private GameObject activeGroup;

        [Header("마지막 보석")]
        [SerializeField] private Image gemTile;
        [SerializeField] private Image gemIcon;
        [SerializeField] private GameObject gemSpecialRing;
        [SerializeField] private TMP_Text multiplierText;
        [SerializeField] private TMP_Text chainText;
        [SerializeField] private TMP_Text nextText;

        [Header("합계")]
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text healText;
        [SerializeField] private TMP_Text delayText;
        [SerializeField] private TMP_Text actionPointText;
        [SerializeField] private TMP_Text bonusText;

        [Header("색")]
        [SerializeField] private Color baseColor = new Color32(168, 175, 199, 255);
        [SerializeField] private Color bonusColor = new Color32(242, 193, 78, 255);
        [SerializeField] private Color comboColor = new Color32(79, 195, 247, 255);

        private void OnEnable()
        {
            puzzle.ConnectionChanged += OnChanged;
            puzzle.ConnectionConfirmed += OnEnded;
            puzzle.ConnectionWasted += OnEnded;
            puzzle.ConnectionCanceled += ShowIdle;
            puzzle.BoardChanged += OnBoardChanged;
            ShowIdle();
        }

        private void OnDisable()
        {
            puzzle.ConnectionChanged -= OnChanged;
            puzzle.ConnectionConfirmed -= OnEnded;
            puzzle.ConnectionWasted -= OnEnded;
            puzzle.ConnectionCanceled -= ShowIdle;
            puzzle.BoardChanged -= OnBoardChanged;
        }

        private void OnEnded(IReadOnlyList<BoardPosition> positions) => ShowIdle();

        private void OnBoardChanged(Board board) => ShowIdle();

        private void ShowIdle()
        {
            idleGroup.SetActive(true);
            activeGroup.SetActive(false);
        }

        private void OnChanged(IReadOnlyList<BoardPosition> positions)
        {
            var preview = puzzle.PreviewEffects();
            var chain = puzzle.Chain;
            if (preview == null || chain == null || positions.Count == 0)
            {
                ShowIdle();
                return;
            }

            idleGroup.SetActive(false);
            activeGroup.SetActive(true);

            var last = positions[positions.Count - 1];
            var gem = puzzle.GetGem(last);
            var type = puzzle.Board[last];
            var isSpecial = puzzle.Board.IsSpecial(last);
            var name = gem != null ? gem.DisplayName : type.ToString();

            ShowGem(gem, isSpecial);

            if (isSpecial)
            {
                SetText(multiplierText, "특수", bonusColor);
                chainText.text = $"{name} 특수 보석";
                nextText.text = SpecialDescription(type, chain);
            }
            else
            {
                var run = RunLengthAtEnd(positions);
                var multiplier = preview.ChainMultipliers[positions.Count - 1];
                SetText(multiplierText, $"×{multiplier:0.0}", run >= chain.ComboLength ? comboColor : multiplier > 1.001f ? bonusColor : baseColor);
                chainText.text = run >= 2 ? $"{name} {run}연속" : $"{name} 1개";
                nextText.text = NextHint(name, type, run, chain, positions.Count >= puzzle.ActionPointLimit);
            }

            damageText.text = preview.TotalDamage.ToString();
            healText.text = preview.Heal.ToString();
            delayText.text = $"+{preview.Delay}";
            actionPointText.text = $"+{preview.NextTurnActionPoints}";
            bonusText.text = BonusLine(preview);
        }

        private void ShowGem(GemData gem, bool isSpecial)
        {
            gemTile.color = gem != null ? gem.Color : Color.magenta;
            gemIcon.sprite = gem != null ? gem.Icon : null;
            gemIcon.color = gem != null ? gem.IconColor : Color.white;
            gemIcon.enabled = gemIcon.sprite != null;
            gemSpecialRing.SetActive(isSpecial);
        }

        private static void SetText(TMP_Text text, string value, Color color)
        {
            text.text = value;
            text.color = color;
        }

        /// <summary>경로 끝에서 거꾸로 센, 같은 종류 일반 보석이 이어진 개수.</summary>
        private int RunLengthAtEnd(IReadOnlyList<BoardPosition> positions)
        {
            var board = puzzle.Board;
            var last = positions[positions.Count - 1];
            var run = 0;
            for (var i = positions.Count - 1; i >= 0; i--)
            {
                if (board.IsSpecial(positions[i]) || board[positions[i]] != board[last]) break;
                run++;
            }

            return run;
        }

        private static string NextHint(string name, GemType type, int run, ChainSettings chain, bool full)
        {
            if (run >= chain.SpecialLength) return "<b>특수 보석 생성!</b> (경로 마지막 칸)";

            var combo = ComboName(type);
            if (run >= chain.ComboLength)
            {
                return full
                    ? $"<b>{combo} 발동!</b>"
                    : $"<b>{combo} 발동!</b> · 특수 보석까지 {chain.SpecialLength - run}개";
            }

            if (full) return "행동력을 모두 썼습니다";

            var next = 1f + chain.BonusPerGem * run;
            return $"다음 {name} ×{next:0.0} · {combo}까지 {chain.ComboLength - run}개";
        }

        private static string BonusLine(EffectSummary preview)
        {
            var parts = new List<string>();
            foreach (var effect in preview.Events)
            {
                if (effect.Source == EffectSource.Combo) parts.Add(ComboName(effect.Gem));
            }

            if (preview.CreatedSpecial.HasValue) parts.Add("특수 보석 생성");
            return parts.Count > 0 ? string.Join(" · ", parts) : "연속 보너스 없음";
        }

        /// <summary>4연속 연속 강화 이름. 기획서 5.6</summary>
        public static string ComboName(GemType type)
        {
            switch (type)
            {
                case GemType.Physical:
                case GemType.Magic: return "강공격";
                case GemType.Heal: return "추가 회복";
                case GemType.Chaos: return "지연 강화";
                case GemType.Balance: return "행동력 +1";
                default: return "연속 강화";
            }
        }

        /// <summary>특수 보석 효과 설명. 기획서 5.6</summary>
        public static string SpecialDescription(GemType type, ChainSettings chain)
        {
            var power = chain.SpecialPower(type);
            switch (type)
            {
                case GemType.Physical: return $"한 적에게 물리 ×{power:0.#}";
                case GemType.Magic: return $"모든 적에게 마법 ×{power:0.#}";
                case GemType.Heal: return $"회복력 ×{power:0.#} 회복";
                case GemType.Chaos: return $"모든 적 행동 +{power:0}";
                case GemType.Balance: return $"다음 턴 행동력 +{power:0}";
                default: return "효과 없음";
            }
        }
    }
}
