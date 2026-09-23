using System.Linq;
using ProjectP.Core;
using ProjectP.Data;
using ProjectP.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.EditorTools
{
    /// <summary>
    /// 00_Boot 화면 조립. 기획서 12.2 — 로고, 진행 상태, 로딩 표시만 둔다. 조작 기능은 두지 않는다.
    /// </summary>
    internal static class BootUIBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/00_Boot.unity";
        private const string RootName = "[BootUI]";

        [MenuItem("Project P/Rebuild/Boot UI")]
        private static void Menu() => Build(UIFactory.Load());

        public static void Build(UIFactory ui)
        {
            var scene = UIFactory.OpenScene(ScenePath);
            UIFactory.RemoveRoots(scene, RootName);

            var bootstrapper = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Bootstrapper>(true)).FirstOrDefault();
            if (bootstrapper == null) throw new System.InvalidOperationException("00_Boot에서 Bootstrapper를 찾지 못했습니다.");

            var theme = ui.Theme;
            var root = ui.Canvas(RootName);
            var view = root.AddComponent<BootScreenView>();

            ui.Background(root.transform);
            ui.Glow(root.transform, WithAlpha(theme.accent, 0.08f), new Vector2(1100f, 700f), new Vector2(0f, 90f));
            GemEmblem(ui, root.transform, 60f, 84f, 230f);

            var logo = ui.Text("Logo", root.transform, "PROJECT P", 120f, theme.textPrimary, FontStyles.Bold);
            logo.characterSpacing = 6f;
            UIFactory.Place(logo.rectTransform, new Vector2(1400f, 150f), new Vector2(0f, 110f));

            var subtitle = ui.Text("Subtitle", root.transform, "보석 연결 퍼즐 · 턴제 전략 RPG", 30f, theme.textSecondary);
            UIFactory.Place(subtitle.rectTransform, new Vector2(1000f, 44f), new Vector2(0f, 22f));

            var track = UIFactory.Image("ProgressTrack", root.transform, theme.rounded, theme.surfaceRaised, sliced: true);
            track.pixelsPerUnitMultiplier = 3f;
            UIFactory.Place(track.rectTransform, new Vector2(560f, 12f), new Vector2(0f, -110f));

            var fill = UIFactory.Image("Fill", track.transform, theme.rounded, theme.accent, sliced: true);
            fill.pixelsPerUnitMultiplier = 3f;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var status = ui.Text("Status", root.transform, "준비 중...", 26f, theme.textSecondary);
            UIFactory.Place(status.rectTransform, new Vector2(900f, 40f), new Vector2(0f, -150f));

            var errorPanel = ui.Card("ErrorPanel", root.transform, new Vector2(980f, 240f), new Vector2(0f, -330f),
                new Color32(58, 28, 36, 250));
            errorPanel.Find("Border").GetComponent<Image>().color = theme.danger;

            var errorTitle = ui.Text("Title", errorPanel, "초기화에 실패했습니다", 32f, new Color32(255, 180, 182, 255), FontStyles.Bold);
            UIFactory.AnchorTop(errorTitle.rectTransform, new Vector2(900f, 44f), 28f);

            var errorText = ui.Text("Message", errorPanel, "", 26f, theme.textPrimary);
            UIFactory.AnchorTop(errorText.rectTransform, new Vector2(900f, 130f), 84f);

            UIFactory.Wire(view,
                ("progressFill", fill.rectTransform),
                ("statusText", status),
                ("errorPanel", errorPanel.gameObject),
                ("errorText", errorText));
            UIFactory.Wire(bootstrapper, ("view", view));

            errorPanel.gameObject.SetActive(false);
            UIFactory.SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
            UIFactory.SaveScene(scene);
            Debug.Log("[BootUIBuilder] 00_Boot 화면 조립 완료");
        }

        /// <summary>보석 5종을 가로로 늘어놓은 장식. 타이틀과 같은 브랜드 표시로 쓴다.</summary>
        public static void GemEmblem(UIFactory ui, Transform parent, float size, float spacing, float y, float arc = 0f)
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
            var types = new[] { GemType.Physical, GemType.Magic, GemType.Heal, GemType.Chaos, GemType.Balance };
            var tilts = new[] { -8f, 5f, -3f, 6f, -7f };

            var emblem = UIFactory.Rect("GemEmblem", parent);
            UIFactory.Place(emblem, new Vector2(spacing * 5f, size * 2f), new Vector2(0f, y));

            for (var i = 0; i < types.Length; i++)
            {
                var offset = i - 2;
                var badge = ui.GemBadge(types[i].ToString(), emblem, database != null ? database.GetGem(types[i]) : null, size);
                badge.anchoredPosition = new Vector2(offset * spacing, arc * (1f - offset * offset / 4f));
                badge.localEulerAngles = new Vector3(0f, 0f, tilts[i]);
            }
        }

        public static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
