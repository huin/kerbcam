using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP.IO;


namespace KerbCam {

    // Class purely for the purpose for injecting the plugin.
    // Plugin startup taken from:
    // http://forum.kerbalspaceprogram.com/showthread.php/43027
    public class Bootstrap : KSP.Testing.UnitTest {
        public Bootstrap() {
            var gameObject = new GameObject("KerbCam", typeof(KerbCam));
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }
    }

    // Plugin behaviour class.
    public class KerbCam : MonoBehaviour {
        private bool isEnabled = false;
        private MainWindow mainWindow;
        private State state;

        // TODO: Custom keybindings.
        private Event KEY_PATH_TOGGLE_RUNNING = Event.KeyboardEvent(KeyCode.Home.ToString());
        private Event KEY_PATH_ADD_POINT = Event.KeyboardEvent(KeyCode.Insert.ToString());
        private Event KEY_PATH_TOGGLE_WINDOW = Event.KeyboardEvent(KeyCode.F8.ToString());

        public void OnLevelWasLoaded() {
            isEnabled = (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                && HighLogic.LoadedScene == GameScenes.FLIGHT;

            if (!isEnabled) {
                if (state != null) {
                    state.Stop();
                }
                if (mainWindow != null) {
                    mainWindow.HideWindow();
                }
            } else {
                if (state == null) {
                    state = new State();
                }
                if (mainWindow == null) {
                    mainWindow = new MainWindow(state);
                }
            }
        }

        public void Update() {
            if (!isEnabled)
                return;

            if (state.SelectedPath != null)
                state.SelectedPath.Update();
        }

        public void OnGUI() {
            if (!isEnabled)
                return;

            try {

                var ev = Event.current;

                if (state.SelectedPath != null) {
                    // Events that require an active path.
                    if (ev.Equals(KEY_PATH_ADD_POINT)) {
                        state.SelectedPath.AddKeyToEnd(FlightCamera.fetch.transform);
                    } else if (ev.Equals(KEY_PATH_TOGGLE_RUNNING)) {
                        state.SelectedPath.ToggleRunning(FlightCamera.fetch);
                    }
                }

                if (ev.Equals(KEY_PATH_TOGGLE_WINDOW)) {
                    mainWindow.ToggleWindow();
                }
            } catch (Exception e) {
                Debug.LogError(e.ToString() + "\n" +  e.StackTrace);
            }
        }
    }

    /// <summary>
    /// Central stored state of KerbCam.
    /// </summary>
    class State {
        private SimpleCamPath selectedPath;
        public List<SimpleCamPath> paths = new List<SimpleCamPath>();
        public int numCreatedPaths = 0;

        public void RemovePathAt(int index) {
            var path = paths[index];
            if (path == selectedPath) {
                selectedPath.StopRunning();
                selectedPath = null;
            }
            paths.RemoveAt(index);
        }

        public SimpleCamPath SelectedPath {
            get { return selectedPath; }
            set {
                if (selectedPath != null) {
                    selectedPath.StopRunning();
                }
                selectedPath = value;
            }
        }

        public void Stop() {
            if (selectedPath != null)
                selectedPath.StopRunning();
        }
    }

    class MainWindow {
        private const int WINDOW_ID = 73469086; // xkcd/221 compliance.
        private State state;
        private Version version;

        private bool isWindowOpen = false;
        private SimpleCamPathEditor pathEditor = null;
        private Vector2 pathListScroll = new Vector2();
        private WindowResizer resizer;

        public MainWindow(State state) {
            this.state = state;
            version = Assembly.GetCallingAssembly().GetName().Version;
            resizer = new WindowResizer(
                new Rect(10, 100, 200, 200),
                new Vector2(200, 150));
        }

        public void ToggleWindow() {
            if (isWindowOpen) {
                HideWindow();
            } else {
                ShowWindow();
            }
        }

        public void ShowWindow() {
            isWindowOpen = true;
            RenderingManager.AddToPostDrawQueue(3, new Callback(DrawGUI));
            GUI.FocusWindow(WINDOW_ID);
        }

        public void HideWindow() {
            isWindowOpen = false;
            RenderingManager.RemoveFromPostDrawQueue(3, new Callback(DrawGUI));
        }

        private void DrawGUI() {

            GUI.skin = HighLogic.Skin;
            resizer.Position = GUILayout.Window(
                WINDOW_ID, resizer.Position, DoGUI,
                "KerbCam " + version.ToString(2),
                resizer.LayoutMinWidth(),
                resizer.LayoutMinHeight());
        }

        private void DoGUI(int windowID) {
            try {
                C.InitGUIConstants();

                if (state.SelectedPath != null) {
                    // A path is selected.
                    if (pathEditor == null || !pathEditor.IsForPath(state.SelectedPath)) {
                        // Selected path has changed.
                        pathEditor = state.SelectedPath.MakeEditor();
                        resizer.MinWidth = 400;
                        resizer.MinHeight = 250;
                    }
                } else {
                    // No path is selected.
                    if (pathEditor != null) {
                        pathEditor = null;
                        resizer.MinWidth = 200;
                        resizer.MinHeight = 150;
                    }
                }

                GUILayout.BeginVertical(); // BEGIN outer container

                GUILayout.BeginHorizontal(); // BEGIN left/right panes

                GUILayout.BeginVertical(); // BEGIN main controls

                if (GUILayout.Button("New simple path")) {
                    state.numCreatedPaths++;
                    var newPath = new SimpleCamPath(
                        "Path #" + state.numCreatedPaths,
                        2);
                    state.paths.Add(newPath);
                    state.SelectedPath = newPath;
                }

                // Scroll list allowing selection of an existing path.
                pathListScroll = GUILayout.BeginScrollView(pathListScroll, false, true);
                for (int i = 0; i < state.paths.Count; i++) {
                    GUILayout.BeginHorizontal(); // BEGIN path widgets
                    if (GUILayout.Button("X", C.DeleteButtonStyle)) {
                        state.RemovePathAt(i);
                        if (i >= state.paths.Count) {
                            break;
                        }
                    }

                    {
                        var path = state.paths[i];
                        bool isSelected = path == state.SelectedPath;
                        bool doSelect = GUILayout.Toggle(path == state.SelectedPath, "");
                        if (isSelected != doSelect) {
                            if (doSelect) {
                                state.SelectedPath = path;
                            } else {
                                state.SelectedPath = null;
                            }
                        }
                        GUILayout.Label(path.Name);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal(); // END path widgets
                }
                GUILayout.EndScrollView();

                GUILayout.EndVertical(); // END main controls

                // Path editor lives in right-hand-frame.
                if (pathEditor != null) {
                    pathEditor.DoGUI();
                }

                GUILayout.EndHorizontal(); // END left/right panes
                resizer.HandleResize();
                GUILayout.EndVertical(); // END outer container

                GUI.DragWindow(new Rect(0, 0, 10000, 20));
            } catch (Exception e) {
                Debug.LogError(e.ToString() + "\n" + e.StackTrace);
            }
        }
    }
}
