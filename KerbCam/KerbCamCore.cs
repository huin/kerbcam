using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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
        private bool shouldRun = false;
        private bool initialized = false;

        private bool ShouldRun() {
            bool shouldRunNow =
                !State.broken
                && FlightGlobals.fetch != null
                && FlightGlobals.ActiveVessel != null
                && HighLogic.LoadedScene == GameScenes.FLIGHT;

            if (shouldRun != shouldRunNow) {
                if (!shouldRunNow) {
                    State.Stop();
                } else {
                    Init();
                }
            }

            shouldRun = shouldRunNow;
            return shouldRun;
        }

        private void Init() {
            if (initialized) {
                return;
            }
            initialized = true;

            try {
                C.Init();
                State.Init();

                State.keyBindings.ListenKeyUp(BoundKey.KEY_DEBUG, HandleDebug);

                State.LoadConfig();
                State.LoadPaths();
            } catch (Exception e) {
                DebugUtil.LogException(e);
                State.broken = true;
            }
        }

        public void OnDestroy() {
            Debug.LogWarning("KerbCam was destroyed");
        }

        public void OnGUI() {
            try {
                if (!ShouldRun()) {
                    return;
                }
                State.keyBindings.HandleEvent(Event.current);
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        public void OnLevelWasLoaded(int level) {
            try {
                State.Stop();
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        private void HandleDebug() {
            if (State.developerMode) {
                // Random bits of logging used by the developer to
                // work out whatever the heck he's doing.
                DebugUtil.LogCamerasTransformTree();
                DebugUtil.LogVessel(FlightGlobals.ActiveVessel);
            }
        }
    }

    public enum BoundKey {
        KEY_TOGGLE_WINDOW,
        KEY_PATH_TOGGLE_RUNNING,
        KEY_PATH_TOGGLE_PAUSE,
        KEY_TRN_UP,
        KEY_TRN_FORWARD,
        KEY_TRN_LEFT,
        KEY_TRN_RIGHT,
        KEY_TRN_DOWN,
        KEY_TRN_BACKWARD,
        KEY_ROT_ROLL_LEFT,
        KEY_ROT_UP,
        KEY_ROT_ROLL_RIGHT,
        KEY_ROT_LEFT,
        KEY_ROT_RIGHT,
        KEY_ROT_DOWN,
        KEY_DEBUG,
    }

    /// <summary>
    /// Global stored state of KerbCam.
    /// </summary>
    class State {
        private static bool initialized = false;
        public static bool broken = false;
        public static KeyBindings<BoundKey> keyBindings;
        private static SimpleCamPath selectedPath;
        public static List<SimpleCamPath> paths;
        private static int numCreatedPaths = 0;
        public static bool developerMode = false;
        public static CameraController camControl;
        private static GameObject camControlObj;
        public static ManualCameraControl manCamControl;
        public static MainWindow mainWindow;

        public static void Init() {
            if (initialized) {
                return;
            }
            initialized = true;

            keyBindings = new KeyBindings<BoundKey>();

            keyBindings.AddBinding(BoundKey.KEY_TOGGLE_WINDOW,
                new KeyBind("toggle KerbCam window", true, KeyCode.F8));

            // Playback controls.
            keyBindings.AddBinding(BoundKey.KEY_PATH_TOGGLE_RUNNING,
                new KeyBind("play/stop selected path", false, KeyCode.Insert));
            keyBindings.AddBinding(BoundKey.KEY_PATH_TOGGLE_PAUSE,
                new KeyBind("pause selected path", false, KeyCode.Home));

            // Manual camera control keys.
            keyBindings.AddBinding(BoundKey.KEY_TRN_UP, new KeyBind("translate up"));
            keyBindings.AddBinding(BoundKey.KEY_TRN_FORWARD, new KeyBind("translate forward"));
            keyBindings.AddBinding(BoundKey.KEY_TRN_LEFT, new KeyBind("translate left"));
            keyBindings.AddBinding(BoundKey.KEY_TRN_RIGHT, new KeyBind("translate right"));
            keyBindings.AddBinding(BoundKey.KEY_TRN_DOWN, new KeyBind("translate down"));
            keyBindings.AddBinding(BoundKey.KEY_TRN_BACKWARD, new KeyBind("translate backward"));
            keyBindings.AddBinding(BoundKey.KEY_ROT_ROLL_LEFT, new KeyBind("roll left"));
            keyBindings.AddBinding(BoundKey.KEY_ROT_UP, new KeyBind("pan up"));
            keyBindings.AddBinding(BoundKey.KEY_ROT_ROLL_RIGHT, new KeyBind("roll right"));
            keyBindings.AddBinding(BoundKey.KEY_ROT_LEFT, new KeyBind("pan left"));
            keyBindings.AddBinding(BoundKey.KEY_ROT_RIGHT, new KeyBind("pan right"));
            keyBindings.AddBinding(BoundKey.KEY_ROT_DOWN, new KeyBind("pan down"));

            keyBindings.AddBinding(BoundKey.KEY_DEBUG,
                new KeyBind("log debug data (developer mode only)"));

            paths = new List<SimpleCamPath>();
            camControlObj = new GameObject("KerbCam.CameraController");
            UnityEngine.Object.DontDestroyOnLoad(camControlObj);
            camControl = camControlObj.AddComponent<CameraController>();
            manCamControl = ManualCameraControl.Create();
            mainWindow = new MainWindow();
        }

        public static void LoadConfig() {
            ConfigNode config;
            config = ConfigNode.Load("kerbcam.cfg");
            if (config == null) {
                Debug.LogWarning("KerbCam could not load its configuration. This is okay if one has not been saved yet.");
                return;
            }
            keyBindings.Load(config.GetNode("KEY_BINDINGS"));
            ConfigUtil.Parse<bool>(config, "DEV_MODE", out developerMode, false);
        }

        public static void SaveConfig() {
            var config = new ConfigNode();
            keyBindings.Save(config.AddNode("KEY_BINDINGS"));
            ConfigUtil.Write<bool>(config, "DEV_MODE", developerMode);
            if (!config.Save("kerbcam.cfg")) {
                Debug.LogError("Could not save to kerbcam.cfg");
            }
        }

        public static void LoadPaths() {
            ConfigNode config;
            config = ConfigNode.Load("kerbcam-paths.cfg");
            if (config == null) {
                Debug.LogWarning(
                    "KerbCam could not load paths. This is okay if " +
                    "they have not been saved yet.");
                return;
            }
            var newPaths = new List<SimpleCamPath>();
            foreach (var pathNode in config.GetNodes("PATH")) {
                var path = new SimpleCamPath();
                path.Load(pathNode);
                newPaths.Add(path);
            }
            paths = newPaths;
            SelectedPath = null;
        }

        public static void SavePaths() {
            var config = new ConfigNode();
            foreach (var path in paths) {
                path.Save(config.AddNode("PATH"));
            }
            if (!config.Save("kerbcam-paths.cfg")) {
                Debug.LogError("Could not save to kerbcam-paths.cfg");
            }
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
            var newPath = new SimpleCamPath("Path #" + numCreatedPaths);
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

        public static bool Initialized {
            get { return initialized; }
        }

        public static void Stop() {
            if (!initialized) {
                return;
            }
            camControl.StopControlling();
            if (SelectedPath != null) {
                SelectedPath.StopDrawing();
                SelectedPath = null;
            }
            mainWindow.HideWindow();
        }
    }

    class MainWindow : BaseWindow {
        private Assembly assembly;

        private SimpleCamPathEditor pathEditor = null;
        private Vector2 pathListScroll = new Vector2();
        private WindowResizer resizer;
        private VesselSelectionWindow vesselSelectionWindow;
        private HelpWindow helpWindow;
        private ConfigWindow configWindow;
        private bool cameraControlsOpen = false;
        private ManualCameraControlGUI cameraGui;

        public MainWindow() {
            assembly = Assembly.GetCallingAssembly();
            resizer = new WindowResizer(
                new Rect(50, 50, 250, 250),
                new Vector2(GetGuiMinHeight(), GetGuiMinWidth()));
            vesselSelectionWindow = new VesselSelectionWindow();
            helpWindow = new HelpWindow(assembly);
            cameraGui = new ManualCameraControlGUI();
            configWindow = new ConfigWindow();

            State.keyBindings.ListenKeyUp(BoundKey.KEY_TOGGLE_WINDOW, ToggleWindow);
        }

        public float GetGuiMinHeight() {
            return 200;
        }

        public float GetGuiMinWidth() {
            return 280;
        }

        public override void HideWindow() {
            base.HideWindow();
            vesselSelectionWindow.HideWindow();
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

                GUILayout.BeginVertical(GUILayout.MaxWidth(140)); // BEGIN main controls

                if (GUILayout.Button("New simple path")) {
                    State.SelectedPath = State.NewPath();
                }

                DoPathList();

                if (GUILayout.Button("Relative to")) {
                    vesselSelectionWindow.ShowWindow();
                }

                bool pressed = GUILayout.Button(
                    (cameraControlsOpen ? "\u25bd" : "\u25b9")
                    + " Camera controls",
                    C.FoldButtonStyle);
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
                if (GUILayout.Button("Save")) {
                    State.SavePaths();
                }
                if (GUILayout.Button("Load")) {
                    State.LoadPaths();
                }
                if (GUILayout.Button("Config\u2026")) {
                    configWindow.ToggleWindow();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("?", C.WindowButtonStyle)) {
                    helpWindow.ToggleWindow();
                }
                DoCloseButton();
                resizer.HandleResize();
                GUILayout.EndHorizontal(); // END lower controls

                GUILayout.EndVertical(); // END outer container

                GUI.DragWindow(new Rect(0, 0, 10000, 25));
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        private void DoPathList() {
            var noExpandWidth = GUILayout.ExpandWidth(false);

            // Scroll list allowing selection of an existing path.
            pathListScroll = GUILayout.BeginScrollView(pathListScroll, false, true);
            for (int i = 0; i < State.paths.Count; i++) {
                GUILayout.BeginHorizontal(noExpandWidth); // BEGIN path widgets
                if (GUILayout.Button("X", C.DeleteButtonStyle, noExpandWidth)) {
                    State.RemovePathAt(i);
                    if (i >= State.paths.Count) {
                        break;
                    }
                }

                {
                    var path = State.paths[i];
                    bool isSelected = path == State.SelectedPath;
                    bool doSelect = GUILayout.Toggle(path == State.SelectedPath, "", noExpandWidth);
                    if (isSelected != doSelect) {
                        if (doSelect) {
                            State.SelectedPath = path;
                        } else {
                            State.SelectedPath = null;
                        }
                    }
                    GUILayout.Label(path.Name);
                }
                GUILayout.EndHorizontal(); // END path widgets
            }
            GUILayout.EndScrollView();
        }
    }

    class VesselSelectionWindow : BaseWindow {
        private Vector2 vesselListScroll = new Vector2();
        private WindowResizer resizer;

        public VesselSelectionWindow() {
            resizer = new WindowResizer(
                new Rect(50, 250, 250, 200),
                new Vector2(200, 170));
        }

        protected override void DrawGUI() {
            GUI.skin = HighLogic.Skin;
            resizer.Position = GUILayout.Window(
                windowId, resizer.Position, DoGUI,
                "Choose vessel",
                resizer.LayoutMinWidth(),
                resizer.LayoutMinHeight());
        }

        private void DoGUI(int windowID) {
            try {
                GUILayout.BeginVertical(); // BEGIN outer container

                Transform relTrn = State.camControl.RelativeTrn;

                if (GUILayout.Toggle(relTrn == null, "Active vessel")) {
                    relTrn = null;
                }
                GUILayout.BeginScrollView(vesselListScroll); // BEGIN vessel list scroller
                GUILayout.BeginVertical(); // BEGIN vessel list
                foreach (Vessel vessel in FlightGlobals.Vessels) {
                    bool isVesselSelected = object.ReferenceEquals(relTrn, vessel.transform);
                    if (vessel.loaded && GUILayout.Toggle(isVesselSelected, vessel.name)) {
                        relTrn = vessel.transform;
                    }
                }
                GUILayout.EndVertical(); // END vessel list
                GUILayout.EndScrollView(); // END vessel list scroller

                State.camControl.RelativeTrn = relTrn;

                GUILayout.BeginHorizontal(); // BEGIN lower controls
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
    }

    class HelpWindow : BaseWindow {
        private Assembly assembly;
        private WindowResizer resizer;
        private Vector2 helpScroll = new Vector2();
        private static string origHelpText = string.Join("", new string[]{
            "KerbCam is a basic utility to automatically move the flight",
            " camera along a given path.\n",
            "\n",
            "NOTE: at its current stage of development, it is very rough,",
            " potentially buggy, and feature incomplete. Use at your own risk.",
            " It is not inconceivable that this can crash your spacecraft or",
            " do other nasty things.\n",
            "\n",
            "Paths are saved when the save button is pressed.",
            "\n",
            "Keys: (changeable in Config window)\n",
            "{0}",
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
        private string helpText;

        public HelpWindow(Assembly assembly) {
            this.assembly = assembly;
            resizer = new WindowResizer(
                new Rect(330, 50, 300, 300),
                new Vector2(300, 150));
            State.keyBindings.anyChanged += UpdateHelpText;
            UpdateHelpText();
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
                if (GUILayout.Button("Kerbcam on Spaceport", C.LinkButtonStyle)) {
                    Application.OpenURL("http://kerbalspaceport.com/kerbcam/");
                }
                if (GUILayout.Button("Report issue", C.LinkButtonStyle)) {
                    Application.OpenURL("https://github.com/huin/kerbcam/issues");
                }
                DoCloseButton();
                resizer.HandleResize();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical(); // END outer container

                GUI.DragWindow(new Rect(0, 0, 10000, 25));
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        private void UpdateHelpText() {
            var fmtBindingParts = new List<string>();
            foreach (var kb in State.keyBindings.Bindings()) {
                if (kb.IsBound()) {
                    fmtBindingParts.Add(string.Format("* {0} [{1}]\n",
                        kb.description, kb.HumanBinding));
                }
            }
            string fmtBindings;
            if (fmtBindingParts.Count > 0) {
                fmtBindings = string.Join("", fmtBindingParts.ToArray());
            } else {
                fmtBindings = "<nothing bound>";
            }
            helpText = string.Format(origHelpText,
                fmtBindings);
        }
    }
}
