#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NotFluffy.PackageEditor
{
	[Serializable]
	public class PackageEditorDB
	{
		private const string DATABASE_NAME = "PackageEditorDB.json";

		public List<PackageEditorDBEntry> Entries = new();

		#region Utilities

		public static PackageEditorDB Load()
		{
			var filepath = GetDatabasePath();
			
			if(File.Exists(filepath))
			{
				var contents = File.ReadAllText(filepath);
				return JsonUtility.FromJson<PackageEditorDB>(contents);
			}

			return new();
		}


		public static void Store(PackageEditorDB db)
		{
			var filepath = GetDatabasePath();
			var contents = JsonUtility.ToJson(db, true);
			File.WriteAllText(filepath, contents);
		}
		
		private static string GetDatabasePath() => Path.Combine(Path.GetFullPath("Packages"), DATABASE_NAME);

		#endregion
	}

	[Serializable]
	public struct PackageEditorDBEntry 
	{
		public string Name;
		public string URL;
	}
}

#endif
