using UnityEngine;

namespace KerbCam {
    public class CameraController {

        public interface Client {
            /// <summary>
            /// Called by the controller when another client acquires the
            /// controller, or the controller stops controlling a camera.
            /// </summary>
            void LoseController();
        }

        private bool isControlling = false;

        private GameObject oldCamSettingsObj = null;
        private Camera oldCamSettings = null;
        private Vector3 oldCamPos;
        private Quaternion oldCamRot;
        private Transform oldCamTrnParent = null;

        protected Camera cam = null;
        private GameObject camTrnObj = null;
        private Client curClient = null;

        public Camera Camera {
            get { return cam; }
        }

        public bool IsControlling {
            get { return isControlling; }
        }

        public void StartControlling(Client client) {
            if (client == curClient) {
                return;
            }
            if (curClient != null) {
                curClient.LoseController();
            }

            curClient = client;
            if (isControlling) {
                return;
            }
            isControlling = true;

            var fc = FlightCamera.fetch;
            fc.DeactivateUpdate();
            cam = fc.camera;

            // Save old camera state.
            oldCamSettingsObj = new GameObject("KerbCam saved camera settings");
            oldCamSettings = oldCamSettingsObj.AddComponent<Camera>();
            oldCamSettings.CopyFrom(cam);
            oldCamSettings.enabled = false;
            oldCamPos = cam.transform.localPosition;
            oldCamRot = cam.transform.localRotation;
            oldCamTrnParent = cam.transform.parent;

            // Replace with our own state.
            camTrnObj = new GameObject("KerbCam transform");
            cam.transform.parent = camTrnObj.transform;
            camTrnObj.transform.parent = FlightGlobals.ActiveVessel.transform;
            cam.enabled = true;
        }

        public void StopControlling() {
            if (!isControlling) {
                return;
            }
            isControlling = false;

            if (curClient != null) {
                curClient.LoseController();
                curClient = null;
            }

            // Restore old camera state.
            cam.CopyFrom(oldCamSettings);
            cam.transform.localPosition = oldCamPos;
            cam.transform.localRotation = oldCamRot;
            cam.transform.parent = oldCamTrnParent;
            cam.enabled = true;

            // Throw away old references.
            oldCamTrnParent = null;
            GameObject.Destroy(oldCamSettingsObj);
            oldCamSettingsObj = null;
            oldCamSettings = null;
            GameObject.Destroy(camTrnObj);
            camTrnObj = null;

            var fc = FlightCamera.fetch;
            fc.ActivateUpdate();
        }
    }
}
