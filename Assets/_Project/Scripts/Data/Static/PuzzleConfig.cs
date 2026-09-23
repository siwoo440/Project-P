using UnityEngine;

namespace ProjectP.Data
{
    /// <summary>
    /// 퍼즐 수치 모음. 밸런스 조정 시 이 에셋만 고친다.
    /// 행동력·최소 연결 수 등은 6~7일차에 추가한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PuzzleConfig", menuName = "Project P/Puzzle Config")]
    public class PuzzleConfig : ScriptableObject
    {
        [Tooltip("보드 행 수. 기획서 5.1은 4행이나 2026-09-23 결정으로 6행을 쓴다.")]
        [Min(1)] [SerializeField] private int rows = 6;

        [Tooltip("보드 열 수. 기획서 5.1")]
        [Min(1)] [SerializeField] private int columns = 12;

        public int Rows => rows;
        public int Columns => columns;
    }
}
