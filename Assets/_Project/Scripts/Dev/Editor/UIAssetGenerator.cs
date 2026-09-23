using System.IO;
using ProjectP.UI;
using UnityEditor;
using UnityEngine;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// 임시 UI 그래픽(둥근 사각형·그림자·보석 타일·보석 아이콘 등)을 코드로 그려 PNG로 저장하고 UITheme에 연결한다.
    /// 아트가 확정되면 UITheme·GemData의 스프라이트만 교체하면 된다.
    ///
    /// 테마 색은 처음 만들 때만 기본값을 넣고 이후에는 덮어쓰지 않는다. 배경 그라디언트는 현재 테마 색으로 다시 굽는다.
    /// </summary>
    internal static class UIAssetGenerator
    {
        private const string ArtFolder = "Assets/_Project/Art/UI";
        private const string Folder = ArtFolder + "/Generated";

        public const string IconPhysicalPath = Folder + "/gem_icon_physical.png";
        public const string IconMagicPath = Folder + "/gem_icon_magic.png";
        public const string IconHealPath = Folder + "/gem_icon_heal.png";
        public const string IconChaosPath = Folder + "/gem_icon_chaos.png";
        public const string IconBalancePath = Folder + "/gem_icon_balance.png";

        public static UITheme Generate()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(ArtFolder, "Generated");

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(UIFactory.ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, UIFactory.ThemePath);
            }

            // 9-slice 테두리는 둥근 모서리가 늘어나지 않는 크기로 잡는다.
            theme.rounded = Save("ui_rounded.png", 64, 64, SpriteShapes.RoundedRect(64, 16f), 20);
            theme.roundedOutline = Save("ui_rounded_outline.png", 64, 64, SpriteShapes.RoundedOutline(64, 16f, 3f), 20);
            theme.shadow = Save("ui_shadow.png", 96, 96, SpriteShapes.Shadow(96, 12f, 24f, 0.55f), 36);
            theme.circle = Save("ui_circle.png", 64, 64, SpriteShapes.Circle(64), 0);
            theme.glow = Save("ui_glow.png", 256, 256, SpriteShapes.Glow(256), 0);
            theme.gemTile = Save("gem_tile.png", 128, 128, SpriteShapes.GemTile(128), 0);
            theme.gemTileOutline = Save("gem_tile_outline.png", 128, 128, SpriteShapes.GemTileOutline(128, 7f), 0);
            theme.backgroundGradient = Save("ui_background.png", 16, 512,
                SpriteShapes.VerticalGradient(16, 512, ToRgb(theme.backgroundTop), ToRgb(theme.backgroundBottom)), 0);

            Save(Path.GetFileName(IconPhysicalPath), 128, 128, SpriteShapes.IconPhysical(128), 0);
            Save(Path.GetFileName(IconMagicPath), 128, 128, SpriteShapes.IconMagic(128), 0);
            Save(Path.GetFileName(IconHealPath), 128, 128, SpriteShapes.IconHeal(128), 0);
            Save(Path.GetFileName(IconChaosPath), 128, 128, SpriteShapes.IconChaos(128), 0);
            Save(Path.GetFileName(IconBalancePath), 128, 128, SpriteShapes.IconBalance(128), 0);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[UIAssetGenerator] UI 그래픽 생성 완료");
            return theme;
        }

        private static Sprite Save(string fileName, int width, int height, byte[] pixels, int border)
        {
            var path = $"{Folder}/{fileName}";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static float[] ToRgb(Color color) => new[] { color.r, color.g, color.b };
    }
}
