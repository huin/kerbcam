using UnityEngine;

namespace KerbCam {
    public class CameraController {
        private bool isControlling = false;

        private Camera oldCamSettings;
        private Vector3 oldCamPos;
        private Quaternion oldCamRot;
        private Transform oldCamTrnParent;

        protected Camera cam;
        private GameObject camTrnObj = new GameObject("KerbCam transform");

        public CameraController() {
            oldCamSettings = new GameObject().AddComponent<Camera>();
            oldCamSettings.enabled = false;
        }

        public Camera Camera {
            get { return cam; }
        }

        public bool IsControlling {
            get { return isControlling; }
        }

        public void StartControlling() {
            if (isControlling) {
                return;
            }

            var fc = FlightCamera.fetch;
            fc.DeactivateUpdate();
            cam = fc.camera;

            // Save old camera state.
            oldCamSettings.CopyFrom(cam);
            oldCamSettings.enabled = false;
            oldCamPos = cam.transform.localPosition;
            oldCamRot = cam.transform.localRotation;
            oldCamTrnParent = cam.transform.parent;

            // Replace with our own state.
            cam.transform.parent = camTrnObj.transform;
            camTrnObj.transform.parent = FlightGlobals.ActiveVessel.transform;
            cam.enabled = true;
        }

        public void StopControlling() {
            if (!isControlling) {
                return;
            }
            isControlling = false;

            // Restore old camera state.
            cam.CopyFrom(oldCamSettings);
            cam.enabled = true;
            cam.transform.localPosition = oldCamPos;
            cam.transform.localRotation = oldCamRot;
            cam.transform.parent = oldCamTrnParent;
            oldCamTrnParent = null;

            var fc = FlightCamera.fetch;
            fc.ActivateUpdate();
        }
    }
}
