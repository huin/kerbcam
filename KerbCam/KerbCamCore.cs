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

        public void OnLevelWasLoaded() {
            isEnabled = (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                && HighLogic.LoadedScene == GameScenes.FLIGHT;

            if (!isEnabled) {
                State.Stop();
                State.mainWindow.HideWindow();
            }
        }

        public void Awake() {
            try {
                State.LoadConfig();
                State.SaveConfig();
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        public void OnGUI() {
            if (!isEnabled)
                return;

            try {
                var ev = Event.current;

                if (ev.isKey) {
                    if (State.developerMode) {
                        if (ev.Equals(State.KEY_DEBUG)) {
                            // Random bits of logging used by the developer to
                            // work out whatever the heck he's doing.
                            DebugUtil.LogCameras();
                            DebugUtil.LogVessel(FlightGlobals.ActiveVessel);
                            DebugUtil.LogCamera(Camera.main);
                            ev.Use();
                        }
                    }

                    if (ev.Equals(State.KEY_TOGGLE_WINDOW)) {
                        State.mainWindow.ToggleWindow();
                        ev.Use();
                    }
                }
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }
    }

    /// <summary>
    /// Global stored state of KerbCam.
    /// </summary>
    class State {
        // TODO: Custom and additional keybindings.
        public static Event KEY_PATH_TOGGLE_RUNNING = Event.KeyboardEvent(KeyCode.Insert.ToString());
        public static Event KEY_PATH_TOGGLE_PAUSE = Event.KeyboardEvent(KeyCode.Home.ToString());
        public static Event KEY_TOGGLE_WINDOW = Event.KeyboardEvent(KeyCode.F8.ToString());
        public static Event KEY_DEBUG = Event.KeyboardEvent(KeyCode.F7.ToString());

        private static SimpleCamPath selectedPath;
        public static List<SimpleCamPath> paths = new List<SimpleCamPath>();
        private static int numCreatedPaths = 0;
        public static bool developerMode = false;
        public static CameraController camControl = new CameraController();
        public static MainWindow mainWindow = new MainWindow();

        public static void LoadConfig() {
            var config = KSP.IO.PluginConfiguration.CreateForType<State>();
            config.load();
            KEY_PATH_TOGGLE_RUNNING = LoadKeyboardEvent(config, "KEY_PATH_TOGGLE_RUNNING", KeyCode.Insert.ToString());
            KEY_PATH_TOGGLE_PAUSE = LoadKeyboardEvent(config, "KEY_PATH_TOGGLE_PAUSE", KeyCode.Home.ToString());
            KEY_TOGGLE_WINDOW = LoadKeyboardEvent(config, "KEY_TOGGLE_WINDOW", KeyCode.F8.ToString());
        }

        public static void SaveConfig() {
            var config = KSP.IO.PluginConfiguration.CreateForType<State>();
            SaveKeyboardEvent(config, "KEY_PATH_TOGGLE_RUNNING", KEY_PATH_TOGGLE_RUNNING);
            SaveKeyboardEvent(config, "KEY_PATH_TOGGLE_PAUSE", KEY_PATH_TOGGLE_PAUSE);
            SaveKeyboardEvent(config, "KEY_TOGGLE_WINDOW", KEY_TOGGLE_WINDOW);
            config.save();
        }

        private static Event LoadKeyboardEvent(
            PluginConfiguration config, string key, string _default) {

            string bindString = config.GetValue<string>(key, _default);
            return Event.KeyboardEvent(bindString);
        }

        private static void SaveKeyboardEvent(
            PluginConfiguration config, string key, Event ev) {

            config.SetValue(key, GUIHelper.KeyboardEventString(ev));
        }

        public static void RemovePathAt(int index) {
            var path = paths[index];
            if (path == selectedPath) {
                SelectedPath = null;
            }
            paths.RemoveAt(index);
            path.Destroy();
        }

        public static SimpleCamPath NewPath() {
            numCreatedPaths++;
            var newPath = new SimpleCamPath(
                "Path #" + numCreatedPaths,
                FlightCamera.fetch.camera);
            paths.Add(newPath);
            return newPath;
        }

        public static SimpleCamPath SelectedPath {
            get { return selectedPath; }
            set {
                if (selectedPath != null) {
                    selectedPath.Runner.StopRunning();
                    selectedPath.StopDrawing();
                    camControl.StopControlling();
                    selectedPath.Runner.enabled = false;
                }
                selectedPath = value;
                if (value != null) {
                    value.Runner.enabled = true;
                }
            }
        }

        public static void Stop() {
            SelectedPath = null;
            camControl.StopControlling();
        }
    }

    class MainWindow : BaseWindow {
        private Assembly assembly;

        private SimpleCamPathEditor pathEditor = null;
        private Vector2 pathListScroll = new Vector2();
        private WindowResizer resizer;
        private HelpWindow helpWindow;
        private ConfigWindow configWindow;
        private bool cameraControlsOpen = false;
        private CameraControlGUI cameraGui;

        public MainWindow() {
            assembly = Assembly.GetCallingAssembly();
            resizer = new WindowResizer(
                new Rect(10, 100, 200, 200),
                new Vector2(GetGuiMinHeight(), GetGuiMinWidth()));
            helpWindow = new HelpWindow(assembly);
            cameraGui = new CameraControlGUI(State.camControl);
            configWindow = new ConfigWindow();
        }

        public float GetGuiMinHeight() {
            return 200;
        }

        public float GetGuiMinWidth() {
            return 170;
        }

        public override void HideWindow() {
            base.HideWindow();
            helpWindow.HideWindow();
            configWindow.HideWindow();
        }

        protected override void DrawGUI() {
            GUI.skin = HighLogic.Skin;
            resizer.Position = GUILayout.Window(
                windowId, resizer.Position, DoGUI,
                string.Format(
                    "KerbCam [v{0}]",
                    assembly.GetName().Version.ToString(2)),
                resizer.LayoutMinWidth(),
                resizer.LayoutMinHeight());
        }

        private void DoGUI(int windowID) {
            try {
                C.InitGUIConstants();

                if (State.SelectedPath != null) {
                    // A path is selected.
                    if (pathEditor == null || !pathEditor.IsForPath(State.SelectedPath)) {
                        // Selected path has changed.
                        pathEditor = State.SelectedPath.MakeEditor();
                    }
                } else {
                    // No path is selected.
                    if (pathEditor != null) {
                        pathEditor = null;
                    }
                }

                float minHeight = GetGuiMinHeight();
                float minWidth = GetGuiMinWidth();
                if (cameraControlsOpen) {
                    minHeight += cameraGui.GetGuiMinHeight();
                    minWidth = Math.Max(minWidth, cameraGui.GetGuiMinWidth());
                }
                if (pathEditor != null) {
                    minHeight = Math.Max(minHeight, pathEditor.GetGuiMinHeight());
                    minWidth += pathEditor.GetGuiMinWidth();
                }
                resizer.MinHeight = minHeight;
                resizer.MinWidth = minWidth;

                GUILayout.BeginVertical(); // BEGIN outer container

                GUILayout.BeginHorizontal(); // BEGIN left/right panes

                GUILayout.BeginVertical(); // BEGIN main controls

                if (GUILayout.Button("New simple path")) {
                    State.SelectedPath = State.NewPath();
                }

                DoPathList();

                bool pressed = GUILayout.Button(
                    (cameraControlsOpen ? "\u25bd" : "\u25b9")
                    + " Camera controls");
                cameraControlsOpen = cameraControlsOpen ^ pressed;
                if (cameraControlsOpen) {
                    cameraGui.DoGUI();
                }

                GUILayout.EndVertical(); // END main controls

                // Path editor lives in right-hand-frame.
                if (pathEditor != null) {
                    pathEditor.DoGUI();
                }

                GUILayout.EndHorizontal(); // END left/right panes

                GUILayout.BeginHorizontal(); // BEGIN lower controls
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Config...")) {
                    configWindow.ToggleWindow();
                }
                if (GUILayout.Button("?")) {
                    helpWindow.ToggleWindow();
                }
                resizer.HandleResize();
                GUILayout.EndHorizontal(); // END lower controls

                GUILayout.EndVertical(); // END outer container

                GUI.DragWindow(new Rect(0, 0, 10000, 25));
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        private void DoPathList() {
            // Scroll list allowing selection of an existing path.
            pathListScroll = GUILayout.BeginScrollView(pathListScroll, false, true);
            for (int i = 0; i < State.paths.Count; i++) {
                GUILayout.BeginHorizontal(); // BEGIN path widgets
                if (GUILayout.Button("X", C.DeleteButtonStyle)) {
                    State.RemovePathAt(i);
                    if (i >= State.paths.Count) {
                        break;
                    }
                }

                {
                    var path = State.paths[i];
                    bool isSelected = path == State.SelectedPath;
                    bool doSelect = GUILayout.Toggle(path == State.SelectedPath, "");
                    if (isSelected != doSelect) {
                        if (doSelect) {
                            State.SelectedPath = path;
                        } else {
                            State.SelectedPath = null;
                        }
                    }
                    GUILayout.Label(path.Name);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal(); // END path widgets
            }
            GUILayout.EndScrollView();
        }
    }

    class HelpWindow : BaseWindow {
        private Assembly assembly;
        private WindowResizer resizer;
        private Vector2 helpScroll = new Vector2();
        private string helpText = string.Join("", new string[]{
            "KerbCam is a basic utility to automatically move the flight",
            " camera along a given path.\n",
            "\n",
            "NOTE: at its current stage of development, it is very rough,",
            " buggy, and feature incomplete. Use at your own risk. It is not",
            " inconceivable that this can crash your spacecraft or do other",
            " nasty things.\n",
            "\n",
            "Note that paths are not saved, and will be lost when KSP",
            " is restarted.",
            "\n",
            "Keys:\n",
            "* [Insert] Toggle playback of the currently selected path.\n",
            "* [Home] Toggle pause of playback.\n",
            "* [F8] Toggle the KerbCam window.\n",
            "\n",
            "Create a new path, then add keys to it by positioning your view",
            " and add the key with the \"New key\" button. Existing points",
            " can be viewed with the \"View\" button or moved to the current",
            " view position with the \"Set\" button.\n",
            "\n",
            "If more flexible camera control is required, then press the",
            " \"Camera controls\" button to fold out the 6-degrees-of-freedom",
            " controls. The left hand controls control translation, and the",
            " right control orientation. The sliders above each control the",
            " rate of movement or orientation for fine or coarse control of",
            " the camera position and orientation.\n",
            "\n",
            "Source is hosted at https://github.com/huin/kerbcam under the",
            " BSD license."}
        );

        public HelpWindow(Assembly assembly) {
            this.assembly = assembly;
            resizer = new WindowResizer(
                new Rect(10, 300, 300, 200),
                new Vector2(300, 150));
        }

        protected override void DrawGUI() {
            GUI.skin = HighLogic.Skin;
            resizer.Position = GUILayout.Window(
                windowId, resizer.Position, DoGUI,
                "KerbCam Help",
                resizer.LayoutMinWidth(),
                resizer.LayoutMinHeight());
        }

        private void DoGUI(int windowID) {
            try {
                GUILayout.BeginVertical(); // BEGIN outer container

                GUILayout.Label(string.Format(
                    "KerbCam [v{0}]", assembly.GetName().Version.ToString()));

                // BEGIN text scroller.
                helpScroll = GUILayout.BeginScrollView(helpScroll);
                GUILayout.TextArea(helpText);
                GUILayout.EndScrollView(); // END text scroller.

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Kerbcam on Spaceport")) {
                    Application.OpenURL("http://kerbalspaceport.com/kerbcam/");
                }
                if (GUILayout.Button("Report issue")) {
                    Application.OpenURL("https://github.com/huin/kerbcam/issues");
                }
                if (GUILayout.Button("Close")) {
                    HideWindow();
                }
                resizer.HandleResize();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical(); // END outer container

                GUI.DragWindow(new Rect(0, 0, 10000, 25));
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }
    }
}
