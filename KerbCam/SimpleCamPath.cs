using System;
using UnityEngine;
using KSP.IO;

namespace KerbCam
{
	public class SimpleCamPath
	{
		private string name = "";
		private int keyIndex = 0;
		private AnimationCurve hdgCurve = new AnimationCurve();
		private AnimationCurve pitchCurve = new AnimationCurve();
		private AnimationCurve distanceCurve = new AnimationCurve();

		private bool isRunning = false;
		private float curTime;

		public SimpleCamPath (String name)
		{
			this.name = name;
		}

		public bool IsRunning {
			get { return isRunning; }
		}

		public string Name {
			get { return name; }
			set { this.name = value; }
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
			if (index > hdgCurve.length) {
				return -1;
			}

			return hdgCurve[index].time;
		}

		public void MoveCameraToKey(int index)
		{
			if (index > hdgCurve.length) {
				return;
			}
			SetCamera(hdgCurve[index].value,
			          pitchCurve[index].value,
			          distanceCurve[index].value);
		}

		public void RemoveKey(int index)
		{
			if (index > hdgCurve.length) {
				return;
			}
			hdgCurve.RemoveKey(index);
			pitchCurve.RemoveKey(index);
			distanceCurve.RemoveKey(index);
		}

		public void ToggleRunning ()
		{
			isRunning = !isRunning;
			curTime = 0F;
		}
		
		public void StartRunning()
		{
			isRunning = true;
			curTime = 0F;
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
			if (curTime > (keyIndex - 1))
			{
				StopRunning ();
				return;
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

		private static void SetCamera(float hdg, float pitch, float distance)
		{
			var cam = FlightCamera.fetch;
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
			GUILayout.Label("Simple camera path");
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name:");
			path.Name = GUILayout.TextField(path.Name);
			GUILayout.EndHorizontal();

			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
			var numKeys = path.NumKeys;
			for (int i = 0; i < numKeys; i++) {
				GUILayout.BeginHorizontal();
				//GUILayout.Label(string.Format("#{0} @{1}s", i, path.TimeAt(i)));
				if (GUILayout.Button("View")) {
					path.MoveCameraToKey(i);
				}
				if (GUILayout.Button("X")) {
					path.RemoveKey(i);
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
			
			GUILayout.EndVertical();
		}
	}
}

