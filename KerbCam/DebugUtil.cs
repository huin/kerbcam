using UnityEngine;

namespace KerbCam {
    class DebugUtil {
        public static void LogCameras() {
            // Display full camera list and basic information about each.
            foreach (var cam in Camera.allCameras) {
                Log("is_current={0} enabled={1} depth={2} name={3} is_main={4} is_flight={5}",
                    cam == Camera.current,
                    cam.enabled,
                    cam.depth,
                    cam.name,
                    cam == Camera.main,
                    cam == FlightCamera.fetch.camera);
            }
        }

        public static void LogVessel(Vessel v) {
            Log("Vessel name={0}", v.name);
            LogTransformAscestry(v.transform);
        }

        public static void LogCamera(Camera c) {
            Log("Camera name={0}", c.name);
            LogTransformAscestry(c.transform);
        }

        public static void LogTransformAscestry(Transform trn) {
            int i = 0;
            for (; trn != null; trn = trn.parent, i++) {
                Log("#{0} locPos={1} locRot={2} pos={3} rot={4}",
                    i, trn.localPosition, trn.localRotation,
                    trn.position, trn.rotation);
            }
        }

        public static void Log(string fmt, params object[] args) {
            Debug.Log(string.Format(fmt, args));
        }
    }
}
