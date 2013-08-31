using UnityEngine;

namespace KerbCam {
    class ObjectPicker : MonoBehaviour {
        public delegate void ObjectPicked(Collider c);

        private event ObjectPicked onPicked;

        public void AddObjectPicked(ObjectPicked d) {
            onPicked += d;
            enabled = true;
        }

        public void RemoveObjectPicked(ObjectPicked d) {
            onPicked -= d;
            if (onPicked == null) {
                enabled = false;
            }
        }

        public void Update() {
            Event ev = Event.current;
            if (onPicked != null && ev.isMouse && ev.clickCount == 1 && ev.button == 0) {
                RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));

                Collider nearest = null;
                float nearestDist = float.MaxValue;

                for (int i = 0; i < hits.Length; i++) {
                    if (nearest == null || hits[i].distance < nearestDist) {
                        nearest = hits[i].collider;
                    }
                }

                if (nearest != null) {
                    onPicked(nearest);
                }
            }
        }
    }
}
