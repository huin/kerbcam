using System;
using UnityEngine;
using KSP.IO;

namespace KerbCam {
    public class QuaternionSlerpInterpolator : InterpolatorCurve<Quaternion>.IValueInterpolator {
        public static QuaternionSlerpInterpolator instance = new QuaternionSlerpInterpolator();

        public Quaternion Evaluate(Quaternion a, Quaternion b, float t) {
            return Quaternion.Slerp(a, b, t);
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
        private float nextTime = 0;

        // Running state variables.
        // TODO: Consider factoring these out into a runner class.
        // TODO: Merge isRunning and paused into an enum.
        private bool isRunning = false;
        private bool paused = false;
        private float curTime = 0.0F;
        private FlightCamera runningCam;

        // Initialized from the constructor.
        private string name;
        private int numTransformLevels;

        // The interpolation curves for each transformation level.
        // Each curve is maintained with the same number of keys in.
        // TODO: Consider Making one big type containing an array of the
        // translation and rotation for each level.
        private InterpolatorCurve<Quaternion>[] localRotations;
        private Vector3Curve[] localPositions;

        public SimpleCamPath(String name, int numTransformLevels) {
            if (numTransformLevels < 1) {
                throw new BadTransformCountError(numTransformLevels);
            }

            this.name = name;
            this.numTransformLevels = numTransformLevels;

            localRotations = new InterpolatorCurve<Quaternion>[numTransformLevels];
            localPositions = new Vector3Curve[numTransformLevels];
            for (int i = 0; i < numTransformLevels; i++) {
                localRotations[i] = new InterpolatorCurve<Quaternion>(
                    QuaternionSlerpInterpolator.instance);
                localPositions[i] = new Vector3Curve();
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
            get { return localRotations[0].Count; }
        }

        public void AddKey(Transform trn) {
            var currentTrn = trn;
            for (int i = 0; i < localRotations.Length; i++) {
                if (currentTrn == null) {
                    throw new BadTransformCountError(i);
                }
                localRotations[i].AddKey(nextTime, currentTrn.localRotation);
                localPositions[i].Add(nextTime, currentTrn.localPosition);

                currentTrn = currentTrn.parent;
            }
            // TODO: Provide parameter for time (instead of nextTime), and track externally.
            nextTime += 1.0f;
        }

        public float TimeAt(int index) {
            return localRotations[0][index].t;
        }

        public void RemoveKey(int index) {
            foreach (var curve in localRotations) {
                curve.RemoveAt(index);
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

            this.runningCam = cam;
            this.runningCam.DeactivateUpdate();
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

            if (!paused) {
                curTime += Time.deltaTime;
            }
            UpdateTransform();
            if (!paused && curTime >= localRotations[0].Length) {
                StopRunning();
            }
        }

        private void UpdateTransform() {
            var currentTrn = runningCam.transform;
            for (int i = 0; i < localRotations.Length; i++) {
                if (currentTrn == null) {
                    throw new BadTransformCountError(i);
                }
                currentTrn.localRotation = localRotations[i].Evaluate(curTime);
                currentTrn.localPosition = localPositions[i].EvaluateVector(curTime);

                currentTrn = currentTrn.parent;
            }
        }

        public SimpleCamPathEditor MakeEditor() {
            return new SimpleCamPathEditor(this);
        }
    }

    public class SimpleCamPathEditor {
        private Vector2 scrollPosition = new Vector2(0, 0);

        private SimpleCamPath path;

        public SimpleCamPathEditor(SimpleCamPath path) {
            this.path = path;
        }

        public bool IsForPath(SimpleCamPath path) {
            return this.path == path;
        }

        public void DoGUI() {
            // TODO: Proper GUI etc. so that timings can be tweaked.
            // For now, each key is one second apart.
            GUILayout.BeginVertical();
            GUILayout.Label(
                string.Format("Simple camera path [{0} keys]", path.NumKeys));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            path.Name = GUILayout.TextField(path.Name);
            GUILayout.EndHorizontal();

            bool shouldRun = GUILayout.Toggle(path.IsRunning, "Running");
            if (path.IsRunning != shouldRun) {
                path.ToggleRunning(FlightCamera.fetch);
            }
            path.Paused = GUILayout.Toggle(path.Paused, "Paused");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            for (int i = 0; i < path.NumKeys; i++) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("X", C.DeleteButtonStyle)) {
                    path.RemoveKey(i);
                    if (i >= path.NumKeys) {
                        break;
                    }
                }
                if (GUILayout.Button("View", C.CompactButtonStyle)) {
                    path.CurrentTime = path.TimeAt(i);
                }
                GUILayout.Label(
                    string.Format("#{0} @{1}s", i, path.TimeAt(i)),
                    C.CompactLabelStyle);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }
    }
}

