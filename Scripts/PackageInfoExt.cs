using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;

#if UNITY_EDITOR
namespace NotFluffy.PackageEditor
{
    public static class PackageInfoExt
    {
        public static string GetPackageUrl(this PackageInfo packageInfo)
        {
            var packageId = packageInfo.packageId;
            return packageId[(packageId.IndexOf('@') + 1)..];
        }

        public static void ParseGitUrl(this PackageInfo packageInfo, out string packageUrl, out string repoUrl, out string packagePathInRepo, out string revision)
        {
            packageUrl = packageInfo.GetPackageUrl();
            ParseGitUrl(packageUrl, out repoUrl, out packagePathInRepo, out revision);
        }

        public static void ParseGitUrl(string packageUrl, out string repoUrl, out string packagePathInRepo, out string revision)
        {
            // Regex expression to match the URL, path, and revision
            const string pattern = @"^(?<url>[^?#]+\.git)(?:\?(?:path=(?<path>[^#?]+))?)?(?:#(?<revision>[^?]+))?(?:\?(?:path=(?<path2>[^#]+))?)?$";
            
            var match = new Regex(pattern).Match(packageUrl);

            if (!match.Success)
                throw new Exception($"Failed to match repository info from package URL: {packageUrl}");

            repoUrl = match.Groups["url"].GetValueOrDefault();

            if (!match.Groups["path"].TryGetValue(out packagePathInRepo))
                packagePathInRepo = match.Groups["path2"].GetValueOrDefault();

            if(!string.IsNullOrWhiteSpace(packagePathInRepo))
                packagePathInRepo = Path.Combine(packagePathInRepo.Split('/', '\\'));

            revision = match.Groups["revision"].GetValueOrDefault();
        }

        private static string GetValueOrDefault(this Group group, string defaultValue = default) => group.Success ? group.Value : defaultValue;
        private static bool TryGetValue(this Group group, out string value)
        {
            if (group.Success)
            {
                value = group.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
#endif