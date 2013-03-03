using System;
using UnityEngine;
using KSP.IO;

namespace KerbCam {
    public class TransformPoint {
        public Vector3 position;
        public Quaternion rotation;
    }

    // Note that this enum may well go away when decent rotation
    // interpolation is implemented and there's no longer a reason
    // to choose.
    public enum RotType {
        Slerp,
        Squad,
        Component
    };

    public class TransformPointInterpolator : Interpolator4<TransformPoint>.IValueInterpolator {

        public RotType rotType = RotType.Slerp;

        public TransformPoint Evaluate(
            Key<TransformPoint> k0, bool haveK0,
            Key<TransformPoint> k1,
            Key<TransformPoint> k2,
            Key<TransformPoint> k3, bool haveK3,
            float t
            ) {

            return new TransformPoint {
                position = EvaluatePosition(
                    ref k0, haveK0, ref k1, ref k2, ref k3, haveK3, t),
                rotation = EvaluateRotation(ref k0, haveK0, ref k1, ref k2, ref k3, haveK3, t)
            };
        }

        private Quaternion EvaluateRotation(ref Key<TransformPoint> k0, bool haveK0, ref Key<TransformPoint> k1, ref Key<TransformPoint> k2, ref Key<TransformPoint> k3, bool haveK3, float t) {
            switch (rotType) {
                case RotType.Component:
                    return EvaluateRotationComponent(
                        ref k0, haveK0, ref k1, ref k2, ref k3, haveK3, t);
                case RotType.Slerp:
                    return EvaluateRotationSlerp(
                        ref k0, haveK0, ref k1, ref k2, ref k3, haveK3, t);
                case RotType.Squad:
                    return EvaluateRotationSquad(
                        ref k0, haveK0, ref k1, ref k2, ref k3, haveK3, t);
                default:
                    Debug.LogWarning("Unhandled RotType " + rotType);
                    goto case RotType.Slerp;
            }
        }

        private static Vector3 EvaluatePosition(
            ref Key<TransformPoint> k0, bool haveK0,
            ref Key<TransformPoint> k1,
            ref Key<TransformPoint> k2,
            ref Key<TransformPoint> k3, bool haveK3,
            float t) {

            float dp = k2.param - k1.param;

            Vector3 m0 = new Vector3(0, 0, 0);
            if (haveK0) {
                m0.x = SplineUtil.T(
                    k0.param, k1.param, k2.param,
                    k0.value.position.x, k1.value.position.x, k2.value.position.x) * dp;
                m0.y = SplineUtil.T(
                    k0.param, k1.param, k2.param,
                    k0.value.position.y, k1.value.position.y, k2.value.position.y) * dp;
                m0.z = SplineUtil.T(
                    k0.param, k1.param, k2.param,
                    k0.value.position.z, k1.value.position.z, k2.value.position.z) * dp;
            }
            Vector3 m1 = new Vector3(0, 0, 0);
            if (haveK3) {
                m1.x = SplineUtil.T(
                    k1.param, k2.param, k3.param,
                    k1.value.position.x, k2.value.position.x, k3.value.position.x) * dp;
                m1.y = SplineUtil.T(
                    k1.param, k2.param, k3.param,
                    k1.value.position.y, k2.value.position.y, k3.value.position.y) * dp;
                m1.z = SplineUtil.T(
                    k1.param, k2.param, k3.param,
                    k1.value.position.z, k2.value.position.z, k3.value.position.z) * dp;
            }
            Vector3 position = new Vector3 {
                x = SplineUtil.CubicHermite(t, k1.value.position.x, m0.x, k2.value.position.x, m1.x),
                y = SplineUtil.CubicHermite(t, k1.value.position.y, m0.y, k2.value.position.y, m1.y),
                z = SplineUtil.CubicHermite(t, k1.value.position.z, m0.z, k2.value.position.z, m1.z)
            };
            return position;
        }

