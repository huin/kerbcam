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
            UpdateTransformReferences();

            // TODO: See if we can retain camera position if we're relative to a vessel that is destroyed.
            GameEvents.onGameSceneLoadRequested.Add(delegate(GameScenes s) {
                try {
                    UpdateTransformReferences();
                    // Put the camera back where it was and stop meddling.
                    StopControlling();
                    // Remove the CameraController from the hierarchy,
                    // otherwise it'll be destroyed.
                    transform.parent = null;
                } catch (Exception e) {
                    DebugUtil.LogException(e);
                }
            });
            GameEvents.onVesselChange.Add(delegate(Vessel v) {
                try {
                    // Vessel selection changed, update references as necessary.
                    UpdateTransformReferences();

                    ReacquireFlightCamera();
                    // We need to re-acquire the flight camera at the end of the frame as
                    // well (it looks like the transform parent gets reset after this
                    // callback returns).
                    StartCoroutine(DeferredReacquireFlightCamera());
                } catch (Exception e) {
                    DebugUtil.LogException(e);
                }
            });
        }

        private IEnumerator DeferredReacquireFlightCamera() {
            yield return new WaitForEndOfFrame();
            ReacquireFlightCamera();
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
                    UpdateTransformReferences();
                }
            }
        }

        /// <summary>
        /// Updates references to Transform objects that we're interested in.
        /// </summary>
        private void UpdateTransformReferences() {
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

            // For FlightCamera, the second transform is that for the FlightCamera itself.
            secondTrn = new TransformState(FlightCamera.fetch.transform);
            // ... and the transform parent is the "main camera pivot".
            firstTrn = new TransformState(secondTrn.Transform.parent);
        }

        public bool IsControlling {
            get { return isControlling; }
        }

        private void AcquireFlightCamera() {
            if (isControlling) {
                return;
            }
            isControlling = true;
            ReacquireFlightCamera();
        }

        private void ReacquireFlightCamera() {
            if (!isControlling) {
                return;
            }

            FlightCamera.fetch.DeactivateUpdate();

            // Store the first and second transform states, for when we release the camera.
            firstTrn.Store();
            secondTrn.Store();

            // The CameraController becomes the parent transform for the camera transforms.
            TransformState.MoveToParent(firstTrn.Transform, transform);
        }

        private void ReleaseFlightCamera() {
            if (!isControlling) {
                return;
            }
            isControlling = false;

            // Restore parentage of first and second transforms. This mutates their state
            // to retain their current position, so revert must be *after* this.
            firstTrn.Transform.parent = (FlightGlobals.ActiveVessel == null ?
                null : FlightGlobals.ActiveVessel.transform);
            secondTrn.Transform.parent = firstTrn.Transform; // Probably hasn't changed anyway.

            firstTrn.Revert();
            secondTrn.Revert();

            FlightCamera.fetch.ActivateUpdate();
        }

        private void LoseClient() {
            Client c = curClient;
            // Prevent infinite recursion in case the client calls StartControlling StopControlling
            // again.
            curClient = null;
            c.LoseController();
        }

        public void StartControlling(Client client) {
            if (curClient != null && client != curClient) {
                LoseClient();
            }

            curClient = client;

            // TODO: Consider being able to move InternalCamera as well.
            // Will need to update the delegate in the constructor if so.
            // CameraManager is particularly of note.
            AcquireFlightCamera();
        }

        public void StopControlling() {
            if (curClient != null) {
                LoseClient();
            }

            ReleaseFlightCamera();
        }
    }
}
