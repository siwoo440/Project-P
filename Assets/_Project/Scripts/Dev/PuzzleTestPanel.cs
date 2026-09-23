using System.Collections.Generic;
using System.Text;
using ProjectP.Data;
using ProjectP.Gameplay.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Dev
{
    /// <summary>
    /// Dev_PuzzleTest 전용 제어 화면. 기획서 12.15
    /// 실제 PuzzleManager를 그대로 쓰고, 보드 재생성·시드·보석 분포·연결 기록·턴·행동력 조정만 추가한다.
    /// </summary>
    public class PuzzleTestPanel : MonoBehaviour
    {
        private const int MaxLogEntries = 5;
        private const string IdleStatus = "드래그로 보석을 이어 보세요";

        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private TMP_Text seedText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_InputField seedInput;
        [SerializeField] private Button regenerateButton;
        [SerializeField] private Button seedButton;
        [SerializeField] private GemType[] statTypes;
        [SerializeField] private TMP_Text[] statTexts;
        [SerializeField] private TMP_Text connectionStatusText;
        [SerializeField] private TMP_Text connectionLogText;

        [Header("턴·행동력")]
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private Button nextTurnButton;
        [SerializeField] private Toggle autoTurnToggle;
        [SerializeField] private Button actionPointMinusButton;
        [SerializeField] private Button actionPointPlusButton;
        [SerializeField] private TMP_Text actionPointBaseText;

        private readonly LinkedList<string> log = new LinkedList<string>();

        private void OnEnable()
        {
            puzzle.BoardChanged += Refresh;
            puzzle.TurnStateChanged += RefreshTurn;
            puzzle.ConnectionChanged += OnConnectionChanged;
            puzzle.ConnectionConfirmed += OnConnectionConfirmed;
            puzzle.ConnectionWasted += OnConnectionWasted;
            puzzle.ConnectionCanceled += OnConnectionCanceled;
        }

        private void OnDisable()
        {
            puzzle.BoardChanged -= Refresh;
            puzzle.TurnStateChanged -= RefreshTurn;
            puzzle.ConnectionChanged -= OnConnectionChanged;
            puzzle.ConnectionConfirmed -= OnConnectionConfirmed;
            puzzle.ConnectionWasted -= OnConnectionWasted;
            puzzle.ConnectionCanceled -= OnConnectionCanceled;
        }

        private void Start()
        {
            regenerateButton.onClick.AddListener(() => puzzle.Regenerate());
            seedButton.onClick.AddListener(RegenerateWithInputSeed);
            seedInput.onSubmit.AddListener(_ => RegenerateWithInputSeed());

            nextTurnButton.onClick.AddListener(puzzle.StartTurn);
            autoTurnToggle.onValueChanged.AddListener(isOn => puzzle.AutoNextTurn = isOn);
            actionPointMinusButton.onClick.AddListener(() => puzzle.SetBaseActionPoints(puzzle.BaseActionPoints - 1));
            actionPointPlusButton.onClick.AddListener(() => puzzle.SetBaseActionPoints(puzzle.BaseActionPoints + 1));

            connectionStatusText.text = IdleStatus;
            connectionLogText.text = "";
            if (puzzle.Board != null) Refresh(puzzle.Board);
            RefreshTurn();
        }

        private void RegenerateWithInputSeed()
        {
            if (int.TryParse(seedInput.text, out var seed)) puzzle.Regenerate(seed);
            else Debug.LogWarning($"[PuzzleTestPanel] 시드는 정수여야 합니다: '{seedInput.text}'");
        }

        private void Refresh(Board board)
        {
            seedText.text = puzzle.Seed.ToString();
            summaryText.text = $"{board.Rows} × {board.Columns} · {board.CellCount}칸";

            for (var i = 0; i < statTypes.Length && i < statTexts.Length; i++)
            {
                var count = board.Count(statTypes[i]);
                statTexts[i].text = $"{count}개 · {count * 100f / board.CellCount:0}%";
            }
        }

        private void RefreshTurn()
        {
            var state = puzzle.IsResolving ? "보석 정리 중"
                : puzzle.CanAct ? "연결 가능"
                : "다음 턴 대기";
            turnText.text = $"턴 {puzzle.TurnNumber}  ·  {state}";

            actionPointBaseText.text = puzzle.BaseActionPoints.ToString();
            actionPointMinusButton.interactable = puzzle.BaseActionPoints > 1;
            nextTurnButton.interactable = !puzzle.CanAct && !puzzle.IsResolving;
            autoTurnToggle.SetIsOnWithoutNotify(puzzle.AutoNextTurn);
        }

        private void OnConnectionChanged(IReadOnlyList<BoardPosition> positions) =>
            connectionStatusText.text = $"연결 중 · {positions.Count}개";

        private void OnConnectionCanceled() => connectionStatusText.text = "취소됨 · 턴 유지";

        private void OnConnectionConfirmed(IReadOnlyList<BoardPosition> positions)
        {
            connectionStatusText.text = IdleStatus;
            AddLog($"{Describe(positions)}  <color=#F2C14E>{positions.Count}개 사용</color>");
        }

        private void OnConnectionWasted(IReadOnlyList<BoardPosition> positions)
        {
            connectionStatusText.text = IdleStatus;
            AddLog($"{Describe(positions)}  <color=#E5484D>{positions.Count}개 · 턴 소모</color>");
        }

        private void AddLog(string entry)
        {
            log.AddFirst($"<color=#A8AFC7>턴 {puzzle.TurnNumber}</color>  {entry}");
            while (log.Count > MaxLogEntries) log.RemoveLast();
            connectionLogText.text = string.Join("\n", log);
        }

        /// <summary>경로를 보석 이름 첫 글자로 보여준다. 글자 색은 보석 색.</summary>
        private string Describe(IReadOnlyList<BoardPosition> positions)
        {
            var builder = new StringBuilder();
            foreach (var position in positions)
            {
                var gem = puzzle.GetGem(position);
                var letter = gem != null && !string.IsNullOrEmpty(gem.DisplayName) ? gem.DisplayName.Substring(0, 1) : "?";
                var color = gem != null ? ColorUtility.ToHtmlStringRGB(gem.Color) : "FF00FF";
                builder.Append($"<color=#{color}>{letter}</color>");
            }

            return builder.ToString();
        }
    }
}
