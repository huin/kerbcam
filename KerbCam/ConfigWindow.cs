using System;
using UnityEngine;

namespace KerbCam {
    class ConfigWindow : BaseWindow {
        private WindowResizer resizer = new WindowResizer(
                new Rect(50, 255, 380, 240),
                new Vector2(380, 240));
        private Vector2 scroll = new Vector2();
        private KeyBind captureTarget;

        protected override void DrawGUI() {
            GUI.skin = HighLogic.Skin;
            resizer.Position = GUILayout.Window(
                windowId, resizer.Position, DoGUI,
                "KerbCam configuration",
                resizer.LayoutMinWidth(),
                resizer.LayoutMinHeight());
        }

        private void DoGUI(int windowID) {
            try {
                GUILayout.BeginVertical(); // BEGIN outer container

                // BEGIN vertical scroll.
                scroll = GUILayout.BeginScrollView(scroll);

                foreach (var kb in State.keyBindings.Bindings()) {
                    DoBinding(kb);
                }

                State.developerMode = GUILayout.Toggle(
                    State.developerMode,
                    "Developer mode - enables experimental features.");

                GUILayout.EndScrollView();
                // END vertical scroll.

                GUILayout.BeginHorizontal(); // BEGIN lower controls
                if (GUILayout.Button("Save")) {
                    State.SaveConfig();
                }
                if (GUILayout.Button("Load")) {
                    State.LoadConfig();
                }
                GUILayout.FlexibleSpace();
                DoCloseButton();
                resizer.HandleResize();
                GUILayout.EndHorizontal(); // END lower controls
                GUILayout.EndVertical(); // END outer container

                GUI.DragWindow(new Rect(0, 0, 10000, 25));
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        private void DoBinding(KeyBind kb) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(kb.description, GUILayout.Width(165));
            string label;
            if (IsCapturing() && kb == captureTarget) {
                label = "...";
            } else {
                label = kb.HumanBinding;
            }
            if (GUILayout.Button(label, GUILayout.Width(110))) {
                if (captureTarget == null) {
                    StartKeyCapture(kb);
                } else {
                    CancelKeyCapture();
                }
            }
            if (!kb.IsRequiredBound() && GUILayout.Button("clear", C.DeleteButtonStyle)) {
                CancelKeyCapture();
                kb.SetBinding(null);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void HandleCapturedKey(Event ev) {
            if (ev.keyCode == KeyCode.Escape) {
                CancelKeyCapture();
            } else {
                CompleteKeyCapture(ev);
            }
        }

        private bool IsCapturing() {
            return captureTarget != null;
        }

        private void StartKeyCapture(KeyBind kb) {
            CancelKeyCapture();
            State.keyBindings.captureAnyKey += HandleCapturedKey;
            captureTarget = kb;
        }

        private void CancelKeyCapture() {
            State.keyBindings.captureAnyKey -= HandleCapturedKey;
            captureTarget = null;
        }

        private void CompleteKeyCapture(Event ev) {
            captureTarget.SetBinding(ev);
            State.keyBindings.captureAnyKey -= HandleCapturedKey;
            captureTarget = null;
        }
    }
}
