using System;
using System.Collections.Generic;
using System.Linq;
using ProjectP.Core;
using ProjectP.Data;
using UnityEngine;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 퍼즐 보드 모듈. 규칙(Board·ConnectionPath·PuzzleTurn·BoardGravity)과 화면(BoardView)을 묶는다.
    /// Dev_PuzzleTest와 03_Gameplay가 같은 컴포넌트를 쓴다. 기획서 12.15 — 테스트용으로 코드를 복사하지 않는다.
    ///
    /// 한 턴의 흐름:
    ///   턴 시작(행동력 계산) → 드래그로 경로 작성(행동력만큼) → 놓기
    ///   ├ 취소 영역에서 놓기 / 우클릭·ESC → 취소, 턴 유지
    ///   ├ 2개 미만으로 놓기 → 효과 없이 턴 소모
    ///   └ 2개 이상으로 놓기 → 보석 제거·낙하·보충 → 턴 소모
    ///   → (자동 다음 턴이면) 다음 턴 시작
    /// 실제 게임에서 다음 턴은 13일차 TurnManager가 적 행동 뒤에 시작한다.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        [Tooltip("턴을 쓰면 바로 다음 턴을 시작한다. 퍼즐 테스트용. 실제 게임에서는 TurnManager가 턴을 넘긴다.")]
        [SerializeField] private bool autoNextTurn = true;

        private GameDatabase database;
        private BoardGenerator generator;
        private ConnectionPath path;
        private PuzzleTurn turn;
        private System.Random refillRandom;
        private GemEffectSettings effectSettings;

        public Board Board { get; private set; }

        /// <summary>
        /// 보석 효과 계산에 쓰는 메인 캐릭터 스탯(기획서 6.1). 전투(11일차~)나 퍼즐 테스트가 넣는다.
        /// </summary>
        public CombatStats Stats { get; set; }
        public int Seed { get; private set; }

        /// <summary>현재 그리고 있는 연결 경로 (읽기 전용).</summary>
        public IReadOnlyList<BoardPosition> Connection => path?.Positions ?? (IReadOnlyList<BoardPosition>)Array.Empty<BoardPosition>();

        /// <summary>보석 제거·낙하 연출 중. 이때는 조작할 수 없다.</summary>
        public bool IsResolving { get; private set; }

        public bool CanAct => turn != null && turn.CanAct && !IsResolving;
        public int TurnNumber => turn?.Number ?? 0;
        public int ActionPointLimit => turn?.ActionPoints.Current ?? 0;
        public int BaseActionPoints => turn?.ActionPoints.Base ?? 0;
        public int NextTurnBonus => turn?.ActionPoints.NextTurnBonus ?? 0;

        /// <summary>이번 턴 행동력 중 보너스로 늘어난 몫.</summary>
        public int ActionPointBonus => turn?.ActionPoints.AppliedBonus ?? 0;

        public bool AutoNextTurn
        {
            get => autoNextTurn;
            set
            {
                autoNextTurn = value;
                if (value && turn != null && turn.HasActed && !IsResolving) StartTurn();
                else TurnStateChanged?.Invoke();
            }
        }

        /// <summary>보드 내용이 바뀌었을 때(새 보드, 제거·보충 완료) 호출된다.</summary>
        public event Action<Board> BoardChanged;

        /// <summary>턴 번호·행동력·조작 가능 여부가 바뀌었을 때 호출된다.</summary>
        public event Action TurnStateChanged;

        /// <summary>경로가 시작되거나 늘어나거나 되돌려질 때 호출된다.</summary>
        public event Action<IReadOnlyList<BoardPosition>> ConnectionChanged;

        /// <summary>2개 이상으로 놓아 보석을 사용했을 때. 이 직후 제거·낙하 연출이 시작된다.</summary>
        public event Action<IReadOnlyList<BoardPosition>> ConnectionConfirmed;

        /// <summary>
        /// 사용한 경로의 보석 효과 결과표. 순서대로 처리된 효과와 합계가 들어 있다(기획서 5.5).
        /// 피해·회복·지연은 받는 쪽(전투·퍼즐 테스트)이 적용한다. 균형 보너스는 PuzzleManager가 다음 턴 행동력에 바로 반영한다.
        /// </summary>
        public event Action<EffectSummary> EffectsResolved;

        /// <summary>새 턴이 시작될 때 턴 번호와 함께 호출된다.</summary>
        public event Action<int> TurnStarted;

        /// <summary>2개 미만으로 놓아 효과 없이 턴만 소모했을 때.</summary>
        public event Action<IReadOnlyList<BoardPosition>> ConnectionWasted;

        /// <summary>취소 영역에서 놓았거나 우클릭·ESC로 취소했을 때. 턴은 유지된다.</summary>
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

            var config = database.Puzzle;
            effectSettings = new GemEffectSettings(
                database.Gems.Where(gem => gem != null).ToDictionary(gem => gem.Type, gem => gem.EffectCoefficient),
                config.ChaosDelayPerGem, config.MaxChaosDelay, config.BalanceActionPointsPerGem, config.MaxNextTurnActionPoints);
            Regenerate();
        }

        /// <summary>새 보드로 처음부터 시작한다(턴 1). 시드를 주지 않으면 무작위 시드를 쓴다. 기본 행동력 설정은 유지한다.</summary>
        public void Regenerate(int? seed = null)
        {
            if (generator == null) return;

            CancelConnection();
            IsResolving = false;

            var config = database.Puzzle;
            Seed = seed ?? UnityEngine.Random.Range(0, 1_000_000);
            Board = generator.Generate(config.Rows, config.Columns, Seed);
            refillRandom = new System.Random(unchecked(Seed * 31 + 7)); // 같은 시드 = 같은 보충 순서
            turn = new PuzzleTurn(new ActionPoints(turn?.ActionPoints.Base ?? config.BaseActionPoints, config.MaxNextTurnActionPoints));
            path = new ConnectionPath(Board, config.MinConnection);

            boardView.Render(Board, database.GetGem);
            BoardChanged?.Invoke(Board);
            StartTurn();
        }

        /// <summary>다음 턴을 시작한다. 행동력을 새로 계산하고 보드 조작을 연다.</summary>
        public void StartTurn()
        {
            if (turn == null || IsResolving) return;

            CancelConnection();
            turn.StartTurn();
            path.MaxLength = turn.ActionPoints.Current;
            boardView.SetLocked(false);
            TurnStarted?.Invoke(turn.Number);
            TurnStateChanged?.Invoke();
        }

        /// <summary>기본 행동력을 바꾼다. 다음 턴부터 적용된다. (개발용 조정, 기획서 11.3 — 행동력 6의 적정성 검증)</summary>
        public void SetBaseActionPoints(int value)
        {
            if (turn == null) return;

            turn.ActionPoints.SetBase(Mathf.Max(1, value));
            TurnStateChanged?.Invoke();
        }

        public GemData GetGem(BoardPosition position) => database.GetGem(Board[position]);

        public bool BeginConnection(BoardPosition position)
        {
            if (!CanAct || path.Begin(position) != ConnectionResult.Added) return false;

            ConnectionChanged?.Invoke(path.Positions);
            return true;
        }

        public ConnectionResult ExtendConnection(BoardPosition position)
        {
            if (!CanAct) return ConnectionResult.NotStarted;

            var result = path.TryAdd(position);
            if (result == ConnectionResult.Added || result == ConnectionResult.Backtracked) ConnectionChanged?.Invoke(path.Positions);
            return result;
        }

        /// <summary>손을 뗐을 때. 취소 영역이면 취소, 아니면 개수에 따라 사용하거나 턴만 소모한다.</summary>
        public void EndConnection(bool overCancelZone)
        {
            if (path == null || !path.IsActive) return;

            switch (PuzzleTurn.DecideRelease(path.Count, path.MinLength, overCancelZone))
            {
                case ReleaseOutcome.Canceled:
                    path.Cancel();
                    ConnectionCanceled?.Invoke();
                    break;

                case ReleaseOutcome.Wasted:
                    var wasted = path.Positions.ToList();
                    path.Cancel();
                    turn.MarkActed();
                    ConnectionWasted?.Invoke(wasted);
                    FinishAction();
                    break;

                case ReleaseOutcome.Confirmed:
                    path.TryConfirm(out var confirmed);
                    turn.MarkActed();
                    ConnectionConfirmed?.Invoke(confirmed);
                    ApplyEffects(confirmed);
                    Resolve(confirmed);
                    break;
            }
        }

        /// <summary>우클릭·ESC 취소. 기획서 5.3 — 턴은 유지된다.</summary>
        public void CancelConnection()
        {
            if (path == null || !path.IsActive) return;

            path.Cancel();
            ConnectionCanceled?.Invoke();
        }

        /// <summary>
        /// 사용한 보석의 효과를 연결 순서대로 계산해 알린다(기획서 5.5). 보석이 사라지기 전에 종류를 읽어야 한다.
        /// 균형 보석의 다음 턴 행동력은 여기서 바로 반영한다.
        /// </summary>
        private void ApplyEffects(IReadOnlyList<BoardPosition> used)
        {
            var gems = used.Select(position => Board[position]).ToList();
            var summary = GemEffectResolver.Resolve(gems, Stats, effectSettings);

            if (summary.NextTurnActionPoints > 0) turn.ActionPoints.AddNextTurnBonus(summary.NextTurnActionPoints);
            EffectsResolved?.Invoke(summary);
        }

        /// <summary>사용한 보석을 없애고 남은 보석을 내린 뒤 위에서 새 보석을 채운다. 기획서 5.1</summary>
        private void Resolve(IReadOnlyList<BoardPosition> used)
        {
            IsResolving = true;
            TurnStateChanged?.Invoke();

            var drops = BoardGravity.Collapse(Board, used, () => generator.Pick(refillRandom));
            boardView.PlayResolve(used, drops, Board, database.GetGem, () =>
            {
                IsResolving = false;
                BoardChanged?.Invoke(Board);
                FinishAction();
            });
        }

        private void FinishAction()
        {
            if (autoNextTurn)
            {
                StartTurn();
                return;
            }

            boardView.SetLocked(true);
            TurnStateChanged?.Invoke();
        }
    }
}
