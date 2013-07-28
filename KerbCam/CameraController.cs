using UnityEngine;

namespace KerbCam {
    public class CameraController : MonoBehaviour {

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

        private Client curClient = null;
        private Transform relativeTrn = null;

        public CameraController() {
            UpdateParentTransform();
            GameEvents.onVesselChange.Add(delegate(Vessel v) {
                // Vessel selection changed, update parent transform if necessary.
                UpdateParentTransform();

                if (isControlling) {
                    FlightCamera.fetch.DeactivateUpdate();
                }
            });
        }

        /// <summary>
        /// The "outer"/"first" camera transform. This is the parent of SecondTransform.
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
                if (!object.ReferenceEquals(relativeTrn, value)) {
                    relativeTrn = value;
                    UpdateParentTransform();
                }
            }
        }

        private void UpdateParentTransform() {
            if (relativeTrn == null) {
                TransformState.MoveToParent(transform, FlightGlobals.ActiveVessel.transform);
            } else {
                TransformState.MoveToParent(transform, relativeTrn);
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
            // Will need to update the delegate in the constructor if so.
            // CameraManager is particularly of note.
            var fc = FlightCamera.fetch;
            fc.DeactivateUpdate();

            // Acquire the first and second transforms, saving their start for when we
            // stop controlling.

            // For FlightCamera, the transform parent is the "main camera pivot".
            firstTrn = new TransformState(fc.transform.parent);
            // ... and the transform is that for the FlightCamera itself.
            secondTrn = new TransformState(fc.transform);

            // The CameraController becomes the parent transform for the camera transforms.
            TransformState.MoveToParent(firstTrn.Transform, transform);
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
                TransformState.MoveToParent(firstTrn.Transform, FlightGlobals.ActiveVessel.transform);
                TransformState.MoveToParent(secondTrn.Transform, firstTrn.Transform);
                FlightCamera.fetch.ActivateUpdate();
            }
        }
    }
}
