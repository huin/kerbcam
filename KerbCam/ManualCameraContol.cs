using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbCam {

    public enum ManualMoveType {
        TrnUp, TrnDown,
        TrnLeft, TrnRight,
        TrnForward, TrnBackward,

        RotUp, RotDown,
        RotLeft, RotRight,
        RotRollLeft, RotRollRight,
    }

    public abstract class ManualMove {
        private bool guiState = false;
        private bool keyState = false;

        public bool State {
            get { return guiState || keyState; }
        }

        internal void SetGuiState(bool state) {
            guiState = state;
        }

        internal void HandleKeyUp() {
            keyState = false;
        }

        internal void HandleKeyDown() {
            keyState = true;
        }

        internal abstract void AddMove(Transform translateTrn, float translationFactor,
            Transform rotationTrn, float rotationFactor);
    }

    internal class TranslateMove : ManualMove {
        private Vector3 direction;

        // Move 10 units per second.
        private const float BASE_TRANSLATE_SPEED = 10f;

        internal TranslateMove(Vector3 direction) {
            this.direction = direction;
        }

        internal override void AddMove(Transform translateTrn, float translationFactor,
            Transform rotationTrn, float rotationFactor) {
            translateTrn.localPosition += direction * translationFactor;
        }
    }

    internal class RotateMove : ManualMove {
        private Vector3 axis;

        // Rotate 20 degrees per second.
        private const float BASE_ROTATE_SPEED = 20f;

        internal RotateMove(Vector3 axis) {
            if (State) {
                this.axis = axis;
            }
        }

        internal override void AddMove(Transform translateTrn, float translationFactor,
            Transform rotationTrn, float rotationFactor) {
                Quaternion rot = rotationTrn.localRotation
                    * Quaternion.AngleAxis(
                    BASE_ROTATE_SPEED * rotationFactor, axis);

            QuatUtil.Normalize(ref rot);
            rotationTrn.localRotation = rot;
        }
    }

    public class ManualCameraControl : MonoBehaviour, CameraController.Client {
        private float lastSeenTime = -1f;

        public float translateSliderPosition = 0f;
        public float rotateSliderPosition = 0f;

        public ManualMove TrnUp;
        public ManualMove TrnForward;
        public ManualMove TrnLeft;
        public ManualMove TrnRight;
        public ManualMove TrnDown;
        public ManualMove TrnBackward;
        public ManualMove RotRollLeft;
        public ManualMove RotUp;
        public ManualMove RotRollRight;
        public ManualMove RotLeft;
        public ManualMove RotRight;
        public ManualMove RotDown;

        private List<ManualMove> moves = new List<ManualMove>(12);

        public ManualCameraControl Create() {
            var mc = new ManualCameraControl();
            mc.enabled = false;

            TrnUp = mc.AddMove(new TranslateMove(Vector3.up));
            TrnForward = mc.AddMove(new TranslateMove(Vector3.forward));
            TrnLeft = mc.AddMove(new TranslateMove(Vector3.left));
            TrnRight = mc.AddMove(new TranslateMove(Vector3.right));
            TrnDown = mc.AddMove(new TranslateMove(Vector3.down));
            TrnBackward = mc.AddMove(new TranslateMove(Vector3.back));

            RotRollLeft = mc.AddMove(new RotateMove(Vector3.forward));
            RotUp = mc.AddMove(new RotateMove(Vector3.left));
            RotRollRight = mc.AddMove(new RotateMove(Vector3.back));
            RotLeft = mc.AddMove(new RotateMove(Vector3.down));
            RotRight = mc.AddMove(new RotateMove(Vector3.up));
            RotDown = mc.AddMove(new RotateMove(Vector3.right));

            return mc;
       }

        private ManualMove AddMove(ManualMove move) {
            moves.Add(move);
            return move;
        }

        public void TakeControl() {
            State.camControl.StartControlling(this);
            enabled = true;
        }

        public void LoseControl() {
            State.camControl.StopControlling();
            enabled = false;
        }

        public void Update() {
            // TODO find if anything is pressed, somehow

            var cc = State.camControl;
            // Even if the controller is already controlling, take control
            // with this GUI. This stops any path from moving the camera if it
            // was controlling the camera.
            cc.StartControlling(this);

            if (!cc.IsControlling) {
                return;
            }

            Transform rotationTrn = cc.Camera.transform;
            Transform translateTrn = rotationTrn.parent;

            Quaternion rot = Quaternion.Inverse(rotationTrn.root.localRotation) * rotationTrn.rotation;
            Vector3 forward = rot * Vector3.forward;
            Vector3 up = rot * Vector3.up;
            Vector3 right = rot * Vector3.right;

            float deltaTime = DeltaTime();
            float trnFactor = TranslationFactor(deltaTime);
            float rotFactor = RotationFactor(deltaTime);

            bool haveMoved = false;
            foreach (var move in moves) {
                if (move.State) {
                    haveMoved = true;
                    move.AddMove(translateTrn, trnFactor, rotationTrn, rotFactor);
                }
            }

            if (!haveMoved) {
                lastSeenTime = -1f;
                return;
            }
        }

        private float TranslationFactor(float deltaTime) {
            return deltaTime * (float)Math.Exp(translateSliderPosition);
        }

        private float RotationFactor(float deltaTime) {
            return deltaTime * (float)Math.Exp(rotateSliderPosition);
        }

        private float DeltaTime() {
            float deltaTime;
            float worldTime = Time.realtimeSinceStartup;
            if (lastSeenTime < 0f) {
                deltaTime = 0f;
                lastSeenTime = worldTime;
            } else {
                deltaTime = worldTime - lastSeenTime;
                lastSeenTime = worldTime;
            }
            return deltaTime;
        }

        private static void RotateTransformRotation(Transform trn, float angle, Vector3 axis) {
            Quaternion rot = trn.localRotation * Quaternion.AngleAxis(angle, axis);
            QuatUtil.Normalize(ref rot);
            trn.localRotation = rot;
        }

        void CameraController.Client.LoseController() {
        }
    }
}
