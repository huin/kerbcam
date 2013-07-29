using System;
using System.Collections;
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

        public void OnDestroy() {
            Debug.LogWarning("KerbCam.CameraController was destroyed");
        }

        public CameraController() {
            UpdateParentTransform();

            // TODO: See if we can retain camera position if we're relative to a vessel that is destroyed.
            GameEvents.onGameSceneLoadRequested.Add(delegate(GameScenes s) {
                try {
                    DetachFromHierarchy();
                } catch (Exception e) {
                    DebugUtil.LogException(e);
                }
            });
            GameEvents.onVesselChange.Add(delegate(Vessel v) {
                try {
                    if (!isControlling) {
                        return;
                    }

                    // Vessel selection changed, update parent transform if necessary.
                    UpdateParentTransform();

                    AcquireFlightCamera();
                    // We need to re-acquire the flight camera at the end of the frame as
                    // well (it looks like the transform parent gets reset after this
                    // callback returns).
                    StartCoroutine(DeferredAcquireFlightCamera());
                } catch (Exception e) {
                    DebugUtil.LogException(e);
                }
            });
        }

        private IEnumerator DeferredAcquireFlightCamera() {
            yield return new WaitForEndOfFrame();
            AcquireFlightCamera();
        }

        private void DetachFromHierarchy() {
            TransformState.MoveToParent(transform, null);
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
                    DebugUtil.Log("transform: {0}", DebugUtil.Format(transform));
                    DebugUtil.Log("new rel transform: {0}", DebugUtil.Format(relativeTrn));
                    UpdateParentTransform();
                }
            }
        }

        private void UpdateParentTransform() {
            Transform newParent;
            if (relativeTrn == null) {
                if (FlightGlobals.ActiveVessel == null) {
                    newParent = null;
                } else {
                    newParent = FlightGlobals.ActiveVessel.transform;
                }
            } else {
                newParent = relativeTrn;
            }
            TransformState.MoveToParent(transform, newParent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
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
            AcquireFlightCamera();

            UpdateParentTransform();
        }

        private void AcquireFlightCamera() {
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

        private void ReleaseFlightCamera() {
            firstTrn.Revert();
            secondTrn.Revert();
            TransformState.MoveToParent(firstTrn.Transform, FlightGlobals.ActiveVessel.transform);
            TransformState.MoveToParent(secondTrn.Transform, firstTrn.Transform);
            FlightCamera.fetch.ActivateUpdate();
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

            ReleaseFlightCamera();
        }
    }
}
