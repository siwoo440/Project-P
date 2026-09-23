using System;
using System.Collections;
using ProjectP.Data;
using ProjectP.Gameplay.Battle;
using ProjectP.Gameplay.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Dev
{
    /// <summary>
    /// Dev_PuzzleTest 전용 보석 효과 확인 화면. 전투(11일차~)가 없는 동안 효과 결과표를 허수아비·메인 HP에 적용해 눈으로 확인한다.
    ///
    /// - 허수아비는 여러 마리다. 클릭한 허수아비가 공격 대상이 된다(기획서 6.3).
    /// - 대상이 쓰러지면 살아 있는 허수아비 중 무작위로 대상을 바꾼다(사용자 결정). 규칙은 TargetSelection이 맡는다.
    /// - 효과는 연결 순서대로 하나씩, 짧은 간격을 두고 적용한다(기획서 5.5). 도중에 대상이 쓰러지면 남은 효과는 새 대상에게 간다.
    /// - 모든 적 대상 효과(마법·혼돈 특수 보석)는 살아 있는 허수아비 전원에게 준다. 연속 강화·특수 보석은 이름을 붙여 크게 띄운다.
    /// - 허수아비는 턴마다 행동 카운트가 1씩 줄고 0이 되면 "행동!" 후 기본값으로 돌아간다(기획서 6.4 흉내). 공격은 하지 않는다.
    /// - 이 컴포넌트가 가진 HP·카운트는 개발용 임시 상태다. 실제 게임에서는 BattleManager가 결과표를 적용한다.
    /// </summary>
    public class EffectTestPanel : MonoBehaviour
    {
        private const int StatStep = 1;
        private const int HurtAmount = 20;
        private const float NormalFloatSize = 34f;
        private const float BigFloatSize = 44f;

        [SerializeField] private PuzzleManager puzzle;

        [Header("메인 캐릭터 — 임시 스탯 (11일차 캐릭터 데이터 전까지)")]
        [SerializeField] private int physicalAttack = 10;
        [SerializeField] private int magicAttack = 10;
        [SerializeField] private int healPower = 8;
        [SerializeField] private int mainMaxHp = 100;
        [Tooltip("회복 효과가 보이도록 최대보다 낮게 시작한다.")]
        [SerializeField] private int mainStartHp = 60;
        [SerializeField] private RectTransform mainHpFill;
        [SerializeField] private TMP_Text mainHpText;
        [SerializeField] private RectTransform mainFloatAnchor;
        [Tooltip("0 물리 공격 · 1 마법 공격 · 2 회복력")]
        [SerializeField] private TMP_Text[] statTexts;
        [SerializeField] private Button[] statMinusButtons;
        [SerializeField] private Button[] statPlusButtons;
        [SerializeField] private Button hurtButton;

        [Header("허수아비 — 순번 0이 화면 왼쪽")]
        [SerializeField] private int dummyMaxHp = 150;
        [SerializeField] private int[] dummyActionCounts = { 2, 3, 4 };
        [SerializeField] private Button[] dummyButtons;
        [SerializeField] private RectTransform[] dummyHpFills;
        [SerializeField] private TMP_Text[] dummyHpTexts;
        [SerializeField] private TMP_Text[] dummyCountTexts;
        [SerializeField] private Image[] dummyBorders;
        [SerializeField] private GameObject[] dummyTargetMarks;
        [SerializeField] private CanvasGroup[] dummyGroups;
        [SerializeField] private Button reviveButton;
        [SerializeField] private Color targetBorderColor = new Color32(242, 193, 78, 255);
        [SerializeField] private Color normalBorderColor = new Color32(66, 76, 115, 255);

        [Header("떠오르는 숫자")]
        [SerializeField] private RectTransform floatRoot;
        [SerializeField] private Color damageColor = new Color32(255, 110, 110, 255);
        [SerializeField] private Color healColor = new Color32(80, 220, 150, 255);
        [SerializeField] private Color delayColor = new Color32(180, 150, 255, 255);
        [SerializeField] private Color actionPointColor = new Color32(79, 195, 247, 255);
        [SerializeField] private float eventInterval = 0.12f;

        private int[] stats;
        private int mainHp;
        private int[] dummyHp;
        private int[] dummyCount;
        private TargetSelection targets;

        private int DummyCount => dummyButtons.Length;

        private void OnEnable()
        {
            puzzle.EffectsResolved += OnEffectsResolved;
            puzzle.TurnStarted += OnTurnStarted;
        }

        private void OnDisable()
        {
            puzzle.EffectsResolved -= OnEffectsResolved;
            puzzle.TurnStarted -= OnTurnStarted;
        }

        private void Start()
        {
            stats = new[] { physicalAttack, magicAttack, healPower };
            mainHp = mainStartHp;

            dummyHp = new int[DummyCount];
            dummyCount = new int[DummyCount];
            targets = new TargetSelection(DummyCount, new System.Random(Environment.TickCount));
            for (var i = 0; i < DummyCount; i++)
            {
                var index = i;
                dummyButtons[i].onClick.AddListener(() => SelectTarget(index));
                dummyHp[i] = dummyMaxHp;
                dummyCount[i] = BaseCount(i);
            }

            for (var i = 0; i < stats.Length; i++)
            {
                var index = i;
                statMinusButtons[i].onClick.AddListener(() => ChangeStat(index, -StatStep));
                statPlusButtons[i].onClick.AddListener(() => ChangeStat(index, StatStep));
            }

            hurtButton.onClick.AddListener(() =>
            {
                mainHp = Mathf.Max(1, mainHp - HurtAmount);
                Float(mainFloatAnchor, $"-{HurtAmount}", damageColor);
                Refresh();
            });
            reviveButton.onClick.AddListener(ReviveAll);

            PushStats();
            Refresh();
        }

        private int BaseCount(int index) => dummyActionCounts.Length > 0 ? dummyActionCounts[index % dummyActionCounts.Length] : 3;

        private void SelectTarget(int index)
        {
            if (targets.Select(index)) Refresh();
        }

        private void ChangeStat(int index, int delta)
        {
            stats[index] = Mathf.Max(0, stats[index] + delta);
            PushStats();
        }

        private void PushStats()
        {
            puzzle.Stats = new CombatStats(stats[0], stats[1], stats[2]);
            for (var i = 0; i < statTexts.Length && i < stats.Length; i++) statTexts[i].text = stats[i].ToString();
        }

        private void ReviveAll()
        {
            for (var i = 0; i < DummyCount; i++)
            {
                dummyHp[i] = dummyMaxHp;
                dummyCount[i] = BaseCount(i);
                targets.Revive(i);
            }

            Refresh();
        }

        private void OnTurnStarted(int turnNumber)
        {
            // 턴 1은 새 보드를 만들 때 시작된다. 플레이어가 한 번 행동한 뒤부터 센다.
            if (turnNumber <= 1) return;

            for (var i = 0; i < DummyCount; i++)
            {
                if (!targets.IsAlive(i)) continue;

                dummyCount[i]--;
                if (dummyCount[i] > 0) continue;

                Float(dummyHpFills[i], "행동!", delayColor);
                dummyCount[i] = BaseCount(i);
            }

            Refresh();
        }

        private void OnEffectsResolved(EffectSummary summary) => StartCoroutine(PlayEffects(summary));

        private IEnumerator PlayEffects(EffectSummary summary)
        {
            foreach (var effect in summary.Events)
            {
                // 연속 강화·특수 보석은 이름을 붙여 크게 띄운다.
                var prefix = effect.Source == EffectSource.Combo
                    ? effect.Gem == GemType.Balance ? "4연속 " : $"{ConnectionPreviewView.ComboName(effect.Gem)} "
                    : effect.Source == EffectSource.Special ? "특수 "
                    : "";
                var size = effect.Source == EffectSource.Gem ? NormalFloatSize : BigFloatSize;

                switch (effect.Kind)
                {
                    case GemEffectKind.PhysicalDamage:
                    case GemEffectKind.MagicDamage:
                        if (effect.Target == EffectTarget.AllEnemies) HitAll(effect.Amount, prefix, size);
                        else HitTarget(effect.Amount, prefix, size);
                        break;

                    case GemEffectKind.Heal:
                        mainHp = Mathf.Min(mainMaxHp, mainHp + effect.Amount);
                        Float(mainFloatAnchor, $"{prefix}+{effect.Amount}", healColor, size);
                        break;

                    case GemEffectKind.Delay:
                        if (effect.Target == EffectTarget.AllEnemies) DelayAll(effect.Amount, prefix, size);
                        else DelayTarget(effect.Amount, prefix, size);
                        break;

                    case GemEffectKind.NextTurnActionPoints:
                        Float(mainFloatAnchor, effect.Amount > 0 ? $"{prefix}다음 턴 행동력 +{effect.Amount}" : "행동력 상한", actionPointColor, size);
                        break;
                }

                Refresh();
                yield return new WaitForSecondsRealtime(eventInterval);
            }
        }

        /// <summary>현재 대상에게 피해. 쓰러뜨리면 대상이 무작위로 바뀌고, 남은 효과는 새 대상에게 간다.</summary>
        private void HitTarget(int amount, string prefix, float size)
        {
            var target = targets.Current;
            if (target < 0)
            {
                Float(mainFloatAnchor, "대상 없음", damageColor);
                return;
            }

            Hit(target, amount, prefix, size);
        }

        /// <summary>살아 있는 모든 허수아비에게 피해(마법 특수 보석).</summary>
        private void HitAll(int amount, string prefix, float size)
        {
            var hitAny = false;
            for (var i = 0; i < DummyCount; i++)
            {
                if (!targets.IsAlive(i)) continue;

                Hit(i, amount, prefix, size);
                hitAny = true;
            }

            if (!hitAny) Float(mainFloatAnchor, "대상 없음", damageColor);
        }

        private void Hit(int index, int amount, string prefix, float size)
        {
            dummyHp[index] = Mathf.Max(0, dummyHp[index] - amount);
            Float(dummyHpFills[index], $"{prefix}{amount}", damageColor, size);

            if (dummyHp[index] > 0) return;

            Float(dummyHpFills[index], "처치!", damageColor);
            targets.MarkDefeated(index);
        }

        private void DelayTarget(int amount, string prefix, float size)
        {
            var target = targets.Current;
            if (target >= 0) Delay(target, amount, prefix, size);
        }

        /// <summary>살아 있는 모든 허수아비 행동 지연(혼돈 특수 보석).</summary>
        private void DelayAll(int amount, string prefix, float size)
        {
            for (var i = 0; i < DummyCount; i++)
            {
                if (targets.IsAlive(i)) Delay(i, amount, prefix, size);
            }
        }

        private void Delay(int index, int amount, string prefix, float size)
        {
            if (amount > 0)
            {
                dummyCount[index] += amount;
                Float(dummyHpFills[index], $"{prefix}지연 +{amount}", delayColor, size);
            }
            else
            {
                Float(dummyHpFills[index], "지연 상한", delayColor);
            }
        }

        private void Refresh()
        {
            SetBar(mainHpFill, (float)mainHp / mainMaxHp);
            mainHpText.text = $"{mainHp} / {mainMaxHp}";

            for (var i = 0; i < DummyCount; i++)
            {
                var alive = targets.IsAlive(i);
                var isTarget = targets.Current == i;

                SetBar(dummyHpFills[i], (float)dummyHp[i] / dummyMaxHp);
                dummyHpTexts[i].text = $"{dummyHp[i]} / {dummyMaxHp}";
                dummyCountTexts[i].text = alive ? $"행동까지 {dummyCount[i]}턴" : "처치됨";
                dummyBorders[i].color = isTarget ? targetBorderColor : normalBorderColor;
                dummyTargetMarks[i].SetActive(isTarget);
                dummyGroups[i].alpha = alive ? 1f : 0.4f;
                dummyButtons[i].interactable = alive;
            }
        }

        private static void SetBar(RectTransform fill, float ratio) => fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);

        /// <summary>anchor 위치에서 글자가 떠오르며 사라진다.</summary>
        private void Float(RectTransform anchor, string text, Color color, float size = NormalFloatSize)
        {
            var go = new GameObject("FloatText", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(floatRoot, false);
            rect.sizeDelta = new Vector2(360f, 60f);
            rect.position = anchor.position;
            rect.anchoredPosition += new Vector2(UnityEngine.Random.Range(-40f, 40f), 20f);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            StartCoroutine(Rise(rect, label));
        }

        private static IEnumerator Rise(RectTransform rect, TMP_Text label)
        {
            const float duration = 0.9f;
            var start = rect.anchoredPosition;
            var color = label.color;

            for (var time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                var t = time / duration;
                rect.anchoredPosition = start + new Vector2(0f, 70f * (1f - (1f - t) * (1f - t)));
                label.color = new Color(color.r, color.g, color.b, t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f);
                yield return null;
            }

            Destroy(rect.gameObject);
        }
    }
}
