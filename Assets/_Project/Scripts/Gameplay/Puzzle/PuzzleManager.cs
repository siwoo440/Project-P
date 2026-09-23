using System;
using System.Linq;
using ProjectP.Core;
using ProjectP.Data;
using UnityEngine;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 퍼즐 보드 모듈. Board(규칙)와 BoardView(화면)를 묶는다.
    /// Dev_PuzzleTest와 03_Gameplay가 같은 컴포넌트를 쓴다. 기획서 12.15 — 테스트용으로 코드를 복사하지 않는다.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        private GameDatabase database;
        private BoardGenerator generator;

        public Board Board { get; private set; }
        public int Seed { get; private set; }

        /// <summary>새 보드가 만들어질 때마다 호출된다.</summary>
        public event Action<Board> BoardChanged;

        private void Start()
        {
            if (!GameServices.IsReady)
            {
                Debug.LogError("[PuzzleManager] 전역 서비스가 없습니다. 00_Boot를 거쳤는지 확인하세요.");
                return;
            }

            database = GameServices.Data.Database;
            generator = new BoardGenerator(database.Gems.Where(gem => gem != null).Select(gem => (gem.Type, gem.SpawnWeight)));
            Regenerate();
        }

        /// <summary>새 보드를 만든다. 시드를 주지 않으면 무작위 시드를 쓴다.</summary>
        public void Regenerate(int? seed = null)
        {
            if (generator == null) return;

            Seed = seed ?? UnityEngine.Random.Range(0, 1_000_000);
            var config = database.Puzzle;
            Board = generator.Generate(config.Rows, config.Columns, Seed);
            boardView.Render(Board, database.GetGem);
            BoardChanged?.Invoke(Board);
        }
    }
}
