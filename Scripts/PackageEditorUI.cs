#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine.UIElements;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Debug = UnityEngine.Debug;

namespace NotFluffy.PackageEditor
{
	[InitializeOnLoad]
	public class PackageEditorUI : IPackageManagerExtension
	{
		static PackageEditorUI()
		{
			// add it to the UPM extension
			PackageManagerExtensions.RegisterExtension(new PackageEditorUI());
		}

		// The main object representing the PackageEditor
		private readonly PackageEditor packageEditor = new();

		// The root element of the extension UI
		private VisualElement root;

		public VisualElement CreateExtensionUI()
		{
			root = new VisualElement
			{
				style =
				{
					alignSelf = Align.FlexStart,
					flexDirection = FlexDirection.Row
				}
			};

			return root;
		}

		public void OnPackageAddedOrUpdated(PackageInfo packageInfo) { }

		public void OnPackageRemoved(PackageInfo packageInfo) { }

		public void OnPackageSelectionChange(PackageInfo packageInfo) 
		{
			// start by resetting the extension
			root.Clear();

			if(packageInfo == null) { return; }

			if(packageInfo.source == PackageSource.Git)
			{
				// then add a button to the root
				var button = new Button
				{
					text = "Switch to development mode"
				};
				
				button.clicked += () => packageEditor.SwitchToEmbed(packageInfo);
				root.Add(button);
			}
			else if(packageInfo.source == PackageSource.Embedded)
			{
				// no database found
				if (packageEditor?.Database is null) 
					return;

				// database has no entry of the package
				if(!packageEditor.IsPackageInDatabase(packageInfo.name)) 
					return;

				// data base has entry of the package... so, add the button to revert to production
				var button = new Button
				{
					text = "Revert to production mode"
				};
				
				button.clicked += () => packageEditor.SwitchToGit(packageInfo);
				root.Add(button);
			}
		}
	}
}

#endif
