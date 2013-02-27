using System;
using UnityEngine;
using KSP.IO;

namespace KerbCam {
    public class TransformPoint {
        public Vector3 position;
        public Quaternion rotation;
    }

    public class TransformPointInterpolator : Interpolator4<TransformPoint>.IValueInterpolator {
        public static TransformPointInterpolator instance = new TransformPointInterpolator();

        public TransformPoint Evaluate(
            Key<TransformPoint> am, bool haveAm,
            Key<TransformPoint> a,
            Key<TransformPoint> b,
            Key<TransformPoint> bm, bool haveBm,
            float t
            ) {

            // TODO: Smoother Quaternion interpolation.
            //rot = Quaternion.Slerp(a.value.rotation, b.value.rotation, t);

            return new TransformPoint {
                position = EvaluatePosition(
                    ref am, haveAm, ref a, ref b, ref bm, haveBm, t),
                rotation = EvaluateRotation(
                    ref am, haveAm, ref a, ref b, ref bm, haveBm, t)
            };
        }

        private static Vector3 EvaluatePosition(
            ref Key<TransformPoint> am, bool haveAm,
            ref Key<TransformPoint> a,
            ref Key<TransformPoint> b,
            ref Key<TransformPoint> bm, bool haveBm,
            float t) {

            Vector3 m0 = new Vector3(0, 0, 0);
            if (haveAm) {
                float dp = a.param - am.param;
                m0.x = (a.value.position.x - am.value.position.x) / dp;
                m0.y = (a.value.position.y - am.value.position.y) / dp;
                m0.z = (a.value.position.z - am.value.position.z) / dp;
            }
            Vector3 m1 = new Vector3(0, 0, 0);
            if (haveBm) {
                float dp = b.param - bm.param;
                m1.x = (b.value.position.x - bm.value.position.x) / dp;
                m1.y = (b.value.position.y - bm.value.position.y) / dp;
                m1.z = (b.value.position.z - bm.value.position.z) / dp;
            }
            Vector3 position = new Vector3 {
                x = CubicHermiteSpline.P(t, a.value.position.x, m0.x, b.value.position.x, m1.x),
                y = CubicHermiteSpline.P(t, a.value.position.y, m0.y, b.value.position.y, m1.y),
                z = CubicHermiteSpline.P(t, a.value.position.z, m0.z, b.value.position.z, m1.z)
            };
            return position;
        }

        private static Quaternion EvaluateRotationFirstTry(
            ref Key<TransformPoint> am, bool haveAm,
            ref Key<TransformPoint> a,
            ref Key<TransformPoint> b,
            ref Key<TransformPoint> bm, bool haveBm,
            float t) {

            float angleA, angleB;

            Vector3 va, vb;
            a.value.rotation.ToAngleAxis(out angleA, out va);
            b.value.rotation.ToAngleAxis(out angleB, out vb);
            float angleBetweenRotAxes = Vector3.Angle(va, vb);
            Vector3 rotRotAxis = Vector3.Cross(va, vb);

            // rotRot rotates va to vb for t = 0 to 1.
            Quaternion rotRot = Quaternion.AngleAxis(angleBetweenRotAxes * t, rotRotAxis);
            Vector3 rotAxis = rotRot * va;

            // TODO: Consider wrapping crossing 0/360 degrees.
            float angleRange = angleB - angleA;

            return Quaternion.AngleAxis(angleA + angleRange * t, rotAxis);
        }

