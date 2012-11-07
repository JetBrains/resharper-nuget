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

        public bool IsNuGetPackageAssembly(FileSystemPath location)
        {
            return IsNuGetAvailable && IsPackageAssembly(location);
        }

        private bool IsPackageAssembly(FileSystemPath location)
        {
            // We're talking to NuGet via COM. Make sure we're on the UI thread
            var isPackageAssembly = false;
            threading.Dispatcher.Invoke("NuGet", () =>
                {
                    isPackageAssembly = GetPackageFromAssemblyLocation(location) != null;
                });

            return isPackageAssembly;
        }

        public bool InstallAssemblyAsNuGetPackage(FileSystemPath assemblyLocation, IProject project)
        {
            if (!IsNuGetAvailable)
                return false;

            var installed = false;

            // We talking to NuGet via COM. Make sure we're on the UI thread
            threading.Dispatcher.Invoke("NuGet", () =>
                {
                    installed = DoInstallAssemblyAsNuGetPackage(assemblyLocation, project);
                });

            return installed;
        }

        private bool DoInstallAssemblyAsNuGetPackage(FileSystemPath assemblyLocation, IProject project)
        {
            var metadata = GetPackageFromAssemblyLocation(assemblyLocation);
            if (metadata == null)
                return false;

            var vsProject = GetVsProject(project);
            if (vsProject == null)
                return false;

            // Passing in the package's install path and a null version is enough for NuGet to install the existing
            // package into the current project
            // TODO: Need to add some error handling here. What can go wrong?
            vsPackageInstaller.InstallPackage(metadata.InstallPath, vsProject, metadata.Id, (Version) null, false);

            return true;
        }

        private IVsPackageMetadata GetPackageFromAssemblyLocation(FileSystemPath assemblyLocation)
        {
            return vsPackageInstallerServices.GetInstalledPackages().FirstOrDefault(p => assemblyLocation.FullPath.StartsWith(p.InstallPath, StringComparison.InvariantCultureIgnoreCase));
        }

        private Project GetVsProject(IProject project)
        {
            var projectModelSynchronizer = vsSolutionManager.GetProjectModelSynchronizer(project.GetSolution());
            var projectInfo = projectModelSynchronizer.GetProjectInfoByProject(project);
            return projectInfo != null ? projectInfo.GetExtProject() : null;
        }
    }
}


