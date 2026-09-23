using System;
using UnityEditor;
using UnityEngine;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// 전체 조립 메뉴. 순서대로 실행한다:
    /// TMP 준비 → UI 그래픽 생성 → 퍼즐 데이터 → 00_Boot → 01_Title → Dev_PuzzleTest.
    ///
    /// 배치 모드 실행:
    /// Unity.exe -batchmode -projectPath . -executeMethod ProjectP.EditorTools.ProjectSetupMenu.BuildAllBatch
    /// </summary>
    internal static class ProjectSetupMenu
    {
        [MenuItem("Project P/Build All", priority = 0)]
        private static void BuildAllMenu() => Run(interactive: true);

        public static void BuildAllBatch() => EditorApplication.Exit(Run(interactive: false) ? 0 : 1);

        private static bool Run(bool interactive)
        {
            try
            {
                if (!TmpSetup.EnsureEssentials())
                {
                    Notify(interactive, "TextMeshPro 필수 리소스를 가져왔습니다.\n가져오기가 끝나면 Build All을 한 번 더 실행하세요.");
                    return false;
                }

                TmpSetup.EnsureKoreanFallback();
                UIAssetGenerator.Generate();
                PuzzleDataSetup.Run();

                var ui = UIFactory.Load();
                BootUIBuilder.Build(ui);
                TitleUIBuilder.Build(ui);
                PuzzleTestBuilder.Build(ui);

                Debug.Log("[ProjectSetupMenu] Build All 완료");
                Notify(interactive, "완료했습니다.\nUI 그래픽 · 퍼즐 데이터 · 00_Boot · 01_Title · Dev_PuzzleTest");
                return true;
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning($"[ProjectSetupMenu] {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Notify(interactive, $"실패했습니다.\n{e.Message}");
                return false;
            }
        }

        private static void Notify(bool interactive, string message)
        {
            if (interactive && !Application.isBatchMode) EditorUtility.DisplayDialog("Project P", message, "확인");
        }
    }
}
