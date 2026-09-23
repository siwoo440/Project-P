namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>
    /// 보석 효과 계산에 쓰는 스탯. 기획서 6.1 — 전투 판정은 메인 캐릭터 스탯 기준이다.
    /// 캐릭터 데이터(11일차) 전까지는 퍼즐 테스트가 임시값을 넣는다.
    /// </summary>
    public readonly struct CombatStats
    {
        public CombatStats(int physicalAttack, int magicAttack, int healPower)
        {
            PhysicalAttack = physicalAttack;
            MagicAttack = magicAttack;
            HealPower = healPower;
        }

        public int PhysicalAttack { get; }
        public int MagicAttack { get; }
        public int HealPower { get; }

        public override string ToString() => $"물리 {PhysicalAttack} · 마법 {MagicAttack} · 회복 {HealPower}";
    }
}
