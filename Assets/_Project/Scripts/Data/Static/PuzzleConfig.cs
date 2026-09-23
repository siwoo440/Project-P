using UnityEngine;

namespace ProjectP.Data
{
    /// <summary>
    /// 퍼즐 수치 모음. 밸런스 조정 시 이 에셋만 고친다.
    /// 에셋에 없는 새 항목은 아래 기본값으로 읽힌다.
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

        [Header("보석 효과 (기획서 5.2 공란 → 제안값)")]
        [Tooltip("혼돈 보석 1개당 대상 적 행동 지연")]
        [Min(0)] [SerializeField] private int chaosDelayPerGem = 1;

        [Tooltip("한 번 연결로 줄 수 있는 행동 지연 최대치")]
        [Min(0)] [SerializeField] private int maxChaosDelay = 2;

        [Tooltip("균형 보석 1개당 다음 턴 행동력")]
        [Min(0)] [SerializeField] private int balanceActionPointsPerGem = 1;

        [Tooltip("다음 턴 행동력 보너스 최대치")]
        [Min(0)] [SerializeField] private int maxNextTurnActionPoints = 3;

        [Header("동일 종류 연속 (기획서 5.5·5.6, 공란 → 9일차 제안값)")]
        [Tooltip("같은 종류 연속 시 2번째부터 1개마다 더하는 배율. 0.2 = +20% 누적 (100% / 120% / 140% …)")]
        [Min(0f)] [SerializeField] private float chainBonusPerGem = 0.2f;

        [Tooltip("이 개수 이상 연속이면 연속 강화 효과(강공격·추가 회복·지연 강화·행동력 +1)")]
        [Min(2)] [SerializeField] private int comboLength = 4;

        [Tooltip("강공격: 그 연속 구간 피해 합 × 이 비율을 한 번 더 준다")]
        [Min(0f)] [SerializeField] private float comboDamageRatio = 0.5f;

        [Tooltip("추가 회복: 그 연속 구간 회복 합 × 이 비율을 더 회복한다")]
        [Min(0f)] [SerializeField] private float comboHealRatio = 0.5f;

        [Tooltip("지연 강화: 혼돈 4연속 시 추가 행동 지연. 혼돈 1회 연결 상한과 별개")]
        [Min(0)] [SerializeField] private int comboDelay = 1;

        [Tooltip("균형 4연속 시 다음 턴 행동력 추가 (기획서 5.6 — +1). 다음 턴 보너스 상한은 지킨다")]
        [Min(0)] [SerializeField] private int comboActionPoints = 1;

        [Tooltip("이 개수 이상 연속이면 경로 마지막 칸에 그 종류의 특수 보석을 만든다 (한 번 연결에 1개)")]
        [Min(2)] [SerializeField] private int specialLength = 5;

        [Header("특수 보석 효과 (기획서 5.6.1, 수치 공란 → 9일차 제안값)")]
        [Tooltip("물리 특수 보석: 한 적에게 물리 공격 × 배수")]
        [Min(0f)] [SerializeField] private float specialPhysicalPower = 3f;

        [Tooltip("마법 특수 보석: 살아 있는 모든 적에게 마법 공격 × 배수")]
        [Min(0f)] [SerializeField] private float specialMagicPower = 1.5f;

        [Tooltip("회복 특수 보석: 회복력 × 배수 (상태 효과 제거는 상태 효과가 생기는 전투 단계에서 추가)")]
        [Min(0f)] [SerializeField] private float specialHealPower = 3f;

        [Tooltip("혼돈 특수 보석: 살아 있는 모든 적 행동 지연")]
        [Min(0)] [SerializeField] private int specialChaosDelay = 1;

        [Tooltip("균형 특수 보석: 다음 턴 행동력 (다음 턴 보너스 상한은 지킨다)")]
        [Min(0)] [SerializeField] private int specialBalanceActionPoints = 2;

        public float ChainBonusPerGem => chainBonusPerGem;
        public int ComboLength => comboLength;
        public float ComboDamageRatio => comboDamageRatio;
        public float ComboHealRatio => comboHealRatio;
        public int ComboDelay => comboDelay;
        public int ComboActionPoints => comboActionPoints;
        public int SpecialLength => specialLength;
        public float SpecialPhysicalPower => specialPhysicalPower;
        public float SpecialMagicPower => specialMagicPower;
        public float SpecialHealPower => specialHealPower;
        public int SpecialChaosDelay => specialChaosDelay;
        public int SpecialBalanceActionPoints => specialBalanceActionPoints;

        public int ChaosDelayPerGem => chaosDelayPerGem;
        public int MaxChaosDelay => maxChaosDelay;
        public int BalanceActionPointsPerGem => balanceActionPointsPerGem;
        public int MaxNextTurnActionPoints => maxNextTurnActionPoints;

        public int Rows => rows;
        public int Columns => columns;
        public int MinConnection => minConnection;
        public int BaseActionPoints => baseActionPoints;
    }
}
