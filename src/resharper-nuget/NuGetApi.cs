/*
 * Copyright 2012 JetBrains s.r.o.
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
using System.IO;
using EnvDTE;
using JetBrains.Application;
using JetBrains.Application.Components;
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
    [ShellComponent(ProgramConfigurations.VS_ADDIN)]
    public class NuGetApi
    {
        private readonly IThreading threading;
        private readonly IVsPackageInstallerServices vsPackageInstallerServices;
        private readonly IVsPackageInstaller vsPackageInstaller;

        public NuGetApi(IComponentModel componentModel,
                        IThreading threading)
        {
            this.threading = threading;
            try
            {
                vsPackageInstallerServices = componentModel.GetService<IVsPackageInstallerServices>();
                vsPackageInstaller = componentModel.GetService<IVsPackageInstaller>();
            }
            catch (Exception e)
            {
                Logger.LogException("Unable to get NuGet interfaces. Is NuGet installed?", e);
            }
        }

        private bool IsNuGetAvailable
        {
            get { return vsPackageInstallerServices != null && vsPackageInstaller != null; }
        }

        public bool AreAnyAssemblyFilesNuGetPackages(IList<FileSystemPath> fileLocations)
        {
            if (!IsNuGetAvailable || fileLocations.Count == 0)
                return false;

            // We're talking to NuGet via COM. Make sure we're on the UI thread
            var hasPackageAssembly = false;
            threading.Dispatcher.Invoke("NuGet", () =>
                {
                    hasPackageAssembly = Logger.Catch(() => GetPackageFromAssemblyLocations(fileLocations) != null);
                });

            return hasPackageAssembly;
        }

        // Yeah, that's an out parameter. Bite me.
        public bool InstallNuGetPackageFromAssemblyFiles(IList<FileSystemPath> assemblyLocations, IProject project, out string installedLocation)
        {
            installedLocation = null;

            if (!IsNuGetAvailable || assemblyLocations.Count == 0)
                return false;

            // We're talking to NuGet via COM. Make sure we're on the UI thread
            string location = string.Empty;
            bool handled = false;
            threading.Dispatcher.Invoke("NuGet", () =>
                {
                    handled = DoInstallAssemblyAsNuGetPackage(assemblyLocations, project, out location);
                });
            installedLocation = location;

            return handled;
        }

        private bool DoInstallAssemblyAsNuGetPackage(IEnumerable<FileSystemPath> assemblyLocations, IProject project,
                                                     out string installedLocation)
        {
            var handled = false;
            installedLocation = null;

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


        private bool DoInstallAssemblyAsNuGetPackage(IEnumerable<FileSystemPath> assemblyLocations, Project vsProject, 
                                                     out string installedLocation)
        {
            installedLocation = string.Empty;

            var metadata = GetPackageFromAssemblyLocations(assemblyLocations);
            if (metadata == null)
            {
                // Not a NuGet package, we didn't handle this
                return false;
            }

            // We need to get the repository path from the installed package. Sadly, this means knowing that
            // the package is installed one directory below the repository. Just a small crack in the black box.
            // (We can pass "All" as the package source, rather than the repository path, but that would give
            // us an aggregate of the current package sources, rather than using the local repo as a source)
            var repositoryPath = Path.GetDirectoryName(metadata.InstallPath);
            vsPackageInstaller.InstallPackage(repositoryPath, vsProject, metadata.Id, (Version)null, false);
            installedLocation = metadata.InstallPath;

            // Successfully installed, we handled it
            return true;
        }

        private IVsPackageMetadata GetPackageFromAssemblyLocations(IEnumerable<FileSystemPath> assemblyLocations)
        {
            return (from p in vsPackageInstallerServices.GetInstalledPackages()
                    from l in assemblyLocations
                    where l.FullPath.StartsWith(p.InstallPath, StringComparison.InvariantCultureIgnoreCase)
                    select p).FirstOrDefault();
        }

        private Project GetVsProject(IProject project)
        {
            var projectModelSynchronizer = SolutionEx.GetComponent<ProjectModelSynchronizer>(project.GetSolution());
            var projectInfo = projectModelSynchronizer.GetProjectInfoByProject(project);
            return projectInfo != null ? projectInfo.GetExtProject() : null;
        }
    }
}


