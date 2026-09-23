using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// TextMeshPro 준비: 필수 리소스 가져오기와 한글 fallback 폰트 등록.
    ///
    /// 한글 fallback은 Windows에 설치된 맑은 고딕을 참조만 하는 동적 폰트(Dynamic OS)다.
    /// 폰트 파일을 프로젝트에 넣지 않는 개발용 임시 조치이며 출시 전에 배포 가능한 폰트로 교체한다.
    /// </summary>
    internal static class TmpSetup
    {
        private const string FontFolder = "Assets/_Project/Art/Fonts";
        private const string KoreanFallbackPath = FontFolder + "/KoreanFallback_MalgunGothic.asset";

        /// <summary>TMP 필수 리소스가 준비되어 있으면 true. 방금 가져오기를 시작했다면 false(다시 실행 필요).</summary>
        public static bool EnsureEssentials()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            var path = package == null
                ? null
                : Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");

            if (path == null || !File.Exists(path))
            {
                Debug.LogError("[TmpSetup] TMP 필수 리소스 패키지를 찾지 못했습니다. " +
                               "Window > TextMeshPro > Import TMP Essential Resources를 직접 실행한 뒤 다시 시도하세요.");
                return false;
            }

            AssetDatabase.ImportPackage(path, false);
            AssetDatabase.Refresh();
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;

            Debug.LogWarning("[TmpSetup] TMP 필수 리소스를 가져오는 중입니다. 끝나면 메뉴를 다시 실행하세요.");
            return false;
        }

        public static void EnsureKoreanFallback()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFallbackPath);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 90);
                if (font == null)
                {
                    Debug.LogWarning("[TmpSetup] 맑은 고딕을 찾지 못해 한글 fallback을 등록하지 못했습니다. 한글이 네모로 표시됩니다.");
                    return;
                }

                if (!AssetDatabase.IsValidFolder(FontFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Fonts");

                font.name = Path.GetFileNameWithoutExtension(KoreanFallbackPath);
                AssetDatabase.CreateAsset(font, KoreanFallbackPath);

                // 머티리얼과 아틀라스를 하위 에셋으로 저장해야 참조가 유지된다.
                if (font.material != null)
                {
                    font.material.name = font.name + " Material";
                    AssetDatabase.AddObjectToAsset(font.material, font);
                }

                for (var i = 0; i < font.atlasTextures.Length; i++)
                {
                    if (font.atlasTextures[i] == null) continue;
                    font.atlasTextures[i].name = $"{font.name} Atlas {i}";
                    AssetDatabase.AddObjectToAsset(font.atlasTextures[i], font);
                }

                AssetDatabase.SaveAssets();
            }

            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            var serialized = new SerializedObject(settings);
            var fallbacks = serialized.FindProperty("m_fallbackFontAssets");
            if (fallbacks == null)
            {
                Debug.LogWarning("[TmpSetup] TMP 설정의 fallback 목록을 찾지 못했습니다. " +
                                 $"Project Settings > TextMesh Pro > Settings의 Fallback Font Assets에 {KoreanFallbackPath}를 직접 추가하세요.");
                return;
            }

            for (var i = 0; i < fallbacks.arraySize; i++)
            {
                if (fallbacks.GetArrayElementAtIndex(i).objectReferenceValue == font) return;
            }

            fallbacks.arraySize++;
            fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[TmpSetup] 한글 fallback 폰트를 TMP 설정에 등록했습니다.");
        }
    }
}