        /// <summary>
        /// Interpolate rotation dumbly using cubic Hermite interpolation of
        /// each Quaternion components. This creates odd effects.
        /// </summary>
        private static Quaternion EvaluateRotationComponent(
            ref Key<TransformPoint> k0, bool haveK0,
            ref Key<TransformPoint> k1,
            ref Key<TransformPoint> k2,
            ref Key<TransformPoint> k3, bool haveK3,
            float t) {

            float dp = k2.param - k1.param;

            Quaternion m0 = new Quaternion(0, 0, 0, 0);
            if (haveK0) {
                m0.x = SplineUtil.T(
                    k0.param, k1.param, k2.param,
                    k0.value.rotation.x, k1.value.rotation.x, k2.value.rotation.x) * dp;
                m0.y = SplineUtil.T(
                    k0.param, k1.param, k2.param,
                    k0.value.rotation.y, k1.value.rotation.y, k2.value.rotation.y) * dp;
                m0.z = SplineUtil.T(
                    k0.param, k1.param, k2.param,
                    k0.value.rotation.z, k1.value.rotation.z, k2.value.rotation.z) * dp;
                m0.w = SplineUtil.T(
                    k0.param, k1.param, k2.param,
                    k0.value.rotation.w, k1.value.rotation.w, k2.value.rotation.w) * dp;
            }
            Quaternion m1 = new Quaternion(0, 0, 0, 0);
            if (haveK3) {
                m1.x = SplineUtil.T(
                    k1.param, k2.param, k3.param,
                    k1.value.rotation.x, k2.value.rotation.x, k3.value.rotation.x) * dp;
                m1.y = SplineUtil.T(
                    k1.param, k2.param, k3.param,
                    k1.value.rotation.y, k2.value.rotation.y, k3.value.rotation.y) * dp;
                m1.z = SplineUtil.T(
                    k1.param, k2.param, k3.param,
                    k1.value.rotation.z, k2.value.rotation.z, k3.value.rotation.z) * dp;
                m1.w = SplineUtil.T(
                    k1.param, k2.param, k3.param,
                    k1.value.rotation.w, k2.value.rotation.w, k3.value.rotation.w) * dp;
            }
            return new Quaternion {
                x = SplineUtil.CubicHermite(t, k1.value.rotation.x, m0.x, k2.value.rotation.x, m1.x),
                y = SplineUtil.CubicHermite(t, k1.value.rotation.y, m0.y, k2.value.rotation.y, m1.y),
                z = SplineUtil.CubicHermite(t, k1.value.rotation.z, m0.z, k2.value.rotation.z, m1.z),
                w = SplineUtil.CubicHermite(t, k1.value.rotation.w, m0.w, k2.value.rotation.w, m1.w)
            };
        }

        private static Quaternion EvaluateRotationSquad(
            ref Key<TransformPoint> k0, bool haveK0,
            ref Key<TransformPoint> k1,
            ref Key<TransformPoint> k2,
            ref Key<TransformPoint> k3, bool haveK3,
            float t) {

            Quaternion s0;
            if (!haveK0) {
                s0 = Quaternion.identity;
            } else {
                s0 = QuatUtil.SquadTangent(k0.value.rotation, k1.value.rotation, k2.value.rotation);
                //float angle;
                //Vector3 axis;
                //s0.ToAngleAxis(out angle, out axis);
                //s0 = Quaternion.AngleAxis(angle / (k2.param - k0.param), axis);
            }

            Quaternion s1;
            if (!haveK3) {
                s1 = Quaternion.identity;
            } else {
                s1 = QuatUtil.SquadTangent(k1.value.rotation, k2.value.rotation, k3.value.rotation);
                //float angle;
                //Vector3 axis;
                //s1.ToAngleAxis(out angle, out axis);
                //s1 = Quaternion.AngleAxis(angle / (k3.param-k1.param), axis);
            }

            return QuatUtil.SquadInterpolate(t,
                k1.value.rotation, k2.value.rotation,
                s0, s1);
        }

