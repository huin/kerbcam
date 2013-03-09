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

            bool buttonPressed = (
                trnUp || trnForwards || trnLeft || trnRight || trnDown || trnBackwards
                || rotRollLeft || rotUp || rotRollRight || rotLeft || rotRight || rotDown);

            if (buttonPressed && !controller.IsControlling) {
                controller.StartControlling(this);
            }

            if (controller.IsControlling && buttonPressed) {
                Transform rotationTrn = controller.Camera.transform;
                Transform translateTrn = rotationTrn.parent;

                Quaternion rot = Quaternion.Inverse(rotationTrn.root.localRotation) * rotationTrn.rotation;
                Vector3 forward = rot * Vector3.forward;
                Vector3 up = rot * Vector3.up;
                Vector3 right = rot * Vector3.right;

                // Translation actions.
                if (trnForwards) {
                    translateTrn.localPosition += forward;
                }
                if (trnBackwards) {
                    translateTrn.localPosition -= forward;
                }
                if (trnUp) {
                    translateTrn.localPosition += up;
                }
                if (trnDown) {
                    translateTrn.localPosition -= up;
                }
                if (trnRight) {
                    translateTrn.localPosition += right;
                }
                if (trnLeft) {
                    translateTrn.localPosition -= right;
                }

                // Rotation actions.
                // TODO: Make this work properly.
                if (rotRight) {
                    RotateTransformRotation(rotationTrn, 5f, up);
                }
                if (rotLeft) {
                    RotateTransformRotation(rotationTrn, -5f, up);
                }
                if (rotUp) {
                    RotateTransformRotation(rotationTrn, 5f, right);
                }
                if (rotDown) {
                    RotateTransformRotation(rotationTrn, -5f, right);
                }
                if (rotRollRight) {
                    RotateTransformRotation(rotationTrn, 5f, forward);
                }
                if (rotRollLeft) {
                    RotateTransformRotation(rotationTrn, -5f, forward);
                }
            }
        }

        private static void RotateTransformRotation(Transform trn, float angle, Vector3 axis) {
            trn.localRotation = Quaternion.AngleAxis(angle, axis) * trn.localRotation;
        }
    }
}
