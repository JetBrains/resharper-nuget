/*
 * Copyright 2012 JetBrains
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using EnvDTE;
using JetBrains.Application.Components;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Threading;
using JetBrains.Util;
#if RESHARPER_8
using JetBrains.Util.Logging;
#endif
using JetBrains.VsIntegration.ProjectModel;
using Microsoft.VisualStudio.ComponentModelHost;
using NuGet.VisualStudio;
using System.Linq;

namespace JetBrains.ReSharper.Plugins.NuGet
{
    // We depend on IComponentModel, which lives in a VS assembly, so tell ReSharper
    // that we can only load as part of a VS addin
    [SolutionComponent(ProgramConfigurations.VS_ADDIN)]
    public class NuGetApi
    {
        private readonly ISolution solution;
        private readonly IThreading threading;
        private readonly ProjectModelSynchronizer projectModelSynchronizer;
        private readonly IVsPackageInstallerServices vsPackageInstallerServices;
        private readonly IVsPackageInstaller vsPackageInstaller;
        private readonly IVsPackageInstallerEvents vsPackageInstallerEvents;
        private readonly object syncObject = new object();
        private ILookup<string, FileSystemPath> installedPackages; // there can be several versions of one package (different versions)

        private static readonly ILookup<string, FileSystemPath> emptyLookup = ToLookup(EmptyList<IVsPackageMetadata>.InstanceList);

        public NuGetApi(ISolution solution, Lifetime lifetime, IComponentModel componentModel, IThreading threading, ProjectModelSynchronizer projectModelSynchronizer)
        {
            this.solution = solution;
            this.threading = threading;
            this.projectModelSynchronizer = projectModelSynchronizer;
            try
            {
                vsPackageInstallerServices = componentModel.GetExtensions<IVsPackageInstallerServices>().SingleOrDefault();
                vsPackageInstaller = componentModel.GetExtensions<IVsPackageInstaller>().SingleOrDefault();
                vsPackageInstallerEvents = componentModel.GetExtensions<IVsPackageInstallerEvents>().SingleOrDefault();
            }
            catch (Exception e)
            {
                Logger.LogException("Unable to get NuGet interfaces.", e);
            }

            if (!IsNuGetAvailable)
            {
                Logger.LogMessage(LoggingLevel.VERBOSE, "[NUGET PLUGIN] Unable to get NuGet interfaces. No exception thrown");
                return;
            }

            lifetime.AddBracket(
              () => vsPackageInstallerEvents.PackageInstalled += RecalcInstalledPackages,
              () => vsPackageInstallerEvents.PackageInstalled -= RecalcInstalledPackages);
          
          lifetime.AddBracket(
              () => vsPackageInstallerEvents.PackageUninstalled += RecalcInstalledPackages,
              () => vsPackageInstallerEvents.PackageUninstalled -= RecalcInstalledPackages);

          RecalcInstalledPackages(null);
        }

        private static ILookup<string, FileSystemPath> ToLookup(IEnumerable<IVsPackageMetadata> packages)
        {
          return packages.ToLookup(_ => _.Id, _ => new FileSystemPath(_.InstallPath), StringComparer.OrdinalIgnoreCase);
        }

        private void RecalcInstalledPackages(IVsPackageMetadata metadata)
        {
            if (!IsNuGetAvailable || solution.IsTemporary)
            {
              lock (syncObject)
              {
                installedPackages = emptyLookup;
              }
              return;
            }

            try
            {
              lock (syncObject)
              {
                installedPackages = ToLookup(vsPackageInstallerServices.GetInstalledPackages());
              }
            }
            catch (Exception ex)
            {
              installedPackages = emptyLookup;
              Logger.LogException("RecalcInstalledPackages", ex);
            }
        }

        private bool IsNuGetAvailable
        {
            get { return vsPackageInstallerServices != null && vsPackageInstaller != null && vsPackageInstallerEvents!= null; }
        }

        public bool AreAnyAssemblyFilesNuGetPackages(IList<FileSystemPath> fileLocations)
        {
            if (!IsNuGetAvailable || fileLocations.Count == 0)
                return false;

            FileSystemPath installedLocation;
            var hasPackageAssembly = GetPackageFromAssemblyLocations(fileLocations, out installedLocation) != null;
            if (!hasPackageAssembly)
                LogNoPackageFound(fileLocations);

            return hasPackageAssembly;
        }

        // Yeah, that's an out parameter. Bite me.
        public bool InstallNuGetPackageFromAssemblyFiles(IList<FileSystemPath> assemblyLocations, IProject project, out FileSystemPath installedLocation)
        {
            installedLocation = FileSystemPath.Empty;

            if (!IsNuGetAvailable || assemblyLocations.Count == 0)
                return false;

            // We're talking to NuGet via COM. Make sure we're on the UI thread
            var location = FileSystemPath.Empty;
            var handled = false;
            threading.Dispatcher.Invoke("NuGet", () =>
            {
                handled = DoInstallAssemblyAsNuGetPackage(assemblyLocations, project, out location);
            });
            installedLocation = location;

            return handled;
        }

        private bool DoInstallAssemblyAsNuGetPackage(IList<FileSystemPath> assemblyLocations, IProject project, out FileSystemPath installedLocation)
        {
            var handled = false;
            installedLocation = FileSystemPath.Empty;

            try
            {
                var vsProject = GetVsProject(project);
                if (vsProject != null)
                    handled = DoInstallAssemblyAsNuGetPackage(assemblyLocations, vsProject, out installedLocation);
            }
            catch (Exception e)
            {
                // Something went wrong while trying to install a NuGet package. Don't
                // let the default module referencers add a file reference, so tell
                // ReSharper that we handled it
                Logger.LogException("Failed to install NuGet package", e);
                handled = true;
            }

            return handled;
        }

        private bool DoInstallAssemblyAsNuGetPackage(IList<FileSystemPath> assemblyLocations, Project vsProject, out FileSystemPath installedLocation)
        {
            var id = GetPackageFromAssemblyLocations(assemblyLocations, out installedLocation);
            if (id == null)
            {
                // Not a NuGet package, we didn't handle this
                LogNoPackageFound(assemblyLocations);
                return false;
            }

            // We need to get the repository path from the installed package. Sadly, this means knowing that
            // the package is installed one directory below the repository. Just a small crack in the black box.
            // (We can pass "All" as the package source, rather than the repository path, but that would give
            // us an aggregate of the current package sources, rather than using the local repo as a source)
            // Also, make sure we're dealing with a canonical path, in case the nuget.config has a repository
            // path defined as a relative path
            var repositoryPath = installedLocation.Directory;
            vsPackageInstaller.InstallPackage(repositoryPath.FullPath, vsProject, id, default(string), false);

            // Successfully installed, we handled it
            return true;
        }

        private void LogNoPackageFound(IEnumerable<FileSystemPath> assemblyLocations)
        {
            if (!Logger.IsLoggingEnabled)
                return;

            var assemblies = assemblyLocations.AggregateString(", ", (builder, arg) => builder.Append(arg.QuoteIfNeeded()));
            Logger.LogMessage(LoggingLevel.VERBOSE, "[NUGET PLUGIN] No package found for assemblies: {0}", assemblies);
        }

        private string GetPackageFromAssemblyLocations(IList<FileSystemPath> assemblyLocations, out FileSystemPath installedLocation)
        {
            lock (syncObject)
            {
                installedLocation = FileSystemPath.Empty;
                foreach (var installedPackage in installedPackages)
                foreach (var installedPackageLocation in installedPackage)
                foreach (var assemblyLocation in assemblyLocations)
                {
                    if (installedPackageLocation.IsPrefixOf(assemblyLocation))
                    {
                        installedLocation = installedPackageLocation;
                        return installedPackage.Key;
                    }
                }
                return null;
            }
        }

        private Project GetVsProject(IProject project)
        {
          var projectInfo = projectModelSynchronizer.GetProjectInfoByProject(project);
          return projectInfo != null ? projectInfo.GetExtProject() : null;
        }
    }
}


