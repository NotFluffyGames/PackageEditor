using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Debug = UnityEngine.Debug;

namespace NotFluffy.PackageEditor
{
    public class PackageEditor
    {
        private const string DOCUMENT_SUB_FOLDER = "UnityGitPackages"; // Name of the submodule folder in the User's "My Documents" folder
        private const string PROGRESS_BAR_NAME = "Hibzz.PackageEditor"; // 
        private const int STEPS = 6;

        public readonly PackageEditorDB Database = PackageEditorDB.Load();

        // update the database

        // Switch from git mode to embed mode
        public void SwitchToEmbed(PackageInfo packageInfo)
        {
            try
            {
                // the package id for a package installed with git is `package_name@package_giturl`
                // so we extract the url out
                var repoUrl = packageInfo.packageId.Substring(packageInfo.packageId.IndexOf('@') + 1);

                // figure out the path to which the repo must be cloned to
                var devPackagesDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    DOCUMENT_SUB_FOLDER
                );

                var packageDirectory = Path.Combine(
                    devPackagesDirectory,
                    packageInfo.name);

                Debug.Log($"Target path: {packageDirectory}");
                // update the progress bar
                UpdateProgress("Initializing", 1);

                // create the file path if it doesn't exist and clone the repo there
                // (according .net documentation, we don't need to check if it exists or not)
                Directory.CreateDirectory(devPackagesDirectory);

                if (Directory.Exists(packageDirectory))
                    Directory.Delete(packageDirectory, true);

                Clone(repoUrl, devPackagesDirectory, packageInfo.name);

                // add the entry to the database
                UpdateProgress("Updating Database", 3);
                Database.Entries.Add(new PackageEditorDBEntry { Name = packageInfo.name, URL = repoUrl });

                // create a symbolic link to the packages folder
                UpdateProgress("Creating symbolic link to the cloned repository", 5);

                var destination = PackagePath(packageInfo);

                CreateSymlink(source: packageDirectory, destination);

                // remove the git package
                UpdateProgress("Removing package downloaded from Git", 4);
                Client.Remove(packageInfo.name);

                // Perform the serialization / save
                UpdateProgress("Serializing Database", 6);
                PackageEditorDB.Store(Database);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                // Done!
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        // switch from embed mode to git
        public void SwitchToGit(PackageInfo packageInfo)
        {
            try
            {
                // remove the package
                UpdateProgress("Removing the symlink", 2);

                var packagePath = PackagePath(packageInfo);

                Directory.Delete(packagePath);

                // install the one with the git url from the entries
                UpdateProgress("Reinstalling git package", 4);
                var data = Database.Entries.Find(data => data.Name == packageInfo.name);
                Client.Add(data.URL);

                // Update the database
                UpdateProgress("Updating database", 6);
                Database.Entries.RemoveAll(entry => entry.Name == packageInfo.name);
                PackageEditorDB.Store(Database);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                // Done!
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        private static string PackagePath(PackageInfo packageInfo)
        {
            var packagePath = Path.Combine(
                Path.GetFullPath("Packages\\"),
                packageInfo.name);
            return packagePath;
        }

        public static void OpenDirectory(PackageInfo packageInfo)
        {
            try
            {
                var path = PackagePath(packageInfo);

                var process = new Process
                {
                    StartInfo = CreateOpenDirectoryProcessStartInfo(path)
                };
            
                RunProcess(process);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // check if the given repo is part of the database
        public bool IsPackageInDatabase(string name)
        {
            // no database found
            if (Database is null)
                return false;

            // no entries with the given name found in the database
            if (Database.Entries.All(data => data.Name != name))
                return false;

            // found
            return true;
        }

        // clone the given url if possible
        private static void Clone(string url, string workingDirectory, string packageName)
        {
            var path = Path.Combine(
                workingDirectory,
                packageName);

            // if there's already content in the path, we skip the cloning process
            if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
            {
                Debug.Log("<color=#1473e6><b>Skipping Git Clone: <b></color>" +
                          "A developer environment already exists for the requested package.\n" +
                          $"If this is not the intended effect, please delete the appropriate folder in the Documents/{DOCUMENT_SUB_FOLDER}/");
                return;
            }

            // Configure the process that can perform the git clone
            var process = new Process
            {
                StartInfo = CreateGitProcessStartInfo($"clone {url} {packageName}", workingDirectory)
            };

            // update the progress bar
            UpdateProgress("Cloning the git repository", 2);

            RunProcess(process);
        }

        // Creates a symbolic link between the source and the given destination
        private static void CreateSymlink(string source, string destination)
        {
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
            // configure the process to create a symlink
#if UNITY_EDITOR_WIN
			var startInfo = CreateProcessStartInfo($"/c mklink /D \"{destination}\" \"{source}\"");
#else
            var startInfo = CreateProcessStartInfo($"ln -s {source} {destination}");
#endif

            var process = new Process
            {
                StartInfo = startInfo
            };

            RunProcess(process);
        }

        private static ProcessStartInfo CreateGitProcessStartInfo(string argument, string workingDirectory = "")
        {
#if UNITY_EDITOR_WIN
			return new ProcessStartInfo
			{
				FileName = "git",
				Arguments = argument,
				WorkingDirectory = workingDirectory
			};
#else
            return CreateProcessStartInfo("git " + argument, workingDirectory);
#endif
        }

        private static ProcessStartInfo CreateProcessStartInfo(string argument, string workingDirectory = "")
        {
#if UNITY_EDITOR_WIN
			return new ProcessStartInfo()
			{
				FileName = "cmd.exe",
				Arguments = argument,
				Verb = "runas",
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = workingDirectory
			};
#else
            return new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = " -c \"" + argument + " \"",
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
#endif
        }

        private static void RunProcess(Process process)
        {
            // Start the process and wait for it to be done
            process.Start();

            var error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(error))
                Debug.LogError(error);

            process.WaitForExit();
        }

        private static ProcessStartInfo CreateOpenDirectoryProcessStartInfo(string path, string workingDirectory = "")
        {
#if UNITY_EDITOR_WIN
			return new ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = path,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = workingDirectory
			};
#else
            return new ProcessStartInfo
            {
                FileName = "open",
                Arguments = path,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
#endif
        }

        // update the progress bar
        private static void UpdateProgress(string info, float currentStep)
        {
            EditorUtility.DisplayProgressBar(PROGRESS_BAR_NAME, info, currentStep / STEPS);
        }
    }
}