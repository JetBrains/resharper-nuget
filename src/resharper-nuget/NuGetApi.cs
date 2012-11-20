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
using EnvDTE;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.VsIntegration.ProjectModel;
using Microsoft.VisualStudio.ComponentModelHost;
using NuGet.VisualStudio;
using System.Linq;

namespace JetBrains.ReSharper.Plugins.NuGet
{
    [ShellComponent]
    public class NuGetApi
    {
        private readonly VSSolutionManager vsSolutionManager;
        private readonly IThreading threading;
        private readonly IVsPackageInstallerServices vsPackageInstallerServices;
        private readonly IVsPackageInstaller vsPackageInstaller;

        public NuGetApi(IComponentModel componentModel,
                        VSSolutionManager vsSolutionManager,
                        IThreading threading)
        {
            this.vsSolutionManager = vsSolutionManager;
            this.threading = threading;
            try
            {
                vsPackageInstallerServices = componentModel.GetService<IVsPackageInstallerServices>();
                vsPackageInstaller = componentModel.GetService<IVsPackageInstaller>();
            }
            catch (Exception e)
            {
                // NuGet isn't installed
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
                    hasPackageAssembly = GetPackageFromAssemblyLocations(fileLocations) != null;
                });

            return hasPackageAssembly;
        }

        public string InstallNuGetPackageFromAssemblyFiles(IList<FileSystemPath> assemblyLocations, IProject project)
        {
            if (!IsNuGetAvailable || assemblyLocations.Count == 0)
                return null;

            string installedAssembly = null;

            // We're talking to NuGet via COM. Make sure we're on the UI thread
            threading.Dispatcher.Invoke("NuGet", () =>
                {
                    var vsProject = GetVsProject(project);
                    if (vsProject != null)
                        installedAssembly = DoInstallAssemblyAsNuGetPackage(assemblyLocations, vsProject);
                });

            return installedAssembly;
        }

        private string DoInstallAssemblyAsNuGetPackage(IEnumerable<FileSystemPath> assemblyLocations, Project vsProject)
        {
            var metadata = GetPackageFromAssemblyLocations(assemblyLocations);
            if (metadata == null)
                return null;

            // Passing in the package's install path and a null version is enough for NuGet to install the existing
            // package into the current project
            // TODO: Need to add some error handling here. What can go wrong?
            vsPackageInstaller.InstallPackage(metadata.InstallPath, vsProject, metadata.Id, (Version) null, false);

            return metadata.InstallPath;
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
            var projectModelSynchronizer = vsSolutionManager.GetProjectModelSynchronizer(project.GetSolution());
            var projectInfo = projectModelSynchronizer.GetProjectInfoByProject(project);
            return projectInfo != null ? projectInfo.GetExtProject() : null;
        }
    }
}


