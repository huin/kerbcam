using System;
using UnityEngine;
using KSP.IO;

namespace KerbCam {
    public class QuaternionSlerpInterpolator : Interpolator<Quaternion>.IValueInterpolator {
        public static QuaternionSlerpInterpolator instance = new QuaternionSlerpInterpolator();

        public Quaternion Evaluate(Quaternion a, Quaternion b, float t) {
            return Quaternion.Slerp(a, b, t);
        }
    }

    public class Vector3LerpInterpolator : Interpolator<Vector3>.IValueInterpolator {
        public static Vector3LerpInterpolator instance = new Vector3LerpInterpolator();

        public Vector3 Evaluate(Vector3 a, Vector3 b, float t) {
            return Vector3.Lerp(a, b, t);
        }
    }

    public class BadTransformCountError : Exception {
        public int numTransformLevels;

        public BadTransformCountError(int numTransformLevels) {
            this.numTransformLevels = numTransformLevels;
        }

        public override string ToString() {
            return string.Format("Bad number of transform levels: {0}", numTransformLevels);
        }
    }

    public class SimpleCamPath {
        // Running state variables.
        // TODO: Consider factoring these out into a runner class.
        // TODO: Merge isRunning and paused into an enum.
        private bool isRunning = false;
        private bool paused = false;
        private float lastSeenTime;
        private float curTime = 0.0F;
        private FlightCamera runningCam;

        // Initialized from the constructor.
        private string name;
        private int numTransformLevels;

        // The interpolation curves for each transformation level.
        // Each curve is maintained with the same number of keys in.
        // TODO: Consider Making one big type containing an array of the
        // translation and rotation for each level.
        private Interpolator<Quaternion>[] localRotations;
        private Interpolator<Vector3>[] localPositions;

        public SimpleCamPath(String name, int numTransformLevels) {
            if (numTransformLevels < 1) {
                throw new BadTransformCountError(numTransformLevels);
            }

            this.name = name;
            this.numTransformLevels = numTransformLevels;

            localRotations = new Interpolator<Quaternion>[numTransformLevels];
            localPositions = new Interpolator<Vector3>[numTransformLevels];
            for (int i = 0; i < numTransformLevels; i++) {
                localRotations[i] = new Interpolator<Quaternion>(
                    QuaternionSlerpInterpolator.instance);
                localPositions[i] = new Interpolator<Vector3>(
                    Vector3LerpInterpolator.instance);
            }
        }

        public bool IsRunning {
            get { return isRunning; }
        }

        /// The value of Paused only has an effect while running.
        public bool Paused {
            get { return paused; }
            set { paused = value; }
        }

        /// The value of CurrentTime only has an effect while running.
        public float CurrentTime {
            get { return curTime; }
            set { curTime = value; }
        }

        public string Name {
            get { return name; }
            set { this.name = value; }
        }

        public int NumKeys {
            get { return localRotations[0].Keys.Count; }
        }

        public float MaxTime {
            get { return localRotations[0].Keys.MaxParam; }
        }

        public int AddKey(Transform trn, float time) {
            var currentTrn = trn;
            int newIndex = -1;
            for (int i = 0; i < localRotations.Length; i++) {
                if (currentTrn == null) {
                    throw new BadTransformCountError(i);
                }
                newIndex = localRotations[i].Keys.AddKey(time, currentTrn.localRotation);
                localPositions[i].Keys.AddKey(time, currentTrn.localPosition);

                currentTrn = currentTrn.parent;
            }
            return newIndex;
        }

        public void AddKeyToEnd(Transform trn) {
            if (localRotations[0].Keys.Count > 0) {
                AddKey(trn, MaxTime + 1f);
            } else {
                AddKey(trn, 0f);
            }
        }

        public float TimeAt(int index) {
            return localRotations[0].Keys[index].param;
        }

        public int MoveKeyAt(int index, float t) {
            int newIndex = 0;
            for (int i = 0; i < localRotations.Length; i++) {
                newIndex = localRotations[i].Keys.MoveKeyAt(index, t);
                localPositions[i].Keys.MoveKeyAt(index, t);
            }
            return newIndex;
        }

