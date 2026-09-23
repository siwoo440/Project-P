using ProjectP.Data;

namespace ProjectP.Gameplay.Puzzle
{
    /// <summary>연결 경로 안 보석 하나: 종류 + 특수 보석 여부. 기획서 5.6 — 특수 보석은 종류마다 있다(물리 특수 등).</summary>
    public readonly struct PathGem
    {
        public PathGem(GemType type, bool isSpecial = false)
        {
            Type = type;
            IsSpecial = isSpecial;
        }

        public GemType Type { get; }
        public bool IsSpecial { get; }

        public static implicit operator PathGem(GemType type) => new PathGem(type);

        public override string ToString() => IsSpecial ? $"{Type}(특수)" : Type.ToString();
    }
}
