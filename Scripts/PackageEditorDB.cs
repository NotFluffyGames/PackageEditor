#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace NotFluffy.PackageEditor
{
	[InitializeOnLoad]
	public static class PackageEditorDB
	{
		private const string DATABASE_NAME = "PackageEditorDB.json";

		private static readonly Dictionary<string, string> _entries;
		public static IReadOnlyDictionary<string, string> Entries => _entries;

		public static int Count => _entries.Count;

		static PackageEditorDB()
		{
			_entries = Load();
		}

		private static Dictionary<string, string> Load()
		{
			var filepath = GetDatabasePath();

			if (!File.Exists(filepath))
				return new();
			
			var contents = File.ReadAllText(filepath);
			var result = FromString(contents);
			return result;
		}

		public static bool TryGetUrl(PackageInfo packageInfo, out string url) => _entries.TryGetValue(packageInfo.name, out url);
		public static bool Contains(PackageInfo packageInfo) => _entries.ContainsKey(packageInfo.name);

		public static void Add(PackageInfo packageInfo)
		{
			var url = packageInfo.GetPackageUrl();
			
			if (_entries.TryGetValue(packageInfo.name, out var existing) && existing == url)
				return;
			
			_entries[packageInfo.name] = url;
			Flush();
		}
		
		public static void Remove(PackageInfo packageInfo)
		{
			if (_entries.Remove(packageInfo.name))
				Flush();
		}

		private static void Flush()
		{
			var filepath = GetDatabasePath();
			
			if(_entries.Count == 0)
			{
				File.Delete(filepath);
			}
			else
			{
				var contents = ToString(_entries);
				File.WriteAllText(filepath, contents);
			}
		}

		private static string ToString(Dictionary<string, string> dict)
		{
			return string.Join("\n", dict.Select(pair => $"{pair.Key}: {pair.Value}"));
		}

		private static Dictionary<string, string> FromString(string str)
		{
			var result = new Dictionary<string, string>();
			
			if(string.IsNullOrWhiteSpace(str))
				return result;
			
			var pairs = str.Split("\n");
			foreach (var pair in pairs)
			{
				var pairValues = pair.Split(": ");
				result.Add(pairValues[0], pairValues[1]);
			}

			return result;
		} 

		private static string GetDatabasePath() => Path.Combine(Path.GetFullPath("Packages"), DATABASE_NAME);
	}
}

#endif
