using UnityEngine;

namespace KerbCam {
    public class PathRunner : MonoBehaviour, CameraController.Client {
        // Running state variables.
        private CameraController controller = null;
        private bool isPaused = false;
        private float lastSeenTime;
        private float curTime = 0.0F;

        private GameObject ownerObject;
        private SimpleCamPath path;

        internal static PathRunner Create(SimpleCamPath path) {
            GameObject ownerObject = new GameObject("KerbCam.PathRunner");
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
            get { return controller != null; }
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

        public void ToggleRunning(CameraController controller) {
            if (!IsRunning)
                StartRunning(controller);
            else
                StopRunning();
        }

        public void StartRunning(CameraController controller) {
            if (IsRunning || path.NumKeys == 0) {
                return;
            }

            controller.StartControlling(this);
            this.controller = controller;

            lastSeenTime = Time.realtimeSinceStartup;
            path.UpdateTransform(State.camControl.Camera.transform, curTime);
        }

        public void StopRunning() {
            if (!IsRunning) {
                return;
            }
            if (controller != null) {
                controller.StopControlling();
                controller = null;
            }
            isPaused = false;
            curTime = 0f;
        }

        public void TogglePause() {
            isPaused = !isPaused;
        }

        /// <summary>
        /// Overrides MonoBehaviour.Update.
        /// </summary>
        public void Update() {
            if (!IsRunning)
                return;

            float worldTime = Time.realtimeSinceStartup;
            if (!isPaused) {
                float dt = worldTime - lastSeenTime;
                curTime += dt;
            }
            lastSeenTime = worldTime;

            path.UpdateTransform(controller.Camera.transform, curTime);
            if (!isPaused && curTime >= path.MaxTime) {
                // Pause at the end of the path.
                isPaused = true;
            }
        }

        private void HandleToggleRun() {
            State.SelectedPath.Runner.ToggleRunning(State.camControl);
        }

        private void HandleTogglePause() {
            State.SelectedPath.Runner.TogglePause();
        }

        /// <summary>
        /// Overrides MonoBehaviour.OnEnable.
        /// </summary>
        public void OnEnable() {
            State.keyBindings.ListenKeyUp(BoundKey.KEY_PATH_TOGGLE_RUNNING, HandleToggleRun);
            State.keyBindings.ListenKeyUp(BoundKey.KEY_PATH_TOGGLE_PAUSE, HandleTogglePause);
        }

        /// <summary>
        /// Overrides MonoBehaviour.OnDisable.
        /// </summary>
        public void OnDisable() {
            State.keyBindings.UnlistenKeyUp(BoundKey.KEY_PATH_TOGGLE_RUNNING, HandleToggleRun);
            State.keyBindings.UnlistenKeyUp(BoundKey.KEY_PATH_TOGGLE_PAUSE, HandleTogglePause);
        }

        void CameraController.Client.LoseController() {
            controller = null;
            StopRunning();
        }
    }
}