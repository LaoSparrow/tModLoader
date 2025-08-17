#if ANDROID

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Android.App;
using Org.Libsdl.App;

namespace Terraria
{
	[Activity(
		MainLauncher = true,
		HardwareAccelerated = true
	)]
	public abstract class AndroidGameActivityEXT : SDLActivity
	{
		public delegate void MainFunc();

		[DllImport("main")]
		static extern void SetMain(MainFunc main);

		[SupportedOSPlatform("android21.0")]
		public override void LoadLibraries() {
			base.LoadLibraries();

			SetMain(SDLMain);
		}

		protected abstract void SDLMain();
	}

}

#endif
