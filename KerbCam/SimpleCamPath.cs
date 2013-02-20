using System;
using UnityEngine;
using KSP.IO;

namespace KerbCam
{
	public class SimpleCamPath
	{
		private int keyIndex = 0;
		private AnimationCurve hdgCurve = new AnimationCurve();
		private AnimationCurve pitchCurve = new AnimationCurve();
		private AnimationCurve distanceCurve = new AnimationCurve();
		private bool isRunning = false;
		private float curTime = 0.0F;

		private string name;
		private FlightCamera.Modes cameraMode;

		public SimpleCamPath(String name, FlightCamera.Modes cameraMode)
		{
			this.name = name;
			this.cameraMode = cameraMode;
		}

		public bool IsRunning {
			get { return isRunning; }
		}

		public string Name {
			get { return name; }
			set { this.name = value; }
		}

		public FlightCamera.Modes CameraMode {
			get { return cameraMode; }
		}

		public int NumKeys {
			get { return hdgCurve.length; }
		}

		public void AddKey()
		{
			var cam = FlightCamera.fetch;
			// TODO: Proper GUI etc. so that timings can be tweaked.
			// For now, each key is one second apart.
			hdgCurve.AddKey(keyIndex, cam.camHdg);
			pitchCurve.AddKey(keyIndex, cam.camPitch);
			distanceCurve.AddKey(keyIndex, cam.Distance);
			keyIndex++;
		}

		public float TimeAt(int index)
		{
			if (index >= hdgCurve.length) {
				return -1;
			}

			return hdgCurve[index].time;
		}

		public void MoveCameraToKey(int index)
		{
			if (index >= hdgCurve.length) {
				return;
			}
			SetCamera(hdgCurve[index].value,
			          pitchCurve[index].value,
			          distanceCurve[index].value);
		}

		public void RemoveKey(int index)
		{
			if (index >= hdgCurve.length) {
				return;
			}
			hdgCurve.RemoveKey(index);
			pitchCurve.RemoveKey(index);
			distanceCurve.RemoveKey(index);
		}

		public void ToggleRunning ()
		{
			if (!isRunning)
				StartRunning();
			else
				StopRunning();
		}
		
		public void StartRunning()
		{
			if (hdgCurve.length == 0) {
				return;
			}

			isRunning = true;
			curTime = 0F;
			UpdateCamera();
		}
		
		public void StopRunning()
		{
			isRunning = false;
		}

		public void Update()
		{
			if (!isRunning)
				return;

			curTime += Time.deltaTime;
			var maxTime = hdgCurve[hdgCurve.length - 1].time;
			if (curTime >= maxTime) {
				curTime = maxTime;
				StopRunning();
			}

			UpdateCamera();
		}
		
		private void UpdateCamera()
		{
			SetCamera(hdgCurve.Evaluate(curTime),
			          pitchCurve.Evaluate(curTime),
			          distanceCurve.Evaluate(curTime));
		}

		public SimpleCamPathEditor MakeEditor()
		{
			return new SimpleCamPathEditor(this);
		}

		private void SetCamera(float hdg, float pitch, float distance)
		{
			var cam = FlightCamera.fetch;
			cam.mode = cameraMode;
			cam.camHdg = hdg;
			cam.camPitch = pitch;
			cam.SetDistance(distance);
		}
	}

	public class SimpleCamPathEditor
	{
		private Vector2 scrollPosition = new Vector2(0, 0);

		private SimpleCamPath path;

		public SimpleCamPathEditor(SimpleCamPath path)
		{
			this.path = path;
		}

		public bool IsForPath(SimpleCamPath path)
		{
			return this.path == path;
		}

		public void DoGUI()
		{
			GUILayout.BeginVertical();
			GUILayout.Label("Simple camera path [" + path.CameraMode + "]");
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name:");
			path.Name = GUILayout.TextField(path.Name);
			GUILayout.EndHorizontal();

			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
			for (int i = 0; i < path.NumKeys; i++) {
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("X", C.DeleteButtonStyle)) {
					path.RemoveKey(i);
				}
				if (GUILayout.Button("View", C.CompactButtonStyle)) {
					path.MoveCameraToKey(i);
				}
				GUILayout.Label(string.Format("#{0} @{1}s", i, path.TimeAt(i)), C.CompactLabelStyle);
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
			
			GUILayout.EndVertical();
		}
	}
}

