namespace ProjectP.Core
{
    /// <summary>
    /// Scene 이름 상수. Scene 이름을 문자열로 직접 입력하지 않고 이 상수를 사용한다.
    /// 빌드 목록(EditorBuildSettings)의 Scene 파일명과 일치해야 한다.
    /// </summary>
    public static class SceneNames
    {
        public const string Boot = "00_Boot";
        public const string Title = "01_Title";
        public const string MainHub = "02_MainHub";
        public const string Gameplay = "03_Gameplay";
        public const string Ending = "90_Ending";

        // 개발 전용. 빌드 목록에 없으므로 SceneFlow로는 불러올 수 없다.
        public const string DevPuzzleTest = "Dev_PuzzleTest";
        public const string DevBattleTest = "Dev_BattleTest";
    }
}
