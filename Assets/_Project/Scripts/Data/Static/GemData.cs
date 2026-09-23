using UnityEngine;

namespace ProjectP.Data
{
    /// <summary>
    /// 보석 1종의 정의. 기획서 5.2 / 10.2
    /// 색만으로 구분하지 않도록 종류마다 고유 아이콘을 함께 쓴다.
    /// 8일차에 효과 계수를 이 에셋에 추가한다.
    /// </summary>
    [CreateAssetMenu(fileName = "Gem", menuName = "Project P/Gem Data")]
    public class GemData : ScriptableObject
    {
        [SerializeField] private GemType type;
        [SerializeField] private string displayName;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Color iconColor = Color.white;
        [SerializeField] private Sprite icon;

        [Tooltip("무작위 생성 가중치. 0이면 무작위로 나오지 않는다. 기획서 5.2 공란 → 5종 균등(20)으로 시작.")]
        [Min(0)] [SerializeField] private int spawnWeight = 20;

        [Tooltip("효과 계수. 물리·마법·회복 보석 1개 = 메인 스탯 × 계수. 혼돈·균형은 PuzzleConfig 수치를 쓴다. 기획서 5.2 공란 → 1.0")]
        [Min(0f)] [SerializeField] private float effectCoefficient = 1f;

        public GemType Type => type;
        public string DisplayName => displayName;
        public Color Color => color;
        public Color IconColor => iconColor;
        public Sprite Icon => icon;
        public int SpawnWeight => spawnWeight;
        public float EffectCoefficient => effectCoefficient;
    }
}