        private static Quaternion EvaluateRotationSlerp(
            ref Key<TransformPoint> k0, bool haveK0,
            ref Key<TransformPoint> k1,
            ref Key<TransformPoint> k2,
            ref Key<TransformPoint> k3, bool haveK3,
            float t) {

            return Quaternion.Slerp(k1.value.rotation, k2.value.rotation, t);
        }
    }

    public class SimpleCamPath {
        // Running state variables.
        // TODO: Consider factoring these out into a runner class.
        // TODO: Merge isRunning and paused into an enum.
        private bool isRunning = false;
        private bool isPaused = false;
        private float lastSeenTime;
        private float curTime = 0.0F;
        private FlightCamera runningCam;

        private bool isDrawn = false;
        private Transform drawnRelTo;
        private GameObject drawnPathObj;

        // Initialized from the constructor.
        private string name;

        /// The interpolation curve for the transformations.
        private Interpolator4<TransformPoint> transformsCurve;

        /// Interpolates for the curve.
        private TransformPointInterpolator interpolator;

        public SimpleCamPath(String name) {
            this.name = name;
            interpolator = new TransformPointInterpolator();
            transformsCurve = new Interpolator4<TransformPoint>(interpolator);
        }

        public RotType RotType {
            get { return interpolator.rotType; }
            set { interpolator.rotType = value; }
        }

        public bool IsDrawn {
            get { return isDrawn; }
        }

        public bool IsRunning {
            get { return isRunning; }
        }

        /// The value of IsPaused only has an effect while running.
        public bool IsPaused {
            get { return isPaused; }
            set { isPaused = value; }
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
            get { return transformsCurve.Count; }
        }

        public float MaxTime {
            get { return transformsCurve.MaxParam; }
        }

        private TransformPoint MakeTransformPoint(Transform trn) {
            return new TransformPoint {
                position = trn.parent.localRotation * trn.localPosition,
                rotation = trn.parent.localRotation * trn.localRotation
            };
        }

        public int AddKey(Transform trn, float time) {
            var v = MakeTransformPoint(trn);
            int index = transformsCurve.AddKey(time, v);
            UpdateDrawn();
            return index;
        }

        public void AddKeyToEnd(Transform trn) {
            if (transformsCurve.Count > 0) {
                AddKey(trn, MaxTime + 5f);
            } else {
                AddKey(trn, 0f);
            }
            UpdateDrawn();
        }

        public float TimeAt(int index) {
            return transformsCurve[index].param;
        }

        public int MoveKeyAt(int index, float t) {
            int newIndex = transformsCurve.MoveKeyAt(index, t);
            UpdateDrawn();
            return newIndex;
        }

        public void RemoveKey(int index) {
            transformsCurve.RemoveAt(index);
            UpdateDrawn();
        }

        public void ToggleDrawn(Transform relTo) {
            if (!isDrawn) {
                StartDrawing(relTo);
            } else {
                StopDrawing();
            }
        }

        public void StartDrawing(Transform relTo) {
            isDrawn = true;
            drawnRelTo = relTo;
            drawnPathObj = new GameObject("Path");
            drawnPathObj.transform.parent = relTo;
            drawnPathObj.transform.localPosition = Vector3.zero;
            drawnPathObj.transform.localRotation = Quaternion.identity;

            var lines = (LineRenderer)drawnPathObj.AddComponent("LineRenderer");
            lines.useWorldSpace = false;
            lines.SetColors(Color.white, Color.white);
            lines.SetWidth(0.2f, 0.2f);
            UpdateDrawn();
        }

