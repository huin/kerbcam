using UnityEngine;

namespace KerbCam {
    public class CameraController {
        private struct TransformState {
            private Transform trn;
            private Vector3 localPosition;
            private Quaternion localRotation;
            private Vector3 localScale;

            public TransformState(Transform trn) {
                this.trn = trn;
                localPosition = trn.localPosition;
                localRotation = trn.localRotation;
                localScale = trn.localScale;
            }

            public Transform Transform {
                get { return trn; }
            }

            public void Revert() {
                if (trn == null) {
                    DebugUtil.Log("Attempted to revert a null transform");
                    return;
                }
                trn.localPosition = localPosition;
                trn.localRotation = localRotation;
                trn.localScale = localScale;
            }
        }

        public interface Client {
            /// <summary>
            /// Called by the controller when another client acquires the
            /// controller, or the controller stops controlling a camera.
            /// </summary>
            void LoseController();
        }

        private bool isControlling = false;

        private TransformState firstTrn;
        private TransformState secondTrn;

        private GameObject camTrnObj = null;
        private Client curClient = null;
        private Transform relativeTrn = null;

        /// <summary>
        /// The "outer"/"first" camera transform.
        /// </summary>
        public Transform FirstTransform {
            get { return firstTrn.Transform; }
        }

        /// <summary>
        /// The "inner"/"second" camera transform.
        /// </summary>
        public Transform SecondTransform {
            get { return secondTrn.Transform; }
        }

        /// <summary>
        /// Sets the transform to move the camera relative to.
        /// If null, then uses the active vessel's transform.
        /// </summary>
        public Transform RelativeTrn {
            get { return relativeTrn; }
            set {
                if (relativeTrn != value) {
                    relativeTrn = value;
                    UpdateParentTransform();
                }
            }
        }

        public Transform EffectiveRelativeTrn {
            get {
                if (relativeTrn == null) return FlightGlobals.ActiveVessel.transform;
                else return relativeTrn;
            }
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

            // TODO: Consider being able to move InternalCamera as well.
            // CameraManager is particularly of note.
            var fc = FlightCamera.fetch;
            fc.DeactivateUpdate();

            // Acquire the first and second transforms, saving their start for when we
            // stop controlling.

            // For FlightCamera, the transform parent is the "main camera pivot".
            firstTrn = new TransformState(fc.transform.parent);
            // ... and the transform is that for the FlightCamera itself.
            secondTrn = new TransformState(fc.transform);
        }

        private void UpdateParentTransform() {
            if (camTrnObj != null) {
                camTrnObj.transform.parent = EffectiveRelativeTrn.transform;
            }
        }

        public void StopControlling(bool restoreCamera) {
            if (!isControlling) {
                return;
            }
            isControlling = false;

            if (curClient != null) {
                curClient.LoseController();
                curClient = null;
            }

            if (restoreCamera) {
                firstTrn.Revert();
                secondTrn.Revert();
                firstTrn.Transform.parent = FlightGlobals.ActiveVessel.transform;
                secondTrn.Transform.parent = firstTrn.Transform;
                FlightCamera.fetch.ActivateUpdate();
            }
        }
    }
}
