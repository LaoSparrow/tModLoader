#if ANDROID
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using SDL = SDL2.SDL;
using ThreadPriority = Android.OS.ThreadPriority;

namespace Terraria
{
	[Activity(
		Label = "@string/app_name",
		MainLauncher = true,
		AlwaysRetainTaskState = true,
		LaunchMode = LaunchMode.SingleInstance,
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden |
		                       ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
	)]
	public class MainActivity : AndroidGameActivityEXT
	{
		protected override string[] GetLibraries() => [
			"SDL2",
			"theorafile",
			"FAudio",
			"FNA3D",
			"main"
		];

		protected override void SDLMain()
		{
			// Enable high DPI "Retina" support. Trust us, you'll want this.
			SDL.SDL_SetHint("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");

			// Keep screen Landscape
			SDL.SDL_SetHint(SDL.SDL_HINT_ORIENTATIONS, "LandscapeLeft LandscapeRight");
			// Keep mouse and touch input separate.
			SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_TOUCH_EVENTS, "0");
			SDL.SDL_SetHint(SDL.SDL_HINT_TOUCH_MOUSE_EVENTS, "1");
			System.Environment.SetEnvironmentVariable("FNA_PLATFORM_BACKEND", "SDL2");
			SDL.SDL_SetHint("FNA_PLATFORM_BACKEND", "SDL2");
			SDL.SDL_SetHint("FNA3D_FORCE_DRIVER", "OpenGL");
			SDL.SDL_SetHint("FNA3D_OPENGL_FORCE_ES3", "1");

			System.Environment.SetEnvironmentVariable("XDG_DATA_HOME", Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "XDGDataHome"));
			System.Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "XDGConfigHome"));
			System.Environment.SetEnvironmentVariable("HOME", GetExternalFilesDir(null)!.AbsolutePath);
			Directory.SetCurrentDirectory(GetExternalFilesDir(null)!.AbsolutePath);

			typeof(Logging).GetField("LogDir")!.SetValue(null, Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "tModLoader-Logs"));
			string logDir = (string)typeof(Logging).GetField("LogDir")!.GetValue(null)!;
			if (!Directory.Exists(logDir))
				Directory.CreateDirectory(logDir);

			System.Environment.SetEnvironmentVariable("MONOMOD_LogToFile",
				Path.Combine(logDir, "mmd.log"));

			string titleLocation = (string)typeof(TitleContainer).Assembly.GetType("Microsoft.Xna.Framework.TitleLocation")!.GetProperty("Path",
				BindingFlags.NonPublic |
				BindingFlags.Public |
				BindingFlags.Static)!.GetValue(null)!;
			foreach (string f in EnumerateAssetFiles("Content")) {
				string path = Path.Combine(titleLocation, f);
				if (File.Exists(path)) {
					continue;
				}

				using Stream inputStream = Application.Context.Assets!.Open(f);

				Directory.CreateDirectory(Path.GetDirectoryName(path)!); // Ensure subdirectories
				using var s = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
				inputStream.CopyTo(s);
			}

			// var mainThread = new Thread(() => {
			// 	Android.OS.Process.SetThreadPriority(ThreadPriority.Display);
			// 	MonoLaunch.Main(["-nosteam"]);
			// });
			// mainThread.Start();
			// mainThread.Join();

			MonoLaunch.Main(["-nosteam"]);
		}

		protected IEnumerable<string> EnumerateAssetFiles(string path)
		{
			string[] entries = Application.Context.Assets!.List(path)!;
			foreach (string f in entries) {
				string[] probeEntries = Application.Context.Assets!.List(Path.Combine(path, f))!;
				if (probeEntries.Length != 0) {
					// Is Directory
					foreach (string enumerated in EnumerateAssetFiles(Path.Combine(path, f))) {
						yield return enumerated;
					}
				}
				else {
					// Is File
					yield return Path.Combine(path, f);
				}
			}
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Window.AddFlags(WindowManagerFlags.KeepScreenOn);
			Window.AddFlags(WindowManagerFlags.TranslucentNavigation);
			Window.AddFlags(WindowManagerFlags.TranslucentStatus);
		}

		public override void OnWindowFocusChanged(bool hasFocus)
		{
			base.OnWindowFocusChanged(hasFocus);

			if (hasFocus)
				SetImmersive();
		}

		private void SetImmersive()
		{
			if (System.OperatingSystem.IsAndroidVersionAtLeast(30)) {
				Window.SetDecorFitsSystemWindows(false);
				Window.InsetsController.SystemBarsBehavior =
					(int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
				//NO Color Type error.
				//Window.SetNavigationBarColor(Color.Transparent);
				Window.InsetsController.Hide(WindowInsets.Type.SystemBars());
			}
			else {
#pragma warning disable CS0618
				this.Window.DecorView.SystemUiVisibility =
					(StatusBarVisibility)(SystemUiFlags.LayoutStable | SystemUiFlags.LayoutHideNavigation |
					                      SystemUiFlags.LayoutFullscreen | SystemUiFlags.HideNavigation |
					                      SystemUiFlags.Fullscreen | SystemUiFlags.ImmersiveSticky);
#pragma warning restore CS0618
			}
		}
	}
}
#endif