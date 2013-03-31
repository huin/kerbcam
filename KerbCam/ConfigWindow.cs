using System;
using UnityEngine;

namespace KerbCam {
    class ConfigWindow : BaseWindow {
        private WindowResizer resizer = new WindowResizer(
                new Rect(10, 100, 340, 200),
                new Vector2(340, 200));
        private Vector2 scroll = new Vector2();

        protected override void DrawGUI() {
            GUI.skin = HighLogic.Skin;
            resizer.Position = GUILayout.Window(
                windowId, resizer.Position, DoGUI,
                "KerbCam configuration",
                resizer.LayoutMinWidth(),
                resizer.LayoutMinHeight());
        }

        private void DoGUI(int windowID) {
            GUILayout.BeginVertical(); // BEGIN outer container

            // BEGIN vertical scroll.
            scroll = GUILayout.BeginScrollView(scroll);

            DoBinding(State.KEY_PATH_TOGGLE_RUNNING, "play/stop selected path");
            DoBinding(State.KEY_PATH_TOGGLE_PAUSE, "pause selected path");
            DoBinding(State.KEY_TOGGLE_WINDOW, "toggle KerbCam window");

            State.developerMode = GUILayout.Toggle(
                State.developerMode, "Developer mode - enables experimental features.");

            GUILayout.EndScrollView();
            // END vertical scroll.

            GUILayout.BeginHorizontal(); // BEGIN lower controls
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close")) {
                HideWindow();
            }
            resizer.HandleResize();
            GUILayout.EndHorizontal(); // END lower controls
            GUILayout.EndVertical(); // END outer container

            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        private void DoBinding(Event binding, string desc) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(desc, GUILayout.Width(165));
            GUILayout.Button(GUIHelper.KeyboardEventHumanString(binding), GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
