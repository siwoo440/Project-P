using System.Collections.Generic;
using ProjectP.Data;
using UnityEditor;
using UnityEngine;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// 퍼즐 StaticData(GemData 5종, PuzzleConfig)를 만들고 GameDatabase에 연결한다.
    /// 이미 있는 에셋의 값은 덮어쓰지 않는다(밸런스 조정 값 보존). 비어 있는 아이콘만 채운다.
    /// </summary>
    internal static class PuzzleDataSetup
    {
        private const string DatabasePath = "Assets/_Project/Data/GameDatabase.asset";
        private const string ConfigPath = "Assets/_Project/Data/PuzzleConfig.asset";
        private const string GemFolder = "Assets/_Project/Data/Gems";

        // 기본값: 레거시 색 기준, 혼돈은 어두운 배경에서 보이도록 검정 대신 짙은 보라. 생성 가중치는 5종 균등.
        private static readonly GemDefault[] Defaults =
        {
            new GemDefault(GemType.Physical, "물리", new Color32(229, 72, 77, 255), Color.white, UIAssetGenerator.IconPhysicalPath),
            new GemDefault(GemType.Magic, "마법", new Color32(62, 142, 247, 255), Color.white, UIAssetGenerator.IconMagicPath),
            new GemDefault(GemType.Heal, "회복", new Color32(48, 192, 122, 255), Color.white, UIAssetGenerator.IconHealPath),
            new GemDefault(GemType.Chaos, "혼돈", new Color32(107, 63, 196, 255), Color.white, UIAssetGenerator.IconChaosPath),
            new GemDefault(GemType.Balance, "균형", new Color32(233, 228, 212, 255), new Color32(42, 47, 69, 255), UIAssetGenerator.IconBalancePath)
        };

        public static void Run()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"[PuzzleDataSetup] {DatabasePath}가 없습니다.");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<PuzzleConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PuzzleConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var gems = new List<GemData>();
            foreach (var entry in Defaults) gems.Add(LoadOrCreateGem(entry));

            var serialized = new SerializedObject(database);
            serialized.FindProperty("puzzleConfig").objectReferenceValue = config;
            var list = serialized.FindProperty("gems");
            list.arraySize = gems.Count;
            for (var i = 0; i < gems.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = gems[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Debug.Log("[PuzzleDataSetup] 보석 5종·퍼즐 설정을 GameDatabase에 연결했습니다.");
        }

        private static GemData LoadOrCreateGem(GemDefault entry)
        {
            var path = $"{GemFolder}/Gem_{entry.Type}.asset";
            var gem = AssetDatabase.LoadAssetAtPath<GemData>(path);
            var created = gem == null;
            if (created)
            {
                gem = ScriptableObject.CreateInstance<GemData>();
                AssetDatabase.CreateAsset(gem, path);
            }

            var serialized = new SerializedObject(gem);
            if (created)
            {
                serialized.FindProperty("type").intValue = (int)entry.Type;
                serialized.FindProperty("displayName").stringValue = entry.Name;
                serialized.FindProperty("color").colorValue = entry.Color;
                serialized.FindProperty("iconColor").colorValue = entry.IconColor;
                serialized.FindProperty("spawnWeight").intValue = 20;
            }

            var icon = serialized.FindProperty("icon");
            if (icon.objectReferenceValue == null) icon.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(entry.IconPath);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gem;
        }

        private readonly struct GemDefault
        {
            public GemDefault(GemType type, string name, Color color, Color iconColor, string iconPath)
            {
                Type = type;
                Name = name;
                Color = color;
                IconColor = iconColor;
                IconPath = iconPath;
            }

            public GemType Type { get; }
            public string Name { get; }
            public Color Color { get; }
            public Color IconColor { get; }
            public string IconPath { get; }
        }
    }
}
