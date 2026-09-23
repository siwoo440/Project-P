using System;
using System.Collections.Generic;
using System.Linq;
using ProjectP.Core;
using ProjectP.Data;
using UnityEngine;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 퍼즐 보드 모듈. Board·ConnectionPath(규칙)와 BoardView(화면)를 묶는다.
    /// Dev_PuzzleTest와 03_Gameplay가 같은 컴포넌트를 쓴다. 기획서 12.15 — 테스트용으로 코드를 복사하지 않는다.
    ///
    /// 연결 입력은 BoardInput이 이 컴포넌트의 Begin/Extend/End/Cancel을 호출한다.
    /// 화면은 이벤트를 받아 그리기만 한다.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        private GameDatabase database;
        private BoardGenerator generator;
        private ConnectionPath path;

        public Board Board { get; private set; }
        public int Seed { get; private set; }

        /// <summary>현재 그리고 있는 연결 경로 (읽기 전용).</summary>
        public IReadOnlyList<BoardPosition> Connection => path?.Positions ?? Array.Empty<BoardPosition>();

        /// <summary>새 보드가 만들어질 때마다 호출된다.</summary>
        public event Action<Board> BoardChanged;

        /// <summary>경로가 시작되거나 늘어나거나 되돌려질 때 호출된다.</summary>
        public event Action<IReadOnlyList<BoardPosition>> ConnectionChanged;

        /// <summary>최소 개수 이상으로 손을 떼 확정됐을 때 호출된다. 보드는 아직 바뀌지 않는다(제거는 9일차).</summary>
        public event Action<IReadOnlyList<BoardPosition>> ConnectionConfirmed;

        /// <summary>취소되었거나 최소 개수 미만으로 손을 뗐을 때 호출된다.</summary>
        public event Action ConnectionCanceled;

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

        /// <summary>새 보드를 만든다. 시드를 주지 않으면 무작위 시드를 쓴다. 그리던 연결은 취소된다.</summary>
        public void Regenerate(int? seed = null)
        {
            if (generator == null) return;

            CancelConnection();

            Seed = seed ?? UnityEngine.Random.Range(0, 1_000_000);
            var config = database.Puzzle;
            Board = generator.Generate(config.Rows, config.Columns, Seed);
            path = new ConnectionPath(Board, config.MinConnection);

            boardView.Render(Board, database.GetGem);
            BoardChanged?.Invoke(Board);
        }

        public GemData GetGem(BoardPosition position) => database.GetGem(Board[position]);

        public bool BeginConnection(BoardPosition position)
        {
            if (path == null || path.Begin(position) != ConnectionResult.Added) return false;

            ConnectionChanged?.Invoke(path.Positions);
            return true;
        }

        public ConnectionResult ExtendConnection(BoardPosition position)
        {
            if (path == null) return ConnectionResult.NotStarted;

            var result = path.TryAdd(position);
            if (result == ConnectionResult.Added || result == ConnectionResult.Backtracked) ConnectionChanged?.Invoke(path.Positions);
            return result;
        }

        /// <summary>손을 뗐을 때. 최소 개수 이상이면 확정, 아니면 취소로 처리한다. 기획서 5.3</summary>
        public void EndConnection()
        {
            if (path == null || !path.IsActive) return;

            if (path.TryConfirm(out var confirmed))
            {
                ConnectionConfirmed?.Invoke(confirmed);
                return;
            }

            path.Cancel();
            ConnectionCanceled?.Invoke();
        }

        /// <summary>우클릭·ESC 취소. 기획서 5.3</summary>
        public void CancelConnection()
        {
            if (path == null || !path.IsActive) return;

            path.Cancel();
            ConnectionCanceled?.Invoke();
        }
    }
}
