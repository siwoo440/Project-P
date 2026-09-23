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

        [Tooltip("한 번에 확정하려면 최소 몇 개를 이어야 하는지. 기획서 5.3")]
        [Min(1)] [SerializeField] private int minConnection = 2;

        [Tooltip("기본 행동력. 보석 1개를 이을 때마다 1을 쓴다. 기획서 5.4")]
        [Min(1)] [SerializeField] private int baseActionPoints = 6;

        public int Rows => rows;
        public int Columns => columns;
        public int MinConnection => minConnection;
        public int BaseActionPoints => baseActionPoints;
    }
}
