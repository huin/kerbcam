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

            float dp = b.param - a.param;

            Vector3 m0 = new Vector3(0, 0, 0);
            if (haveAm) {
                m0.x = SplineUtil.T(
                    am.param, a.param, b.param,
                    am.value.position.x, a.value.position.x, b.value.position.x)*dp;
                m0.y = SplineUtil.T(
                    am.param, a.param, b.param,
                    am.value.position.y, a.value.position.y, b.value.position.y)*dp;
                m0.z = SplineUtil.T(
                    am.param, a.param, b.param,
                    am.value.position.z, a.value.position.z, b.value.position.z)*dp;
            }
            Vector3 m1 = new Vector3(0, 0, 0);
            if (haveBm) {
                m1.x = SplineUtil.T(
                    a.param, b.param, bm.param,
                    a.value.position.x, b.value.position.x, bm.value.position.x)*dp;
                m1.y = SplineUtil.T(
                    a.param, b.param, bm.param,
                    a.value.position.y, b.value.position.y, bm.value.position.y)*dp;
                m1.z = SplineUtil.T(
                    a.param, b.param, bm.param,
                    a.value.position.z, b.value.position.z, bm.value.position.z)*dp;
            }
            Vector3 position = new Vector3 {
                x = SplineUtil.CubicHermite(t, a.value.position.x, m0.x, b.value.position.x, m1.x),
                y = SplineUtil.CubicHermite(t, a.value.position.y, m0.y, b.value.position.y, m1.y),
                z = SplineUtil.CubicHermite(t, a.value.position.z, m0.z, b.value.position.z, m1.z)
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

        private static Quaternion EvaluateRotationSecondTry(
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
            if (!haveAm) {
                // Assume that the camera axis not rotating at time a.
                va_ = va;
                angSpd_am_a = 0;
            } else {
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
            }

            Vector3 vb_;
            float angSpd_b_bm;
            if (!haveBm) {
                // Assume that the camera axis not rotating at time a.
                vb_ = vb;
                angSpd_b_bm = 0;
            } else {
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
                float startAngle = -angSpd_b_bm * a_b_time;
                vb_ = Quaternion.AngleAxis(
                    startAngle + (angSpd_b_bm * interval_time),
                    v_b_bm_rotAxis) * vb;
            }

            float angleBetweenRotAxes = Vector3.Angle(vb_, va_);
            Vector3 rotRotAxis = Vector3.Cross(va_, vb_);
            rotRotAxis.Normalize();

            float angle = SplineUtil.CubicHermite(t, 0, angSpd_am_a, angleBetweenRotAxes, angSpd_b_bm);

            // rotRot rotates va_ to vb_ for t = 0 to 1.
            Quaternion rotRot = Quaternion.AngleAxis(angle, rotRotAxis);
            Vector3 rotAxis = rotRot * va_;
            //Debug.Log(string.Format("{0} {1} {2}", va_, vb_, rotAxis));

            // TODO: Consider wrapping crossing 0/360 degrees.
            float angleRange = angleB - angleA;

            return Quaternion.AngleAxis(angleA + angleRange * t, rotAxis);
        }

        private static Quaternion EvaluateRotationThirdTry(
            ref Key<TransformPoint> am, bool haveAm,
            ref Key<TransformPoint> a,
            ref Key<TransformPoint> b,
            ref Key<TransformPoint> bm, bool haveBm,
            float t) {

            float dp = b.param - a.param;

            Quaternion m0 = new Quaternion(0, 0, 0, 0);
            if (haveAm) {
                m0.x = SplineUtil.T(
                    am.param, a.param, b.param,
                    am.value.rotation.x, a.value.rotation.x, b.value.rotation.x) * dp;
                m0.y = SplineUtil.T(
                    am.param, a.param, b.param,
                    am.value.rotation.y, a.value.rotation.y, b.value.rotation.y) * dp;
                m0.z = SplineUtil.T(
                    am.param, a.param, b.param,
                    am.value.rotation.z, a.value.rotation.z, b.value.rotation.z) * dp;
                m0.w = SplineUtil.T(
                    am.param, a.param, b.param,
                    am.value.rotation.w, a.value.rotation.w, b.value.rotation.w) * dp;
            }
            Quaternion m1 = new Quaternion(0, 0, 0, 0);
            if (haveBm) {
                m1.x = SplineUtil.T(
                    a.param, b.param, bm.param,
                    a.value.rotation.x, b.value.rotation.x, bm.value.rotation.x) * dp;
                m1.y = SplineUtil.T(
                    a.param, b.param, bm.param,
                    a.value.rotation.y, b.value.rotation.y, bm.value.rotation.y) * dp;
                m1.z = SplineUtil.T(
                    a.param, b.param, bm.param,
                    a.value.rotation.z, b.value.rotation.z, bm.value.rotation.z) * dp;
                m1.w = SplineUtil.T(
                    a.param, b.param, bm.param,
                    a.value.rotation.w, b.value.rotation.w, bm.value.rotation.w) * dp;
            }
            return new Quaternion {
                x = SplineUtil.CubicHermite(t, a.value.rotation.x, m0.x, b.value.rotation.x, m1.x),
                y = SplineUtil.CubicHermite(t, a.value.rotation.y, m0.y, b.value.rotation.y, m1.y),
                z = SplineUtil.CubicHermite(t, a.value.rotation.z, m0.z, b.value.rotation.z, m1.z),
                w = SplineUtil.CubicHermite(t, a.value.rotation.w, m0.w, b.value.rotation.w, m1.w)
            };
        }

        private static Quaternion EvaluateRotation(
            ref Key<TransformPoint> am, bool haveAm,
            ref Key<TransformPoint> a,
            ref Key<TransformPoint> b,
            ref Key<TransformPoint> bm, bool haveBm,
            float t) {

            Quaternion m0;
            if (!haveAm) {
                m0 = Quaternion.identity;
            } else {
                m0 = QuatUtil.SquadTangent(am.value.rotation, a.value.rotation, b.value.rotation);
                float angle;
                Vector3 axis;
                m0.ToAngleAxis(out angle, out axis);
                m0 = Quaternion.AngleAxis(angle / (b.param - am.param), axis);
            }

            Quaternion m1;
            if (!haveBm) {
                m1 = Quaternion.identity;
            } else {
                m1 = QuatUtil.SquadTangent(a.value.rotation, b.value.rotation, bm.value.rotation);
                float angle;
                Vector3 axis;
                m1.ToAngleAxis(out angle, out axis);
                m1 = Quaternion.AngleAxis(angle / (bm.param-a.param), axis);
            }

            return QuatUtil.SquadInterpolate(t,
                a.value.rotation, m0,
                b.value.rotation, m1);

            //return I.HermiteQuaternion(t,
            //    a.value.rotation, m0,
            //    b.value.rotation, m1);
            //return a.value.rotation * m0;
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

        private bool isDrawn = false;
        private Transform drawnRelTo;
        private GameObject drawnPathObj;

        // Initialized from the constructor.
        private string name;

        // The interpolation curves for the transformations.
        private Interpolator4<TransformPoint> transformsCurve =
            new Interpolator4<TransformPoint>(
                TransformPointInterpolator.instance);

        public SimpleCamPath(String name) {
            this.name = name;
        }

        public bool IsDrawn {
            get { return isDrawn; }
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
            int index = transformsCurve.AddKey(time, v);
            UpdateDrawn();
            return index;
        }

        public void AddKeyToEnd(Transform trn) {
            if (transformsCurve.Count > 0) {
                AddKey(trn, MaxTime + 1f);
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
            var lines = (LineRenderer)drawnPathObj.AddComponent("LineRenderer");
            lines.useWorldSpace = true;
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

            UpdateTransform(runningCam.transform, curTime);
            if (!paused && curTime >= transformsCurve.MaxParam) {
                StopRunning();
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
            Debug.Log("UpdateDrawn()");
            if (!isDrawn)
                return;

            GameObject pathPosObj = new GameObject("Path Pos");
            Transform pathPosTrn = pathPosObj.transform;
            GameObject pathLookObj = new GameObject("Path Look");
            Transform pathLookTrn = pathLookObj.transform;
            pathPosTrn.parent = drawnRelTo;
            pathLookTrn.parent = pathPosTrn;

            var lines = (LineRenderer)drawnPathObj.GetComponent("LineRenderer");

            int numVerts = (int)((transformsCurve.MaxParam - transformsCurve.MinParam) / 0.1f);
            lines.SetVertexCount(numVerts);

            int i = 0;
            for (float t = transformsCurve.MinParam; i < numVerts && t < transformsCurve.MaxParam; t += 0.1f, i++) {
                UpdateTransform(pathLookTrn, t);

                Vector3 curPos = pathLookTrn.position;
                lines.SetPosition(i, curPos);

                Debug.Log(string.Format("{0} {1}", t, curPos));
            }
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

            bool shouldDraw = GUILayout.Toggle(path.IsDrawn, "");
            GUILayout.Label("Draw");
            if (path.IsDrawn != shouldDraw) {
                path.ToggleDrawn(FlightCamera.fetch.transform.root);
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
