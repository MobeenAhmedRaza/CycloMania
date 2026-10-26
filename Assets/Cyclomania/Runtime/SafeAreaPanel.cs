using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace Cyclomania
{
    // Place on a direct child of a full-screen overlay Canvas.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform panel;
        private Rect lastArea;
        private Vector2Int lastSize;

        private void OnEnable() { panel = GetComponent<RectTransform>(); Apply(true); }
        private void Update() { Apply(false); }

        private void Apply(bool force)
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            var area = Screen.safeArea;
            if (size.x <= 0 || size.y <= 0 || area.width <= 0 || area.height <= 0) return;
            if (!force && size == lastSize && area == lastArea) return;
            lastSize = size;
            lastArea = area;
            panel.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            panel.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }
    }
}
