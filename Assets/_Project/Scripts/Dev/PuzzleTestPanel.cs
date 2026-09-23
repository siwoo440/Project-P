using ProjectP.Data;
using ProjectP.Gameplay.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.Dev
{
    /// <summary>
    /// Dev_PuzzleTest 전용 제어 화면. 기획서 12.15
    /// 실제 PuzzleManager를 그대로 쓰고, 보드 재생성·시드 지정·보석 분포 표시만 추가한다.
    /// </summary>
    public class PuzzleTestPanel : MonoBehaviour
    {
        [SerializeField] private PuzzleManager puzzle;
        [SerializeField] private TMP_Text seedText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_InputField seedInput;
        [SerializeField] private Button regenerateButton;
        [SerializeField] private Button seedButton;
        [SerializeField] private GemType[] statTypes;
        [SerializeField] private TMP_Text[] statTexts;

        private void OnEnable() => puzzle.BoardChanged += Refresh;

        private void OnDisable() => puzzle.BoardChanged -= Refresh;

        private void Start()
        {
            regenerateButton.onClick.AddListener(() => puzzle.Regenerate());
            seedButton.onClick.AddListener(RegenerateWithInputSeed);
            seedInput.onSubmit.AddListener(_ => RegenerateWithInputSeed());

            if (puzzle.Board != null) Refresh(puzzle.Board);
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
    }
}
