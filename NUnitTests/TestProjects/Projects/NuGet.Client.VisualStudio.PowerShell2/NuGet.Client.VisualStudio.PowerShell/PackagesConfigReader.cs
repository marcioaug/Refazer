﻿using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// TODO: Use PackagesConfigReader from NuGet.Packaging.
    /// </summary>
    public class PackagesConfigReader
    {
        private readonly Stream _stream;

        public PackagesConfigReader(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            _stream = stream;
        }

        public IEnumerable<PackageIdentity> GetPackages()
        {
            XDocument doc = XDocument.Load(_stream);

            List<PackageIdentity> packages = new List<PackageIdentity>();

            foreach (var package in doc.Root.Elements(XName.Get("package")))
            {
                string id = package.Attributes(XName.Get("id")).Single().Value;
                string version = package.Attributes(XName.Get("version")).Single().Value;

                // todo: handle validation
                NuGetVersion semver = NuGetVersion.Parse(version);

                packages.Add(new PackageIdentity(id, semver));
            }

            return packages;
        }
    }
}
