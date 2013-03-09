using System;
using UnityEngine;

namespace KerbCam {
    class CameraControlGUI : CameraController.Client {
        private CameraController controller;
        private static float GRID_SIZE = 25f;
        private static GUILayoutOption[] BUTTON_OPTS = new GUILayoutOption[]{
                GUILayout.Height(GRID_SIZE),
                GUILayout.Width(GRID_SIZE),
                GUILayout.ExpandHeight(false),
                GUILayout.ExpandWidth(false)
            };

        public CameraControlGUI(CameraController controller) {
            this.controller = controller;
        }

        public float GetGuiMinHeight() {
            return 100;
        }

        public float GetGuiMinWidth() {
            return 200;
        }

        void CameraController.Client.LoseController() {
        }

        internal void DoGUI() {
            GUILayout.BeginVertical(); // BEGIN toggle above movement

            GUILayout.BeginHorizontal(); // BEGIN control toggle
            bool shouldControl = GUILayout.Toggle(controller.IsControlling, "");
            if (shouldControl != controller.IsControlling) {
                if (shouldControl) {
                    controller.StartControlling(this);
                } else {
                    controller.StopControlling();
                }
            }
            GUILayout.Label("Controlling camera");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal(); // END control toggle

            GUILayout.BeginHorizontal(); // BEGIN side-by-side

            GUILayout.BeginVertical(); // BEGIN translation controls
            GUILayout.BeginHorizontal(); // BEGIN top row
            GUILayout.Space(GRID_SIZE);
            bool trnUp = GUILayout.RepeatButton("\u25b2", C.UnpaddedButtonStyle, BUTTON_OPTS); // Up.
            bool trnForwards = GUILayout.RepeatButton("\u25b3", C.UnpaddedButtonStyle, BUTTON_OPTS); // Forwards.
            GUILayout.EndHorizontal(); // END top row
            GUILayout.BeginHorizontal(); // BEGIN middle row
            bool trnLeft = GUILayout.RepeatButton("\u25c0", C.UnpaddedButtonStyle, BUTTON_OPTS); // Left.
            GUILayout.Space(GRID_SIZE);
            bool trnRight = GUILayout.RepeatButton("\u25b6", C.UnpaddedButtonStyle, BUTTON_OPTS); // Right.
            GUILayout.EndHorizontal(); // END middle row
            GUILayout.BeginHorizontal(); // BEGIN bottom row
            GUILayout.Space(GRID_SIZE);
            bool trnDown = GUILayout.RepeatButton("\u25bc", C.UnpaddedButtonStyle, BUTTON_OPTS); // Down.
            bool trnBackwards = GUILayout.RepeatButton("\u25bd", C.UnpaddedButtonStyle, BUTTON_OPTS); // Forwards.
            GUILayout.EndHorizontal(); // END bottom row
            GUILayout.EndVertical(); // END translation controls

            GUILayout.BeginVertical(); // BEGIN rotation controls
            GUILayout.BeginHorizontal(); // BEGIN top row
            bool rotRollLeft = GUILayout.RepeatButton("\u21b6", C.UnpaddedButtonStyle, BUTTON_OPTS); // Up.
            bool rotUp = GUILayout.RepeatButton("\u2191", C.UnpaddedButtonStyle, BUTTON_OPTS); // Up.
            bool rotRollRight = GUILayout.RepeatButton("\u21b7", C.UnpaddedButtonStyle, BUTTON_OPTS); // Up.
            GUILayout.EndHorizontal(); // END top row
            GUILayout.BeginHorizontal(); // BEGIN middle row
            bool rotLeft = GUILayout.RepeatButton("\u2190", C.UnpaddedButtonStyle, BUTTON_OPTS); // Left.
            GUILayout.Space(GRID_SIZE);
            bool rotRight = GUILayout.RepeatButton("\u2192", C.UnpaddedButtonStyle, BUTTON_OPTS); // Right.
            GUILayout.EndHorizontal(); // END middle row
            GUILayout.BeginHorizontal(); // BEGIN bottom row
            GUILayout.Space(GRID_SIZE);
            bool rotDown = GUILayout.RepeatButton("\u2193", C.UnpaddedButtonStyle, BUTTON_OPTS); // Down.
            GUILayout.Space(GRID_SIZE);
            GUILayout.EndHorizontal(); // END bottom row
            GUILayout.EndVertical(); // END rotation controls

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal(); // END side-by-side

            GUILayout.EndVertical(); // END toggle above movement
        }
    }
}
