using UnityEngine;

namespace ProjectP.UI
{
    /// <summary>
    /// UI 공통 디자인 값. 모든 UI 조립 도구가 이 에셋의 색과 스프라이트를 쓴다.
    /// 색을 바꾼 뒤 메뉴 Project P > Build All을 다시 실행하면 전체 화면에 반영된다.
    /// 스프라이트는 코드로 생성한 임시 그래픽이며 아트 확정 후 교체한다.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "Project P/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("배경")]
        public Color backgroundTop = new Color32(28, 36, 69, 255);
        public Color backgroundBottom = new Color32(9, 11, 22, 255);

        [Header("면")]
        public Color surface = new Color32(26, 31, 51, 245);
        public Color surfaceRaised = new Color32(40, 47, 76, 255);
        public Color outline = new Color32(66, 76, 115, 255);
        public Color dim = new Color(0f, 0f, 0f, 0.65f);

        [Header("강조")]
        public Color accent = new Color32(242, 193, 78, 255);
        public Color accentText = new Color32(34, 24, 6, 255);
        public Color accentSecondary = new Color32(79, 195, 247, 255);
        public Color danger = new Color32(229, 72, 77, 255);

        [Header("글자")]
        public Color textPrimary = new Color32(244, 241, 232, 255);
        public Color textSecondary = new Color32(168, 175, 199, 255);

        [Header("스프라이트 (자동 생성)")]
        public Sprite rounded;
        public Sprite roundedOutline;
        public Sprite shadow;
        public Sprite circle;
        public Sprite backgroundGradient;
        public Sprite glow;
        public Sprite gemTile;
        public Sprite gemTileOutline;
        public Sprite vignette;
    }
}
