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
        internal delegate void StateChangeHandler(int id, bool newState);

        private bool guiState = false;
        private bool keyState = false;

        private int id;
        private StateChangeHandler stateListener;

        internal void SetIdAndListener(int id, StateChangeHandler stateListener) {
            this.id = id;
            this.stateListener = stateListener;
        }

        public bool State {
            get { return guiState || keyState; }
        }

        private void FireState(bool oldState) {
            if (stateListener == null) {
                return;
            }
            bool newState = State;
            if (oldState != newState) {
                stateListener(id, newState);
            }
        }

        internal void SetGuiState(bool state) {
            bool oldState = State;
            guiState = state;
            FireState(oldState);
        }

        internal void HandleKeyUp() {
            bool oldState = State;
            keyState = false;
            FireState(oldState);
        }

        internal void HandleKeyDown() {
            bool oldState = State;
            keyState = true;
            FireState(oldState);
        }

        internal abstract void AddMove(Quaternion rot, Transform translateTrn, float translationFactor,
            Transform rotationTrn, float rotationFactor);
    }

    internal class TranslateMove : ManualMove {
        private Vector3 direction;

        // Move 10 units per second.
        private const float BASE_TRANSLATE_SPEED = 10f;

        internal TranslateMove(Vector3 direction) {
            this.direction = direction;
        }

        internal override void AddMove(Quaternion rot, Transform translateTrn, float translationFactor,
            Transform rotationTrn, float rotationFactor) {

            if (State) {
                translateTrn.localPosition += rot * direction * translationFactor;
            }
        }
    }

    internal class RotateMove : ManualMove {
        private Vector3 axis;

        // Rotate 20 degrees per second.
        private const float BASE_ROTATE_SPEED = 20f;

        internal RotateMove(Vector3 axis) {
            this.axis = axis;
        }

        internal override void AddMove(Quaternion rot, Transform translateTrn, float translationFactor,
            Transform rotationTrn, float rotationFactor) {

            if (State) {
                Quaternion newRot = rotationTrn.localRotation
                    * Quaternion.AngleAxis(
                    BASE_ROTATE_SPEED * rotationFactor, axis);

                QuatUtil.Normalize(ref newRot);
                rotationTrn.localRotation = newRot;
            }
        }
    }

    public class ManualCameraControl : MonoBehaviour, CameraController.Client {
        private float lastSeenTime = -1f;

        public float translateSliderPosition = 0f;
        public float rotateSliderPosition = 0f;

        /// <summary>
        /// Bitfield of each of the move states. If non-zero, then at least one move
        /// is active. This can track up to 32 states - we use only 12.
        /// </summary>
        private int moveStates = 0;

        private GameObject ownerObject;
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

        public static ManualCameraControl Create() {
            var ownerObject = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(ownerObject);
            var mc = ownerObject.AddComponent<ManualCameraControl>();
            mc.ownerObject = ownerObject;

            mc.TrnUp = mc.AddTrn(Vector3.up, BoundKey.KEY_TRN_UP);
            mc.TrnForward = mc.AddTrn(Vector3.forward, BoundKey.KEY_TRN_FORWARD);
            mc.TrnLeft = mc.AddTrn(Vector3.left, BoundKey.KEY_TRN_LEFT);
            mc.TrnRight = mc.AddTrn(Vector3.right, BoundKey.KEY_TRN_RIGHT);
            mc.TrnDown = mc.AddTrn(Vector3.down, BoundKey.KEY_TRN_DOWN);
            mc.TrnBackward = mc.AddTrn(Vector3.back, BoundKey.KEY_TRN_BACKWARD);

            mc.RotRollLeft = mc.AddRot(Vector3.forward, BoundKey.KEY_ROT_ROLL_LEFT);
            mc.RotUp = mc.AddRot(Vector3.left, BoundKey.KEY_ROT_UP);
            mc.RotRollRight = mc.AddRot(Vector3.back, BoundKey.KEY_ROT_ROLL_RIGHT);
            mc.RotLeft = mc.AddRot(Vector3.down, BoundKey.KEY_ROT_LEFT);
            mc.RotRight = mc.AddRot(Vector3.up, BoundKey.KEY_ROT_RIGHT);
            mc.RotDown = mc.AddRot(Vector3.right, BoundKey.KEY_ROT_DOWN);

            mc.useGUILayout = false;
            mc.enabled = false;
            return mc;
        }

        private ManualMove AddTrn(Vector3 trn, BoundKey binding) {
            return AddMove(new TranslateMove(trn), binding);
        }

        private ManualMove AddRot(Vector3 axis, BoundKey binding) {
            return AddMove(new RotateMove(axis), binding);
        }

        private ManualMove AddMove(ManualMove move, BoundKey binding) {
            int id = moves.Count;
            move.SetIdAndListener(id, HandleMoveStateChange);
            moves.Add(move);
            State.keyBindings.ListenKeyUp(binding, move.HandleKeyUp);
            State.keyBindings.ListenKeyDown(binding, move.HandleKeyDown);
            return move;
        }

        public void TakeControl() {
            State.camControl.StartControlling(this);
            enabled = true;
        }

        public void LoseControl() {
            State.camControl.StopControlling(true);
            enabled = false;
        }

        public void HandleMoveStateChange(int id, bool newState) {
            // Update moveStates.
            int bit = 1 << id;
            if (newState) {
                TakeControl();
                // Set the bit - the move is currently active.
                moveStates |= bit;
            } else {
                // Unset the bit - the move is currently inactive.
                moveStates &= ~bit;
            }

            // Cause Update() calls only if something is pressed.
            enabled = moveStates != 0;
            if (!enabled) {
                lastSeenTime = -1f;
            }
        }

        public void Update() {
            try {
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

                float deltaTime = DeltaTime();
                float trnFactor = TranslationFactor(deltaTime);
                float rotFactor = RotationFactor(deltaTime);

                foreach (var move in moves) {
                    if (move.State) {
                        move.AddMove(rot, translateTrn, trnFactor, rotationTrn, rotFactor);
                    }
                }
            } catch (Exception e) {
                DebugUtil.LogException(e);
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
            enabled = false;
        }
    }
}
