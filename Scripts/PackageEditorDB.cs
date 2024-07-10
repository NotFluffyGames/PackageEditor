#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NotFluffy.PackageEditor
{
	public static class PackageEditorDB
	{
		private const string DATABASE_NAME = "PackageEditorDB.json";

		private static readonly Dictionary<string, string> Entries;

		#region Utilities

		static PackageEditorDB()
		{
			Entries = Load();
		}

		private static Dictionary<string, string> Load()
		{
			var filepath = GetDatabasePath();

			if (!File.Exists(filepath))
				return new();
			
			var contents = File.ReadAllText(filepath);
			return JsonUtility.FromJson<Dictionary<string, string>>(contents);
		}

		public static bool TryGetUrl(PackageInfo packageInfo, out string url) => Entries.TryGetValue(packageInfo.name, out url);
		public static bool Contains(PackageInfo packageInfo) => Entries.ContainsKey(packageInfo.name);

		public static void Add(PackageInfo packageInfo)
		{
			var url = packageInfo.GetPackageUrl();
			
			if (Entries.TryGetValue(packageInfo.name, out var existing) && existing == url)
				return;
			
			Entries[packageInfo.name] = url;
			Flush();
		}
		
		public static void Remove(PackageInfo packageInfo)
		{
			if (Entries.Remove(packageInfo.name))
				Flush();
		}

		private static void Flush()
		{
			var filepath = GetDatabasePath();
			var contents = JsonUtility.ToJson(Entries, true);
			File.WriteAllText(filepath, contents);
		}
		
		private static string GetDatabasePath() => Path.Combine(Path.GetFullPath("Packages"), DATABASE_NAME);

		#endregion
	}
}

#endif
