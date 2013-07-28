using UnityEngine;

namespace KerbCam {
    public struct TransformState {
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

        /// <summary>
        /// Moves toMove, retaining the local position, scale, rotation.
        /// 
        /// This is in contrast to simply modifying Transform.parent, which modifies
        /// the local transformation state to retain the same world-space result.
        /// </summary>
        /// <param name="toMove">The Transform to reparent.</param>
        /// <param name="newParent">The new parent Transform.</param>
        public static void MoveToParent(Transform toMove, Transform newParent) {
            DebugUtil.Log("transform==null => {0}", toMove==null);
            var state = new TransformState(toMove);
            DebugUtil.Log("transform {0} before: {1}", toMove.name, toMove.localPosition);
            toMove.parent = newParent;
            DebugUtil.Log("transform {0} after reparent: {1}", toMove.name, toMove.localPosition);
            state.Revert();
            DebugUtil.Log("transform {0} after local revert: {1}", toMove.name, toMove.localPosition);
        }
    }
}