        public void StopDrawing() {
            isDrawn = false;
            drawnRelTo = null;

            if (drawnPathObj != null) {
                GameObject.Destroy(drawnPathObj);
                drawnPathObj = null;
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
            UpdateTransform(runningCam.transform, curTime);
        }

        public void StopRunning() {
            if (runningCam == null) {
                return;
            }
            isRunning = false;
            isPaused = false;
            runningCam.ActivateUpdate();
            runningCam = null;
        }

        public void TogglePause() {
            isPaused = !isPaused;
        }

        public void Update() {
            if (!isRunning)
                return;

            float worldTime = Time.realtimeSinceStartup;
            if (!isPaused) {
                float dt = worldTime - lastSeenTime;
                curTime += dt;
            }
            lastSeenTime = worldTime;

            UpdateTransform(runningCam.transform, curTime);
            if (!isPaused && curTime >= transformsCurve.MaxParam) {
                // Pause at the end of the path.
                isPaused = true;
            }
        }

        private void UpdateTransform(Transform objTrns, float t) {
            Transform objParentTrns = objTrns.parent;

            TransformPoint curTrnPoint = transformsCurve.Evaluate(t);
            objParentTrns.localRotation = Quaternion.identity;
            objParentTrns.localPosition = curTrnPoint.position;
            objTrns.localRotation = curTrnPoint.rotation;
            objTrns.localPosition = Vector3.zero;
        }

        public SimpleCamPathEditor MakeEditor() {
            return new SimpleCamPathEditor(this);
        }

        private void UpdateDrawn() {
            if (!isDrawn)
                return;

            GameObject pathPosObj = new GameObject("Path Pos");
            Transform pathPosTrn = pathPosObj.transform;
            GameObject pathLookObj = new GameObject("Path Look");
            Transform pathLookTrn = pathLookObj.transform;
            pathPosTrn.parent = null;
            pathLookTrn.parent = pathPosTrn;

            var lines = (LineRenderer)drawnPathObj.GetComponent("LineRenderer");

            int numVerts = (int)((transformsCurve.MaxParam - transformsCurve.MinParam) / 0.1f);
            lines.SetVertexCount(numVerts);

            int i = 0;
            for (float t = transformsCurve.MinParam; i < numVerts && t < transformsCurve.MaxParam; t += 0.1f, i++) {
                UpdateTransform(pathLookTrn, t);

                Vector3 curPos = pathLookTrn.position;
                lines.SetPosition(i, curPos);
            }

            GameObject.Destroy(pathPosObj);
            GameObject.Destroy(pathLookObj);
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

        public void DoGUI(bool developerMode) {
            GUILayout.BeginHorizontal(); // BEGIN outer
            DoPathEditing(developerMode);
            DoKeyEditing();
            GUILayout.EndHorizontal(); // END outer
        }

        private void DoPathEditing(bool developerMode) {
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

            if (developerMode) {
                GUILayout.BeginHorizontal(); // BEGIN rotation choice
                GUILayout.Label("Rotation type:");
                var rotTypes = new RotType[] {
                    RotType.Slerp,
                    RotType.Squad,
                    RotType.Component
                };
                foreach (var rotType in rotTypes) {
                    if (GUILayout.Toggle(path.RotType == rotType, "")) {
                        path.RotType = rotType;
                    }
                    GUILayout.Label(rotType.ToString());
                }
                GUILayout.EndHorizontal(); // END rotation choice
            }

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

            path.IsPaused = GUILayout.Toggle(path.IsPaused, "");
            GUILayout.Label("Pause");

            bool shouldDraw = GUILayout.Toggle(path.IsDrawn, "");
            GUILayout.Label("Draw");
            if (path.IsDrawn != shouldDraw) {
                path.ToggleDrawn(FlightGlobals.ActiveVessel.transform);
            }
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
                string newSelectedKeyTimeString = GUILayout.TextField(selectedKeyTimeString);
                if (newSelectedKeyTimeString != selectedKeyTimeString) {
                    selectedKeyTimeString = newSelectedKeyTimeString;
                    float newKeyTime;
                    if (float.TryParse(selectedKeyTimeString, out newKeyTime)) {
                        selectedKeyIndex = path.MoveKeyAt(selectedKeyIndex, newKeyTime);
                    }
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

            if (GUILayout.Button("View")) {
                path.IsPaused = true;
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
