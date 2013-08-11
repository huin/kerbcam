using UnityEngine;

namespace KerbCam {
    /// <summary>
    /// Behaviour object that signals when it is destroyed.
    /// </summary>
    /// This is intended for use when introducing an object into an object
    /// hierarchy and detecting destruction of a parent object prior to it
    /// reaching a child.
    class DestructionProxy : MonoBehaviour {
        public delegate void DestructionDelegate(DestructionProxy p);
        public event DestructionDelegate onDestroy;

        public void OnDestroy() {
            if (onDestroy != null) {
                onDestroy(this);
            }
        }
    }
}
