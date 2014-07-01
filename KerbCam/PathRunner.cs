using System;
using UnityEngine;

namespace KerbCam {
    public class PathRunner : MonoBehaviour, CameraController.Client {
        // Running state variables.
        private bool isRunning = false;
        private bool isPaused = false;
        private float curTime = 0.0F;

        private GameObject ownerObject;
        private SimpleCamPath path;

        internal static PathRunner Create(SimpleCamPath path) {
            GameObject ownerObject = new GameObject("KerbCam.PathRunner");
            UnityEngine.Object.DontDestroyOnLoad(ownerObject);
            PathRunner runner = ownerObject.AddComponent<PathRunner>();
            runner.path = path;
            runner.ownerObject = ownerObject;
            // Don't use GUI layout, just input events and frame updates.
            runner.useGUILayout = false;
            runner.enabled = false;
            return runner;
        }

        public void Destroy() {
            GameObject.Destroy(ownerObject);
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

        public void ToggleRunning() {
            if (!IsRunning)
                StartRunning();
            else
                StopRunning();
        }

        public void StartRunning() {
            if (isRunning || path.Count == 0) {
                return;
            }

            State.camControl.StartControlling(this);
            isRunning = true;

            path.UpdateTransform(
                State.camControl.FirstTransform,
                State.camControl.SecondTransform,
                curTime);
        }

        public void StopRunning() {
            if (!IsRunning) {
                return;
            }
            if (isRunning) {
                State.camControl.StopControlling();
                isRunning = false;
            }
            isPaused = false;
            curTime = 0f;
            // Attempt to nudge the TimeWarp to restore the intended time rate.
            TimeWarp.SetRate(TimeWarp.CurrentRateIndex, true);
        }

        public void TogglePause() {
            isPaused = !isPaused;
        }

        /// <summary>
        /// Overrides MonoBehaviour.Update.
        /// </summary>
        public void Update() {
            try {
                if (!IsRunning)
                    return;

                if (!isPaused) {
                    curTime += Time.deltaTime;
                }

                path.UpdateTransform(
                    State.camControl.FirstTransform,
                    State.camControl.SecondTransform,
                    curTime);
                if (!isPaused && curTime >= path.MaxTime) {
                    // Pause at the end of the path.
                    isPaused = true;
                }
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        private void HandleToggleRun() {
            try {
                State.SelectedPath.Runner.ToggleRunning();
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        private void HandleTogglePause() {
            try {
                State.SelectedPath.Runner.TogglePause();
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        /// <summary>
        /// Overrides MonoBehaviour.OnEnable.
        /// </summary>
        public void OnEnable() {
            try {
                State.keyBindings.ListenKeyUp(BoundKey.KEY_PATH_TOGGLE_RUNNING, HandleToggleRun);
                State.keyBindings.ListenKeyUp(BoundKey.KEY_PATH_TOGGLE_PAUSE, HandleTogglePause);
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        /// <summary>
        /// Overrides MonoBehaviour.OnDisable.
        /// </summary>
        public void OnDisable() {
            try {
                State.keyBindings.UnlistenKeyUp(BoundKey.KEY_PATH_TOGGLE_RUNNING, HandleToggleRun);
                State.keyBindings.UnlistenKeyUp(BoundKey.KEY_PATH_TOGGLE_PAUSE, HandleTogglePause);
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        void CameraController.Client.LoseController() {
            StopRunning();
        }
    }
}