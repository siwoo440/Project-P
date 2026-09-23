using System;
using System.Collections.Generic;
using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 동일 종류 연속·4연속 강화·특수 보석 수치. 기획서 5.5 / 5.6 — 공란인 값은 9일차 제안값으로 시작한다.
    /// PuzzleConfig 에셋에서 만들어 GemEffectSettings에 넘긴다.
    /// </summary>
    public sealed class ChainSettings
    {
        private readonly Dictionary<GemType, float> specialPowers;

        public ChainSettings(
            float bonusPerGem = 0.2f,
            int comboLength = 4,
            float comboDamageRatio = 0.5f,
            float comboHealRatio = 0.5f,
            int comboDelay = 1,
            int comboActionPoints = 1,
            int specialLength = 5,
            IDictionary<GemType, float> specialPowers = null)
        {
            if (bonusPerGem < 0f || comboDamageRatio < 0f || comboHealRatio < 0f || comboDelay < 0 || comboActionPoints < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bonusPerGem), "연속 보너스 수치는 0 이상이어야 합니다.");
            }

            if (comboLength < 2) throw new ArgumentOutOfRangeException(nameof(comboLength), comboLength, "연속 강화 기준은 2 이상이어야 합니다.");
            if (specialLength < 2) throw new ArgumentOutOfRangeException(nameof(specialLength), specialLength, "특수 보석 기준은 2 이상이어야 합니다.");

            BonusPerGem = bonusPerGem;
            ComboLength = comboLength;
            ComboDamageRatio = comboDamageRatio;
            ComboHealRatio = comboHealRatio;
            ComboDelay = comboDelay;
            ComboActionPoints = comboActionPoints;
            SpecialLength = specialLength;
            this.specialPowers = specialPowers != null ? new Dictionary<GemType, float>(specialPowers) : DefaultSpecialPowers();
        }

        /// <summary>제안값 그대로.</summary>
        public static ChainSettings Default { get; } = new ChainSettings();

        /// <summary>같은 종류 연속 시 2번째부터 1개마다 더해지는 배율. 기획서 5.5 — 0.2(+20%).</summary>
        public float BonusPerGem { get; }

        /// <summary>이 개수 이상 연속이면 연속 강화 효과. 기획서 5.6 — 4.</summary>
        public int ComboLength { get; }

        /// <summary>강공격(물리·마법 4연속): 그 연속 구간 피해 합 × 이 비율을 한 번 더.</summary>
        public float ComboDamageRatio { get; }

        /// <summary>추가 회복(회복 4연속): 그 연속 구간 회복 합 × 이 비율을 더.</summary>
        public float ComboHealRatio { get; }

        /// <summary>지연 강화(혼돈 4연속): 행동 지연 추가. 혼돈 1회 연결 상한과 별개다.</summary>
        public int ComboDelay { get; }

        /// <summary>균형 4연속: 다음 턴 행동력 추가. 기획서 5.6 — +1. 다음 턴 보너스 상한은 지킨다.</summary>
        public int ComboActionPoints { get; }

        /// <summary>이 개수 이상 연속이면 경로 마지막 칸에 특수 보석 생성. 기획서 5.6 — 5.</summary>
        public int SpecialLength { get; }

        /// <summary>
        /// 특수 보석 효과 크기. 물리·마법·회복 = 메인 스탯 배수, 혼돈 = 모든 적 행동 지연, 균형 = 다음 턴 행동력.
        /// </summary>
        public float SpecialPower(GemType gem) => specialPowers.TryGetValue(gem, out var value) ? value : 0f;

        private static Dictionary<GemType, float> DefaultSpecialPowers() => new Dictionary<GemType, float>
        {
            { GemType.Physical, 3f },
            { GemType.Magic, 1.5f },
            { GemType.Heal, 3f },
            { GemType.Chaos, 1f },
            { GemType.Balance, 2f }
        };
    }
}