        private static Quaternion EvaluateRotation(
            ref Key<TransformPoint> am, bool haveAm,
            ref Key<TransformPoint> a,
            ref Key<TransformPoint> b,
            ref Key<TransformPoint> bm, bool haveBm,
            float t) {

            float am_a_time = am.param - a.param;
            float a_b_time = a.param - b.param;
            float b_bm_time = b.param - bm.param;

            float interval_time = a_b_time * t;

            float angleAm, angleA, angleB, angleBm;
            Vector3 va, vb;
            a.value.rotation.ToAngleAxis(out angleA, out va);
            b.value.rotation.ToAngleAxis(out angleB, out vb);

            Vector3 va_;
            float angSpd_am_a;
            if (haveAm) {
                Vector3 vam;
                am.value.rotation.ToAngleAxis(out angleAm, out vam);
                // v_am_a_rotAxis is the axis of rotation from vam to va.
                Vector3 v_am_a_rotAxis = Vector3.Cross(va, vam);
                v_am_a_rotAxis.Normalize();
                // angSpd_am_a is the anglular speed between vam and va (deg/s).
                float ang_am_a = Vector3.Angle(vam, va);
                angSpd_am_a = ang_am_a / am_a_time;

                // va_ is vam rotating into va, just as the rotation axis will
                // tangentially change at time a.
                va_ = Quaternion.AngleAxis(
                    angSpd_am_a * interval_time,
                    v_am_a_rotAxis) * va;
            } else {
                // Assume that the camera axis not rotating at time a.
                va_ = va;
                angSpd_am_a = 0;
            }

            Vector3 vb_;
            float angSpd_b_bm;
            if (haveBm) {
                Vector3 vbm;
                bm.value.rotation.ToAngleAxis(out angleBm, out vbm);
                // v_b_bm_rotAxis is the axis of rotation from vb to vbm.
                Vector3 v_b_bm_rotAxis = Vector3.Cross(vbm, vb);
                v_b_bm_rotAxis.Normalize();
                // angSpd_am_a is the anglular speed between vb and vbm (deg/s).
                float ang_b_bm = Vector3.Angle(vb, vbm);
                angSpd_b_bm = ang_b_bm / b_bm_time;

                // vb_ is vb rotating into vbm, just as the rotation axis will
                // tangentially change at time b.
                vb_ = Quaternion.AngleAxis(
                    angSpd_b_bm * interval_time,
                    v_b_bm_rotAxis) * vb;
            } else {
                // Assume that the camera axis not rotating at time a.
                vb_ = vb;
                angSpd_b_bm = 0;
            }

            float angleBetweenRotAxes = Vector3.Angle(vb_, va_);
            Vector3 rotRotAxis = Vector3.Cross(va_, vb_);
            rotRotAxis.Normalize();

            float angle = CubicHermiteSpline.P(t, 0, angSpd_am_a, angleBetweenRotAxes, angSpd_b_bm);

            // rotRot rotates va_ to vb_ for t = 0 to 1.
            Quaternion rotRot = Quaternion.AngleAxis(angle, rotRotAxis);
            Vector3 rotAxis = rotRot * va_;
            Debug.Log(string.Format("{0} {1} {2}", va_, vb_, rotAxis));

            // TODO: Consider wrapping crossing 0/360 degrees.
            float angleRange = angleB - angleA;

            return Quaternion.AngleAxis(angleA + angleRange * t, rotAxis);
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

        // The interpolation curves for the transformations.
        private Interpolator4<TransformPoint> transformsCurve =
            new Interpolator4<TransformPoint>(
                TransformPointInterpolator.instance);

        public SimpleCamPath(String name) {
            this.name = name;
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
            return transformsCurve.AddKey(time, v);
        }

        public void AddKeyToEnd(Transform trn) {
            if (transformsCurve.Count > 0) {
                AddKey(trn, MaxTime + 1f);
            } else {
                AddKey(trn, 0f);
            }
        }

        public float TimeAt(int index) {
            return transformsCurve[index].param;
        }

        public int MoveKeyAt(int index, float t) {
            return transformsCurve.MoveKeyAt(index, t);
        }

        public void RemoveKey(int index) {
            transformsCurve.RemoveAt(index);
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
            if (!paused && curTime >= transformsCurve.MaxParam) {
                StopRunning();
            }
        }

        private void UpdateTransform() {
            Transform camTrn2 = runningCam.transform;
            Transform camTrn1 = camTrn2.parent;

            TransformPoint curTrnPoint = transformsCurve.Evaluate(curTime);
            camTrn1.localRotation = Quaternion.identity;
            camTrn1.localPosition = curTrnPoint.position;
            camTrn2.localRotation = curTrnPoint.rotation;
            camTrn2.localPosition = Vector3.zero;
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

