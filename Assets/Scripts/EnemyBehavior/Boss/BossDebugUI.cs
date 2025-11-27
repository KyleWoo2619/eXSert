using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyBehavior.Boss
{
    // Lightweight on-screen debug feed for boss actions. Toggle via inspector.
    public sealed class BossDebugUI : MonoBehaviour
    {
        public BossRoombaBrain brain;
        [Range(4, 16)] public int lines = 8;
        public bool show = true;
        public Vector2 anchor = new Vector2(20, 20);
        public int fontSize = 14;
        public Color color = Color.yellow;

        void OnGUI()
        {
            if (!show || brain == null) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, normal = { textColor = color } };
            float y = anchor.y;
            IEnumerable<string> feed = brain.GetRecentActions();
            foreach (var s in feed.ToArray())
            {
                GUI.Label(new Rect(anchor.x, y, 600, 20), s, style);
                y += fontSize + 2;
            }
        }
    }
}