        public void RemoveKey(int index) {
            foreach (var curve in localRotations) {
                curve.Keys.RemoveAt(index);
            }
            foreach (var curve in localPositions) {
                curve.Keys.RemoveAt(index);
            }
        }

        public void ToggleRunning(FlightCamera cam) {
            if (!isRunning)
                StartRunning(cam);
            else
                StopRunning();
        }

        public void StartRunning(FlightCamera cam) {
            if (runningCam != null) {
                return;
            }
            if (NumKeys == 0) {
                return;
            }

            lastSeenTime = Time.realtimeSinceStartup;

            runningCam = cam;
            runningCam.DeactivateUpdate();
            isRunning = true;
            curTime = 0F;
            UpdateTransform();
        }

        public void StopRunning() {
            if (runningCam == null) {
                return;
            }
            isRunning = false;
            runningCam.ActivateUpdate();
            runningCam = null;
        }

        public void Update() {
            if (!isRunning)
                return;

            float worldTime = Time.realtimeSinceStartup;
            if (!paused) {
                float dt = worldTime - lastSeenTime;
                curTime += dt;
            }
            lastSeenTime = worldTime;

            UpdateTransform();
            if (!paused && curTime >= localRotations[0].Keys.MaxParam) {
                StopRunning();
            }
        }

        private void UpdateTransform() {
            var currentTrn = runningCam.transform;
            for (int i = 0; i < localRotations.Length; i++) {
                if (currentTrn == null) {
                    throw new BadTransformCountError(i);
                }
                var r = currentTrn.localRotation = localRotations[i].Evaluate(curTime);
                currentTrn.localPosition = localPositions[i].Evaluate(curTime);

                currentTrn = currentTrn.parent;
            }
        }

        public SimpleCamPathEditor MakeEditor() {
            return new SimpleCamPathEditor(this);
        }
    }

    public class SimpleCamPathEditor {
        private Vector2 scrollPosition = new Vector2(0, 0);
        private int selectedKeyIndex = -1;
        private string selectedKeyTimeString = "";
        private string newKeyTimeString = "0.00";

        private SimpleCamPath path;

        public SimpleCamPathEditor(SimpleCamPath path) {
            this.path = path;
        }

        public bool IsForPath(SimpleCamPath path) {
            return this.path == path;
        }

        public void DoGUI() {
            GUILayout.BeginHorizontal(); // BEGIN outer
            DoPathEditing();
            DoKeyEditing();
            GUILayout.EndHorizontal(); // END outer
        }

        private void DoPathEditing() {
            GUILayout.BeginVertical(); // BEGIN path editing
            GUILayout.Label(
                string.Format("Simple camera path [{0} keys]", path.NumKeys));

            GUILayout.BeginHorizontal(); // BEGIN name field
            GUILayout.Label("Name:");
            path.Name = GUILayout.TextField(path.Name);
            GUILayout.EndHorizontal(); // END name field

            DoPlaybackControls();

            DoKeysList();

            GUILayout.Label(string.Format("End: {0:0.00}s", path.MaxTime));

            DoNewKeyControls();

            GUILayout.EndVertical(); // END path editing
        }

