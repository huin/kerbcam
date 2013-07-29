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
                RaycastHit hitInfo = new RaycastHit();
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo)) {
                    onPicked(hitInfo.collider);
                }
            }
        }
    }
}
