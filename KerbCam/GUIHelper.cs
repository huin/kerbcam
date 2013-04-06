using System;
using UnityEngine;

namespace KerbCam {
    class C {
        public const string ChrTimes = "\u00d7";

        private const int WinButtonSize = 25;

        private static bool intialized = false;

        public static GUIStyle DeleteButtonStyle;
        public static GUIStyle DisabledButtonStyle;
        public static GUIStyle LinkButtonStyle;
        public static GUIStyle UnpaddedButtonStyle;
        public static GUIStyle WindowButtonStyle;
        public static GUIStyle FoldButtonStyle;

        public static void Init() {
            if (intialized) {
                return;
            }
            intialized = true;

            GUISkin skin = HighLogic.Skin;

            DeleteButtonStyle = new GUIStyle(skin.button);
            DeleteButtonStyle.active.textColor = Color.red;
            DeleteButtonStyle.focused.textColor = Color.red;
            DeleteButtonStyle.hover.textColor = Color.red;
            DeleteButtonStyle.normal.textColor = Color.red;

            DisabledButtonStyle = new GUIStyle(skin.button);
            Color disabledTextColor = new Color(0.7f, 0.7f, 0.7f);
            DisabledButtonStyle.active.textColor = disabledTextColor;
            DisabledButtonStyle.focused.textColor = disabledTextColor;
            DisabledButtonStyle.hover.textColor = disabledTextColor;
            DisabledButtonStyle.normal.textColor = disabledTextColor;

            LinkButtonStyle = new GUIStyle(skin.button);
            var linkColor = new Color(0.8f, 0.8f, 1f, 1f);
            LinkButtonStyle.active.textColor = linkColor;
            LinkButtonStyle.focused.textColor = linkColor;
            LinkButtonStyle.hover.textColor = linkColor;
            LinkButtonStyle.normal.textColor = new Color(0f, 0f, 0.7f);

            UnpaddedButtonStyle = new GUIStyle(skin.button);
            UnpaddedButtonStyle.margin = new RectOffset(0, 0, 0, 0);
            UnpaddedButtonStyle.padding = new RectOffset(0, 0, 0, 0);

            WindowButtonStyle = new GUIStyle(skin.button);
            WindowButtonStyle.fixedHeight = WinButtonSize;
            WindowButtonStyle.fixedWidth = WinButtonSize;
            WindowButtonStyle.alignment = TextAnchor.LowerCenter;
            WindowButtonStyle.border = new RectOffset(1, 1, 1 ,1);
            WindowButtonStyle.margin = new RectOffset(2, 2, 8, 2);
            WindowButtonStyle.padding = new RectOffset(2, 2, 2, 2);
            var border = new Color(1f, 1f, 1f, 0.7f);
            WindowButtonStyle.active.background = MakeWindowButtonTexture(
                border, new Color(1f, 1f, 1f, 0.3f));
            WindowButtonStyle.focused.background = MakeWindowButtonTexture(
                border, new Color(1f, 1f, 1f, 0.2f));
            WindowButtonStyle.hover.background = MakeWindowButtonTexture(
                border, new Color(1f, 1f, 1f, 0.2f));
            WindowButtonStyle.normal.background = MakeWindowButtonTexture(
                border, new Color(1f, 1f, 1f, 0.1f));

            FoldButtonStyle = new GUIStyle(skin.button);
            FoldButtonStyle.alignment = TextAnchor.MiddleLeft;
            FoldButtonStyle.border = new RectOffset(0, 0, 0, 0);
            FoldButtonStyle.active.background = MakeConstantTexture(
                new Color(1f, 1f, 1f, 0.2f));
            FoldButtonStyle.focused.background = MakeConstantTexture(
                new Color(1f, 1f, 1f, 0.2f));
            FoldButtonStyle.hover.background = MakeConstantTexture(
                new Color(1f, 1f, 1f, 0.2f));
            FoldButtonStyle.normal.background = MakeConstantTexture(
                Color.clear);
        }

        private static Texture2D MakeConstantTexture(Color fill) {
            const int size = 20;
            Texture2D txt = new Texture2D(size, size);
            for (int row = 0; row < size; row++) {
                for (int col = 0; col < size; col++) {
                    txt.SetPixel(col, row, fill);
                }
            }
            txt.Apply();
            txt.Compress(false);
            return txt;
        }

        private static Texture2D MakeWindowButtonTexture(Color border, Color fill) {
            const int size = WinButtonSize;
            Texture2D txt = new Texture2D(size, size);
            for (int i = 0; i < size; i++) {
                txt.SetPixel(i, 0, border);
                txt.SetPixel(i, size - 1, border);
                txt.SetPixel(0, i, border);
                txt.SetPixel(size - 1, i, border);
            }
            for (int row = 1; row < size - 1; row++) {
                for (int col = 1; col < size - 1; col++) {
                    txt.SetPixel(col, row, fill);
                }
            }
            txt.Apply();
            txt.Compress(false);
            return txt;
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

            Rect r = GUILayoutUtility.GetRect(gcDrag, C.WindowButtonStyle);

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

            GUI.Button(r, gcDrag, C.WindowButtonStyle);
        }
    }

    abstract class BaseWindow {
        private bool isWindowOpen = false;
        protected int windowId;
        private Callback drawCallback;

        public BaseWindow() {
            windowId = GetHashCode();
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

        protected void DoCloseButton() {
            if (GUILayout.Button(C.ChrTimes, C.WindowButtonStyle)) {
                HideWindow();
            }
        }

        protected abstract void DrawGUI();
    }
}