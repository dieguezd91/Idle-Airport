using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IdleAirport.GameCore.Prestige
{
    public sealed class AirportPrestigeAreaView : MonoBehaviour
    {
        [SerializeField] private List<Image> _paletteTargets = new();
        private static readonly int BG1Id = Shader.PropertyToID("_BG1_Color");
        private static readonly int BG2Id = Shader.PropertyToID("_BG2_Color");
        private static readonly int WallId = Shader.PropertyToID("_WallColor");
        private static readonly int BorderId = Shader.PropertyToID("_Border_Color");
        private sealed class TargetRuntime { public Image Image; public Material Original; public Material Runtime; }
        private readonly List<TargetRuntime> _targets = new();
        private Color[] _fromBG1, _fromBG2, _fromWall, _fromBorder;

        private void Awake()
        {
            Setup(gameObject.name);
        }

        public bool ConfigureFromStructuralRoot(RectTransform root, bool boarding, string areaName)
        {
            _paletteTargets.Clear();
            if (root == null)
                return false;
            Transform backgroundRoot = boarding ? FindDescendant(root, "Background_2") : root;
            Image background = backgroundRoot?.GetComponent<Image>();
            Transform borders = FindDescendant(backgroundRoot, "Borders");
            if (background != null)
                _paletteTargets.Add(background);
            else if (boarding)
                AddStructuralMaterialTargets(root);
            if (borders != null)
            {
                Image[] borderImages = borders.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < borderImages.Length; i++)
                {
                    if (borderImages[i] != null && borderImages[i].isActiveAndEnabled && !_paletteTargets.Contains(borderImages[i]))
                        _paletteTargets.Add(borderImages[i]);
                }
            }
            return Setup(areaName);
        }

        private void AddStructuralMaterialTargets(Transform root)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.isActiveAndEnabled && HasBackgroundProperties(image.material) && !_paletteTargets.Contains(image))
                    _paletteTargets.Add(image);
            }
        }
        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root == null)
                return null;
            if (root.name == targetName)
                return root;

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i] != null && descendants[i].name == targetName)
                    return descendants[i];
            }
            return null;
        }
        public bool Setup(string areaName)
        {
            DisposeRuntimeMaterials();
            _targets.Clear();
            if (_paletteTargets == null || _paletteTargets.Count == 0)
            {
                Debug.LogError($"[{nameof(AirportPrestigeAreaView)}] {areaName}: no palette targets are configured.", this);
                return false;
            }
            for (int i = 0; i < _paletteTargets.Count; i++)
            {
                Image image = _paletteTargets[i];
                if (image == null || !image.isActiveAndEnabled)
                    continue;
                if (image.material == null || !HasBackgroundProperties(image.material))
                {
                    Debug.LogError($"[{nameof(AirportPrestigeAreaView)}] {areaName}: target {i} has no S_Background material.", this);
                    continue;
                }
                Material original = image.material;
                Material runtime = new Material(original) { name = $"{original.name} ({areaName} Prestige Runtime)" };
                image.material = runtime;
                _targets.Add(new TargetRuntime { Image = image, Original = original, Runtime = runtime });
            }
            if (_targets.Count == 0)
            {
                Debug.LogError($"[{nameof(AirportPrestigeAreaView)}] {areaName}: no active valid palette targets are configured.", this);
                return false;
            }
            _fromBG1 = new Color[_targets.Count];
            _fromBG2 = new Color[_targets.Count];
            _fromWall = new Color[_targets.Count];
            _fromBorder = new Color[_targets.Count];
            return true;
        }

        public void ApplyImmediate(AirportPrestigeAreaPalette palette)
        {
            if (palette == null) return;
            for (int i = 0; i < _targets.Count; i++) SetColors(_targets[i].Runtime, palette);
        }

        public void BeginTransition()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                Material m = _targets[i].Runtime;
                _fromBG1[i] = m.GetColor(BG1Id);
                _fromBG2[i] = m.GetColor(BG2Id);
                _fromWall[i] = m.GetColor(WallId);
                _fromBorder[i] = m.GetColor(BorderId);
            }
        }

        public void ApplyTransition(AirportPrestigeAreaPalette palette, float t)
        {
            if (palette == null) return;
            for (int i = 0; i < _targets.Count; i++)
            {
                Material m = _targets[i].Runtime;
                m.SetColor(BG1Id, Color.LerpUnclamped(_fromBG1[i], palette.BG1, t));
                m.SetColor(BG2Id, Color.LerpUnclamped(_fromBG2[i], palette.BG2, t));
                m.SetColor(WallId, Color.LerpUnclamped(_fromWall[i], palette.Wall, t));
                m.SetColor(BorderId, Color.LerpUnclamped(_fromBorder[i], palette.Border, t));
            }
        }

        private static void SetColors(Material m, AirportPrestigeAreaPalette p)
        {
            m.SetColor(BG1Id, p.BG1); m.SetColor(BG2Id, p.BG2); m.SetColor(WallId, p.Wall); m.SetColor(BorderId, p.Border);
        }

        private static bool HasBackgroundProperties(Material m)
        {
            return m != null && m.HasProperty(BG1Id) && m.HasProperty(BG2Id) && m.HasProperty(WallId) && m.HasProperty(BorderId);
        }

        private void OnDestroy() { DisposeRuntimeMaterials(); }

        private void DisposeRuntimeMaterials()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                TargetRuntime target = _targets[i];
                if (target.Image != null) target.Image.material = target.Original;
                if (target.Runtime != null)
                {
                    if (Application.isPlaying) Destroy(target.Runtime); else DestroyImmediate(target.Runtime);
                }
            }
        }
    }
}
