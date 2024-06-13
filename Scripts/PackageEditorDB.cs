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

		public static PackageEditorDB Reload()
		{
			var filepath = Path.GetFullPath("Packages\\") + DATABASE_NAME;
			if(File.Exists(filepath))
			{
				var contents = File.ReadAllText(filepath);
				return JsonUtility.FromJson<PackageEditorDB>(contents);
			}

			return new PackageEditorDB();
		}

		public static void Store(PackageEditorDB db)
		{
			var filepath = Path.GetFullPath("Packages\\") + DATABASE_NAME;
			var contents = JsonUtility.ToJson(db);
			File.WriteAllText(filepath, contents);
		}

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
