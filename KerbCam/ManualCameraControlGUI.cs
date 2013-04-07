using System;
using UnityEngine;

namespace KerbCam {
    class ManualCameraControlGUI {
        private const float GRID_SIZE = 25f;
        private static GUILayoutOption[] BUTTON_OPTS = new GUILayoutOption[]{
                GUILayout.Height(GRID_SIZE),
                GUILayout.Width(GRID_SIZE),
                GUILayout.ExpandHeight(false),
                GUILayout.ExpandWidth(false)
            };

        private ButtonGrid trnButtons;
        private ButtonGrid rotButtons;

        public ManualCameraControlGUI() {
            var mc = State.manCamControl;
            trnButtons = new ButtonGrid(new GridButton[][]{
                new GridButton[]{
                    null,
                    new GridButton("\u25b2", mc.TrnUp),
                    new GridButton("\u25b3", mc.TrnForward),
                },
                new GridButton[]{
                    new GridButton("\u25c0", mc.TrnLeft),
                    null,
                    new GridButton("\u25b6", mc.TrnRight),
                },
                new GridButton[]{
                    null,
                    new GridButton("\u25bc", mc.TrnDown),
                    new GridButton("\u25bd", mc.TrnBackward),
                },
            });
            rotButtons = new ButtonGrid(new GridButton[][]{
                new GridButton[]{
                    new GridButton("\u21b6", mc.RotRollLeft),
                    new GridButton("\u2191", mc.RotUp),
                    new GridButton("\u21b7", mc.RotRollRight),
                },
                new GridButton[]{
                    new GridButton("\u2190", mc.RotLeft),
                    null,
                    new GridButton("\u2192", mc.RotRight),
                },
                new GridButton[]{
                    null,
                    new GridButton("\u2193", mc.RotDown),
                    null,
                },
            });
        }

        /// <summary>
        /// Repeat button for use in a ButtonGrid for manual movements.
        /// </summary>
        internal class GridButton {
            string label;
            ManualMove movement;

            public GridButton(string label, ManualMove movement) {
                this.label = label;
                this.movement = movement;
            }

            public void DoGUI() {
                bool state = GUILayout.RepeatButton(
                    label, C.UnpaddedButtonStyle, BUTTON_OPTS);
                if (Event.current.type != EventType.Layout) {
                    // Ignore button state on layout events.
                    movement.SetGuiState(state);
                }
            }
        }

        /// <summary>
        /// Grid of buttons used for manual movements.
        /// </summary>
        internal class ButtonGrid {
            GridButton[][] buttonGrid;

            public ButtonGrid(GridButton[][] buttonGrid) {
                this.buttonGrid = buttonGrid;
            }

            public void DoGUI() {
                foreach (var row in buttonGrid) {
                    GUILayout.BeginHorizontal();
                    foreach (var button in row) {
                        if (button != null) {
                            button.DoGUI();
                        } else {
                            GUILayout.Space(GRID_SIZE);
                        }
                    }
                    GUILayout.EndHorizontal(); // END control toggle
                }
            }
        }

        public float GetGuiMinHeight() {
            return 100;
        }

        public float GetGuiMinWidth() {
            return 200;
        }

        internal void DoGUI() {
            var cc = State.camControl;
            var mc = State.manCamControl;

            GUILayout.BeginVertical(); // BEGIN toggle above movement

            GUILayout.BeginHorizontal(); // BEGIN control toggle
            bool shouldControl = GUILayout.Toggle(cc.IsControlling, "");
            if (shouldControl != cc.IsControlling) {
                if (shouldControl) {
                    State.manCamControl.TakeControl();
                } else {
                    State.manCamControl.LoseControl();
                }
            }
            GUILayout.Label("Controlling camera");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal(); // END control toggle

            GUILayout.BeginHorizontal(); // BEGIN side-by-side

            GUILayout.BeginVertical();
            mc.translateSliderPosition = GUILayout.HorizontalSlider(mc.translateSliderPosition, -3f, 3f);
            trnButtons.DoGUI();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            mc.rotateSliderPosition = GUILayout.HorizontalSlider(mc.rotateSliderPosition, -3f, 3f);
            rotButtons.DoGUI();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal(); // END side-by-side

            GUILayout.EndVertical(); // END toggle above movement
        }
    }
}
