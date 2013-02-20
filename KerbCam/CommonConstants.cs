using System;
using UnityEngine;

namespace KerbCam
{
	class C {
		private static bool guiConstantsInitialized = false;

		public static GUIStyle CompactLabelStyle;
		public static GUIStyle CompactButtonStyle;
		public static GUIStyle DeleteButtonStyle;

		public static void InitGUIConstants()
		{
			try {
				if (guiConstantsInitialized) {
					return;
				}

				var compactPadding = new RectOffset(1, 1, 1, 1);

				CompactLabelStyle = new GUIStyle(GUI.skin.label);
				CompactLabelStyle.padding = compactPadding;

				CompactButtonStyle = new GUIStyle(GUI.skin.button);
				CompactButtonStyle.padding = compactPadding;

				DeleteButtonStyle = new GUIStyle(CompactButtonStyle);
				DeleteButtonStyle.normal.textColor = new Color(1F, 0F, 0F);

				Debug.Log("KerbCam constants initialized");
				Debug.LogWarning("KerbCam constants initialized");
				Debug.LogError("KerbCam constants initialized");

				guiConstantsInitialized = true;
			} catch (Exception e) {
				Debug.LogError(e);
			}
		}
	}
}