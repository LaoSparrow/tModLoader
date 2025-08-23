#if ANDROID
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Android.App;
using K4os.Compression.LZ4;
using Terraria.ModLoader;
using Xamarin.Android.AssemblyStore;

namespace Terraria;

public static class MonoAssemblyStoreLoader
{
	static MonoAssemblyStoreLoader()
	{
		Explorer = new AssemblyStoreExplorer(Application.Context.PackageManager!
			.GetApplicationInfo(Application.Context.PackageName!, 0).PublicSourceDir!,
			(level, s) => Logging.tML.Info($"[{nameof(MonoAssemblyStoreLoader)}] [{level:G}] {s}"),
			true);
	}

	public static readonly AssemblyStoreExplorer Explorer;

	const uint CompressedDataMagic = 0x5A4C4158; // 'XALZ', little-endian

	public static bool TryGetBytesByName(string name, [NotNullWhen(true)] out byte[]? bytes)
	{
		if (Explorer.AssembliesByName.TryGetValue(name, out AssemblyStoreAssembly asm)) {
			using var ms = new MemoryStream();
			asm.ExtractImage(ms);

			ms.Seek(0, SeekOrigin.Begin);
			using var reader = new BinaryReader(ms);
			uint magic = reader.ReadUInt32();
			if (magic != CompressedDataMagic) {
				// data not compressed
				bytes = ms.ToArray();
				return true;
			}

			reader.ReadUInt32(); // descriptor index, ignore
			uint decompressedLength = reader.ReadUInt32();
			int inputLength = (int)(ms.Length - 12);
			byte[] sourceBytes = ArrayPool<byte>.Shared.Rent(inputLength);
			reader.Read(sourceBytes, 0, inputLength);

			byte[] assemblyBytes = new byte[decompressedLength];
			int decoded = LZ4Codec.Decode(sourceBytes, 0, inputLength, assemblyBytes, 0, (int)decompressedLength);
			if (decoded != (int)decompressedLength) {
				Logging.tML.Info(
					$"[{nameof(MonoAssemblyStoreLoader)}] Failed to decompress LZ4 data of {name} (decoded: {decoded})");
				bytes = null;
				return false;
			}

			bytes = assemblyBytes;
			return true;
		}

		bytes = null;
		return false;
	}
}
#endif