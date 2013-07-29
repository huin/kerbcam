using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace KerbCam {
    class DebugUtil {
        public static void LogException(Exception e) {
            try {
                Debug.LogError(e.ToString());
            } catch (Exception) {
                Debug.LogError("KerbCam failed to log an exception");
            }
        }

        public static string NameOfMaybeNull(Component c) {
            return c == null ? "<null>" : c.name;
        }

        public static void LogCameras() {
            // Display full camera list and basic information about each.
            foreach (var cam in Camera.allCameras) {
                LogCamera(cam);
            }
        }

        public static void LogVessel(Vessel v) {
            Log("Vessel name={0}", v.name);
        }

        public static void LogCamera(Camera cam) {
            Log("Camera ID {0} name={1} is_current={2} is_main={3} " +
                "enabled={4} active_self={5} active_hierarchy={6} " +
                "depth={7} tag={8}",
                cam.GetHashCode(),
                cam.name,
                cam == Camera.current,
                cam == Camera.main,
                cam.enabled,
                cam.gameObject.activeSelf,
                cam.gameObject.activeInHierarchy,
                cam.depth,
                cam.tag);
        }

        public static void LogCamerasTransformTree() {
            // Root transform IDs to the root transform of that ID.
            var trnRoots = new Dictionary<int, Transform>();
            // Transform IDs to cameras using that transform.
            var trnCams = new Dictionary<int, List<Camera>>();
            // Transform IDs that are in the ancestry of cameras.
            var trnCamAncestors = new Dictionary<int, bool>();
            foreach (Camera c in Camera.allCameras) {
                Transform root = c.transform.root;
                trnRoots[root.GetInstanceID()] = root;

                int camTrnId = c.transform.GetInstanceID();
                List<Camera> camList;
                if (!trnCams.TryGetValue(camTrnId, out camList)) {
                    trnCams[camTrnId] = camList = new List<Camera>();
                }
                camList.Add(c);

                for (var trn = c.transform; trn != null; trn = trn.parent) {
                    trnCamAncestors[trn.GetInstanceID()] = true;
                }
            }

            var result = new StringBuilder();
            foreach (Transform root in trnRoots.Values) {
                result.AppendLine("--------");
                /*string skipReason = null;
                if (object.ReferenceEquals(root.gameObject, ScreenSafeUI.fetch)) {
                    skipReason = "ScreenSafeUI";
                } else if (root.name == "_UI") {
                    skipReason = "_UI";
                }
                if (skipReason != null) {
                    result.AppendFormat("(skipping {0} transform tree)\n", skipReason);
                    continue;
                }*/
                AppendCameraTransform(result, 0, root, trnCams, trnCamAncestors);
            }
            Debug.Log(result.ToString());
        }

        private static void AppendCameraTransform(
            StringBuilder result, int level, Transform trn,
            Dictionary<int, List<Camera>> trnCams, Dictionary<int, bool> trnCamAncestors) {

            bool isAncestor = trnCamAncestors.ContainsKey(trn.GetInstanceID());

            Component[] cmps = trn.gameObject.GetComponents<Component>();
            string[] cmpStrs = new string[cmps.Length];
            for (int i = 0; i < cmps.Length; i++) {
                cmpStrs[i] = cmps[i].GetType().Name;
            }

            result.Append('+', level);
            result.AppendFormat("{0} [{1}]", trn.name, string.Join(", ", cmpStrs));
            if (isAncestor) {
                result.Append(' ');
                result.Append(Format(trn));
                result.AppendLine();
                // Only descend into descendents of transforms that are ancestors of
                // cameras. This logs helpful context for camera transforms, without
                // showing excessive internal model information.
                int numChildTrns = trn.GetChildCount();
                foreach (Transform child in trn) {
                    AppendCameraTransform(result, level + 1, child, trnCams, trnCamAncestors);
                }
                return;
            }
            
            if (trn.GetChildCount() > 0) {
                result.Append(" (descendents hidden)");
            }
            result.AppendLine();
        }

        public static string Format(Transform trn) {
            if (trn == null) {
                return "<null>";
            } else {
                return String.Format(
                    "name={0} locPos={1} locRot={2}",
                    trn.name, trn.localPosition, trn.localRotation);
            }
        }

        public static void LogTransformAscestry(Transform trn) {
            var result = new StringBuilder();
            int i = 0;
            for (; trn != null; trn = trn.parent, i++) {
                result.AppendFormat("#{0} {1}\n", i, Format(trn));
            }
            Log(result.ToString());
        }

        public static void Log(string fmt, params object[] args) {
            Debug.Log(string.Format(fmt, args));
        }
    }
}
