#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

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

			switch (packageInfo.source)
			{
				case PackageSource.Git:
				{
					// then add a button to the root
					var switchButton = new Button(packageInfo.SwitchToEmbed)
					{
						text = "Switch to development mode"
					};
				
					root.Add(switchButton);
					break;
				}
				case PackageSource.Embedded:
				{
					// database has no entry of the package
					if (!PackageEditorDB.Contains(packageInfo))
						return;
					
					var horizontalGroup = new VisualElement
					{
						style =
						{
							flexDirection = FlexDirection.Row
						}
					};

					root.Add(horizontalGroup);
				
					var revertButton = new Button(packageInfo.SwitchToGit)
					{
						text = "Revert to production mode"
					};
				
					horizontalGroup.Add(revertButton);

					var openDirectoryButton = new Button(() => PackageEditor.OpenDirectory(packageInfo))
					{
						text = "Open Directory"
					};
				
					horizontalGroup.Add(openDirectoryButton);
					break;
				}
			}
		}
	}
}

#endif