        private void DoKeysList() {
            // BEGIN Path keys list and scroller.
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            for (int i = 0; i < path.NumKeys; i++) {
                GUILayout.BeginHorizontal();
                bool isSelected = i == selectedKeyIndex;
                bool doSelect = GUILayout.Toggle(isSelected, "");
                if (isSelected != doSelect) {
                    if (doSelect) {
                        selectedKeyIndex = i;
                    } else {
                        selectedKeyIndex = -1;
                    }
                    UpdateSelectedKeyTime();
                }
                GUILayout.Label(
                    string.Format("#{0} @{1:0.00}s", i, path.TimeAt(i)));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView(); // END Path keys list and scroller.
        }

        private void DoPlaybackControls() {
            GUILayout.BeginHorizontal(); // BEGIN playback controls
            GUILayout.Label(string.Format("{0:0.00}s", path.CurrentTime));
            bool shouldRun = GUILayout.Toggle(path.IsRunning, "");
            GUILayout.Label("Play");
            if (path.IsRunning != shouldRun) {
                path.ToggleRunning(FlightCamera.fetch);
            }
            path.Paused = GUILayout.Toggle(path.Paused, "");
            GUILayout.Label("Pause");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal(); // END playback controls
        }

        private void DoNewKeyControls() {
            GUILayout.BeginHorizontal();
            // Create key at the end.
            if (GUILayout.Button("New key")) {
                path.AddKeyToEnd(FlightCamera.fetch.transform);
            }

            // Create key at specified time.
            {
                float newKeyTime;
                bool validNewKeyTime = float.TryParse(newKeyTimeString, out newKeyTime);
                var buttonStyle = validNewKeyTime ? GUI.skin.button : C.DisabledButtonStyle;

                if (GUILayout.Button("at", buttonStyle) && validNewKeyTime) {
                    path.AddKey(FlightCamera.fetch.transform, newKeyTime);
                }
            }

            // Specified time.
            newKeyTimeString = GUILayout.TextField(newKeyTimeString);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DoKeyEditing() {
            if (selectedKeyIndex < 0 || selectedKeyIndex >= path.NumKeys) {
                return;
            }

            // Vertical time slider for selected key time.
            // This is a key editing control, but uses the vertical space
            // between path and key editing controls, so it's not put into
            // the key editing buttons layout region.
            {
                float keyTime = path.TimeAt(selectedKeyIndex);
                float newKeyTime = GUILayout.VerticalSlider(keyTime, 0f, path.MaxTime);
                if (Math.Abs(keyTime - newKeyTime) > 1e-5) {
                    selectedKeyIndex = path.MoveKeyAt(selectedKeyIndex, newKeyTime);
                    UpdateSelectedKeyTime();
                }
            }

            GUILayout.BeginVertical(); // BEGIN key editing buttons
            GUILayout.Label(string.Format("Key #{0}", selectedKeyIndex));

            {
                // Direct editing of key time.
                GUILayout.BeginHorizontal(); // BEGIN key time editing
                selectedKeyTimeString = GUILayout.TextField(selectedKeyTimeString);
                float newKeyTime;
                if (float.TryParse(selectedKeyTimeString, out newKeyTime)) {
                    selectedKeyIndex = path.MoveKeyAt(selectedKeyIndex, newKeyTime);
                }
                GUILayout.Label("s");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal(); // END key time editing
            }

            if (GUILayout.Button("Set")) {
                var keyTime = path.TimeAt(selectedKeyIndex);
                path.RemoveKey(selectedKeyIndex);
                selectedKeyIndex = path.AddKey(FlightCamera.fetch.transform, keyTime);
            }

            if (GUILayout.Button("Dupe")) {
                var keyTime = path.TimeAt(selectedKeyIndex);
                selectedKeyIndex = path.AddKey(FlightCamera.fetch.transform, keyTime+0.01f);
            }

            if (GUILayout.Button("View")) {
                path.Paused = true;
                path.StartRunning(FlightCamera.fetch);
                path.CurrentTime = path.TimeAt(selectedKeyIndex);
            }

            if (GUILayout.Button("Remove", C.DeleteButtonStyle)) {
                path.RemoveKey(selectedKeyIndex);
                if (selectedKeyIndex >= path.NumKeys) {
                    selectedKeyIndex = 0;
                }
                UpdateSelectedKeyTime();
            }

            GUILayout.EndVertical(); // END key editing buttons
        }

        private void UpdateSelectedKeyTime() {
            if (selectedKeyIndex >= 0 && selectedKeyIndex < path.NumKeys) {
                selectedKeyTimeString = string.Format("{0:0.00}",
                    path.TimeAt(selectedKeyIndex));
            } else {
                selectedKeyTimeString = "";
            }
        }
    }
}

