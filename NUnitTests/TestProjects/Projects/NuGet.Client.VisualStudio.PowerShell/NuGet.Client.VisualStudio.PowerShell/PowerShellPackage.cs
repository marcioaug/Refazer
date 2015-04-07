﻿using Newtonsoft.Json.Linq;
using NuGet.Resources;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet.Client.VisualStudio.PowerShell
{
    public class PowerShellPackage
    {
        public string Id { get; set; }

        public NuGetVersion Version { get; set; }

        public string Description { get; set; }

        public static List<PowerShellPackage> GetPowerShellPackageView(IEnumerable<JObject> metadata)
        {
            List<PowerShellPackage> view = new List<PowerShellPackage>();
            foreach (JObject json in metadata)
            {
                PowerShellPackage package = new PowerShellPackage();
                package.Id = json.Value<string>(Properties.PackageId);
                package.Version = NuGetVersion.Parse(json.Value<string>(Properties.Version));
                package.Description = json.Value<string>(Properties.Description);
                if (string.IsNullOrEmpty(package.Description))
                {
                    package.Description = json.Value<string>(Properties.Summary);
                }
                view.Add(package);
            }
            return view;
        }

        // TODO List
        // 1. The unlisted packages are not filtered out. The plan is that Server will return unlisted packages.
        // Test EntityFramework 7.0.0-beta1 is not installed when specify -pre.
        // 2. GetLastestVersionForPackage supports local repository such as UNC share.
        public static string GetLastestVersionForPackage(SourceRepository repo, string packageId, bool allowPrerelease, NuGetVersion nugetVersion = null, bool isSafe = false)
        {
            string version = String.Empty;
            try
            {
                Task<IEnumerable<JObject>> packages = repo.GetPackageMetadataById(packageId);
                var r = packages.Result;
                var allVersions = r.Select(p => NuGetVersion.Parse(p.Value<string>(Properties.Version)));
                if (!allowPrerelease)
                {
                    allVersions = allVersions.Where(p => !p.IsPrerelease);
                }
                if (isSafe && nugetVersion != null)
                {
                    VersionRange spec = GetSafeRange(nugetVersion, allowPrerelease);
                    allVersions = allVersions.Where(p =>
                    {
                        return p < spec.MaxVersion && p >= spec.MinVersion;
                    });       
                }                
                version = allVersions.OrderByDescending(v => v).FirstOrDefault().ToNormalizedString();
            }
            catch (Exception)
            {
                if (string.IsNullOrEmpty(version))
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnknownPackage, packageId));
                }
            }
            return version;
        }

        /// <summary>
        /// Get latest update for package identity
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="identity"></param>
        /// <param name="allowPrerelease"></param>
        /// <returns></returns>
        public static PackageIdentity GetLastestUpdateForPackage(SourceRepository repo, PackageIdentity identity, bool allowPrerelease, bool isSafe)
        {
            string latestVersion = GetLastestVersionForPackage(repo, identity.Id, allowPrerelease, identity.Version, isSafe);
            PackageIdentity latestIdentity = null;
            if (latestVersion != null)
            {
                latestIdentity = new PackageIdentity(identity.Id, NuGetVersion.Parse(latestVersion));
            }
            return latestIdentity;
        }

        /// <summary>
        /// The safe range is defined as the highest build and revision for a given major and minor version
        /// </summary>
        public static VersionRange GetSafeRange(NuGetVersion version, bool includePrerlease)
        {
            SemanticVersion max = new SemanticVersion(new Version(version.Major, version.Minor + 1));
            NuGetVersion maxVersion = NuGetVersion.Parse(max.ToString());
            return new VersionRange(version, true, maxVersion, false, includePrerlease);
        }

        public static bool IsPrereleaseVersion(string version)
        {
            SemanticVersion sVersion = new SemanticVersion(version);
            bool isPrerelease = !String.IsNullOrEmpty(sVersion.SpecialVersion);
            return isPrerelease;
        }
    }
}
