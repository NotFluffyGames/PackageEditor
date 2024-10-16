#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEngine;
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

        private bool _initialized;

        // The root element of the extension UI
        private VisualElement _root;

        public VisualElement CreateExtensionUI()
        {
            _initialized = false;

            _root = new VisualElement
            {
                style =
                {
                    alignSelf = Align.FlexStart,
                    flexDirection = FlexDirection.Row
                }
            };

            return _root;
        }

        public void OnPackageAddedOrUpdated(PackageInfo packageInfo)
        {
        }

        public void OnPackageRemoved(PackageInfo packageInfo)
        {
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            InitializeIfNeeded();

            // start by resetting the extension
            _root.Clear();

            if (packageInfo == null)
                return;

            switch (packageInfo.source)
            {
                case PackageSource.Git:
                {
                    // then add a button to the root
                    var switchButton = new Button(packageInfo.SwitchToEmbed)
                    {
                        text = "Switch to development mode"
                    };

                    _root.Add(switchButton);
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

                    _root.Add(horizontalGroup);

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

        private void InitializeIfNeeded()
        {
            if (_initialized)
                return;

            _initialized = true;

            if(PackageEditorDB.Count == 0)
                return;
            
            var windowRoot = _root.GetRoot().Q<TemplateContainer>();
            var toolbarSpacer = windowRoot.Q("toolbarSpacer");
            var toolbarRoot = toolbarSpacer.parent;
            var index = toolbarRoot.IndexOf(toolbarSpacer);
            
            var switchAllToProduction = new ToolbarButton(PackageEditor.SwitchAllToProduction)
            {
                text = "Switch All To Prod",
                style =
                {
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter)
                }
            };

            toolbarRoot.Insert(index, switchAllToProduction);
        }
    }
}

#endif