using System;
using UnityEngine;

namespace KerbCam {
    class C {
        private static bool guiConstantsInitialized = false;

        public static GUIStyle DeleteButtonStyle;
        public static GUIStyle DisabledButtonStyle;
        public static GUIStyle WindowResizeStyle;
        public static GUIStyle UnpaddedButtonStyle;

        public static void InitGUIConstants() {
            if (guiConstantsInitialized) {
                return;
            }

            Color disabledTextColor = new Color(0.7f, 0.7f, 0.7f);

            DeleteButtonStyle = new GUIStyle(GUI.skin.button);
            DeleteButtonStyle.normal.textColor = new Color(1f, 0f, 0f);

            DisabledButtonStyle = new GUIStyle(GUI.skin.button);
            DisabledButtonStyle.active.textColor = disabledTextColor;
            DisabledButtonStyle.focused.textColor = disabledTextColor;
            DisabledButtonStyle.hover.textColor = disabledTextColor;
            DisabledButtonStyle.normal.textColor = disabledTextColor;

            UnpaddedButtonStyle = new GUIStyle(GUI.skin.button);
            UnpaddedButtonStyle.margin = new RectOffset(0, 0, 0, 0);
            UnpaddedButtonStyle.padding = new RectOffset(0, 0, 0, 0);

            WindowResizeStyle = new GUIStyle(GUI.skin.button);

            guiConstantsInitialized = true;
        }
    }

    public class WindowResizer {
        private static GUIContent gcDrag = new GUIContent("\u25E2");

        private bool isResizing = false;
        private Rect resizeStart = new Rect();

        private Vector2 minSize;
        private Rect position;

        public WindowResizer(Rect windowRect, Vector2 minSize) {
            this.position = windowRect;
            this.minSize = minSize;
        }

        public Rect Position {
            get { return position; }
            set { position = value; }
        }

        public float Width {
            get { return position.width; }
            set { position.width = value; }
        }

        public float Height {
            get { return position.height; }
            set { position.height = value; }
        }

        public float MinWidth {
            get { return minSize.x; }
            set { minSize.x = value; }
        }

        public float MinHeight {
            get { return minSize.y; }
            set { minSize.y = value; }
        }

        // Helpers to return GUILayoutOptions for GUILayout.Window.

        public GUILayoutOption LayoutMinWidth() {
            return GUILayout.MinWidth(minSize.x);
        }

        public GUILayoutOption LayoutMinHeight() {
            return GUILayout.MinHeight(minSize.y);
        }

        // Originally from the following URL and modified:
        // http://answers.unity3d.com/questions/17676/guiwindow-resize-window.html
        public void HandleResize() {
            Vector2 mouse = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

            Rect r = GUILayoutUtility.GetRect(gcDrag, C.WindowResizeStyle);

            if (Event.current.type == EventType.mouseDown && r.Contains(mouse)) {
                isResizing = true;
                resizeStart = new Rect(mouse.x, mouse.y, position.width, position.height);
                //Event.current.Use();  // the GUI.Button below will eat the event, and this way it will show its active state
            } else if (Event.current.type == EventType.mouseUp && isResizing) {
                isResizing = false;
            } else if (!Input.GetMouseButton(0)) {
                // if the mouse is over some other window we won't get an event, this just kind of circumvents that by checking the button state directly
                isResizing = false;
            } else if (isResizing) {
                position.width = Mathf.Max(minSize.x, resizeStart.width + (mouse.x - resizeStart.x));
                position.height = Mathf.Max(minSize.y, resizeStart.height + (mouse.y - resizeStart.y));
                position.xMax = Mathf.Min(Screen.width, position.xMax);  // modifying xMax affects width, not x
                position.yMax = Mathf.Min(Screen.height, position.yMax);  // modifying yMax affects height, not y
            }

            GUI.Button(r, gcDrag, C.WindowResizeStyle);
        }
    }

    abstract class BaseWindow : UnityEngine.Object {
        private bool isWindowOpen = false;
        protected int windowId;
        private Callback drawCallback;

        public BaseWindow() {
            this.windowId = GetInstanceID();
            this.drawCallback = new Callback(DrawGUIWrap);
        }

        public void ToggleWindow() {
            if (isWindowOpen) {
                HideWindow();
            } else {
                ShowWindow();
            }
        }

        public virtual void ShowWindow() {
            isWindowOpen = true;
            RenderingManager.AddToPostDrawQueue(3, drawCallback);
            GUI.FocusWindow(windowId);
        }

        public virtual void HideWindow() {
            isWindowOpen = false;
            RenderingManager.RemoveFromPostDrawQueue(3, drawCallback);
        }

        private void DrawGUIWrap() {
            try {
                DrawGUI();
            } catch (Exception e) {
                DebugUtil.LogException(e);
            }
        }

        protected abstract void DrawGUI();
    }
}