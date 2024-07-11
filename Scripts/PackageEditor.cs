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
    public static class PackageEditor
    {
        private const string DOCUMENT_SUB_FOLDER = "UnityGitPackages"; // Name of the submodule folder in the User's "My Documents" folder
        private const string PROGRESS_BAR_NAME = "Package Editor"; 

        /// <summary>
        /// Switch from git mode to embed mode
        /// </summary>
        public static void SwitchToEmbed(this PackageInfo packageInfo)
        {
            var progress = new ProgressBarHandler(PROGRESS_BAR_NAME, 4);
            try
            {
                ValidateGitPackageInfo(packageInfo);

                packageInfo.ParseGitUrl(out var packageUrl, out var repoUrl, out var packagePath, out _);
                
                if (string.IsNullOrWhiteSpace(packageUrl))
                    throw new NullReferenceException(nameof(packageUrl));

                if (string.IsNullOrWhiteSpace(repoUrl))
                    throw new NullReferenceException(repoUrl);

                if (!Uri.IsWellFormedUriString(repoUrl, UriKind.Absolute))
                    throw new UriFormatException($"Repo url is not a valid URI {repoUrl}");
                
                // figure out the path to which the repo must be cloned to
                var devPackagesDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    DOCUMENT_SUB_FOLDER
                );

                var repoDirectory = Path.Combine(
                    devPackagesDirectory,
                    packageInfo.name);

                var packageDirectory = repoDirectory;
                if(!string.IsNullOrWhiteSpace(packagePath))
                    packageDirectory = Path.Combine(
                        repoDirectory,
                        packagePath);
                
                progress.MoveNext("Cloning the git repository");
                
                // create the file path if it doesn't exist and clone the repo there
                // (according .net documentation, we don't need to check if it exists or not)
                Directory.CreateDirectory(devPackagesDirectory);
                
                Clone(repoUrl, packageInfo.git.hash, packageInfo.name, devPackagesDirectory);

                // create a symbolic link to the packages folder
                progress.MoveNext("Creating symbolic link to the cloned repository");
                
                var symlinkDestination = PackagePath(packageInfo);
                CreateSymlink(source: packageDirectory, symlinkDestination);

                // Perform the serialization / save
                progress.MoveNext("Serializing database");
                PackageEditorDB.Add(packageInfo);
                
                progress.MoveNext("Removing the git package");
                Client.Remove(packageInfo.name);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Failed to switch to development mode", e.Message, "Continue");
            }
            finally
            {
                progress.MoveNext("Refreshing assets");
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ValidateGitPackageInfo(PackageInfo packageInfo)
        {
            if (string.IsNullOrWhiteSpace(packageInfo.packageId))
                throw new NullReferenceException(nameof(packageInfo.packageId));

            if (string.IsNullOrWhiteSpace(packageInfo.name))
                throw new NullReferenceException(nameof(packageInfo.name));
                
            if (string.IsNullOrWhiteSpace(packageInfo.git.hash))
                throw new NullReferenceException(nameof(packageInfo.git.hash));
        }


        /// <summary>
        /// Switch from embed mode to git mode
        /// </summary>
        public static void SwitchToGit(this PackageInfo packageInfo)
        {
            var progress = new ProgressBarHandler(PROGRESS_BAR_NAME, 2);
            
            try
            {
                ValidateEmbededPackageInfo(packageInfo);
                
                if(!PackageEditorDB.TryGetUrl(packageInfo, out var packageUrl))
                    throw new NullReferenceException(nameof(packageUrl));
                
                if(string.IsNullOrWhiteSpace(packageUrl))
                    throw new NullReferenceException(nameof(packageUrl));
                
                PackageInfoExt.ParseGitUrl(packageUrl, out _, out _, out var previousVersion);
                
                if (!string.IsNullOrWhiteSpace(previousVersion))
                {
                    var currentVersion = packageInfo.version;
                    if (currentVersion != previousVersion)
                    {
                        var choice = EditorUtility.DisplayDialogComplex(
                            "Previous package version is different from current version",
                            null, 
                            $"Use previous version ({previousVersion})",
                            $"Use current version ({currentVersion})",
                            "Use latest version");

                        packageUrl = choice switch
                        {
                            0 => packageUrl,
                            1 => packageUrl.Replace($"#{previousVersion}", $"#{currentVersion}"),
                            2 => packageUrl.Replace($"#{previousVersion}", ""),
                            _ => packageUrl
                        };
                    }
                    else
                    {
                        var usePrevious = EditorUtility.DisplayDialog(
                            "Previous package version was found",
                            null, 
                            $"Use previous version ({previousVersion})",
                            "Use latest version");

                        if (!usePrevious)
                            packageUrl = packageUrl.Replace($"#{previousVersion}", "");
                    }
                }
                
                
                progress.MoveNext("Removing the embedded package");

                var packagePath = PackagePath(packageInfo);

                Directory.Delete(packagePath);
                
                progress.MoveNext("Reinstalling git package");
                
                Client.Add(packageUrl);

                progress.MoveNext("Removing the git package from the database");
                PackageEditorDB.Remove(packageInfo);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                progress.MoveNext("Refreshing assets");
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
        }
        
        private static void ValidateEmbededPackageInfo(PackageInfo packageInfo)
        {
            if (string.IsNullOrWhiteSpace(packageInfo.name))
                throw new NullReferenceException(nameof(packageInfo.name));
        }

        private static string PackagePath(PackageInfo packageInfo)
        {
            return Path.Combine(
                Path.GetFullPath("Packages\\"),
                packageInfo.name);
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

        // Clone the given url if possible
        private static void Clone(string repoUrl, string commitHash, string directory, string workingDirectory)
        {
            var path = Path.Combine(
                workingDirectory,
                directory);

            Process process;
            
            // if there's already content in the path, we skip the cloning process
            if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
            {
                process = new Process
                {
                    StartInfo = CreateGitProcessStartInfo($"checkout {commitHash}")
                };
            }
            else
            {
                // Configure the process that can perform the git clone
                process = new Process
                {
                    StartInfo = CreateGitProcessStartInfo($"clone {repoUrl} {directory}", workingDirectory)
                };
            }

            RunProcess(process);

            if (!Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any())
                throw new($"Failed to clone repo {repoUrl} to {directory}");
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
			return new ProcessStartInfo
            {
				FileName = "cmd.exe",
				Arguments = argument,
				Verb = "runas",
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

        private static void RunProcess(Process process)
        {
            // Start the process and wait for it to be done
            process.Start();

            if (process.StartInfo.RedirectStandardError)
            {
                var error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(error))
                    Debug.LogError(error);
            }

            process.WaitForExit();
        }
    }
}