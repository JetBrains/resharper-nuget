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
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Module;
using JetBrains.Util;
using System.Linq;

namespace JetBrains.ReSharper.Plugins.NuGet
{
    [ModuleReferencer(Priority = NuGetModuleReferencerPriority)]
    public class NuGetModuleReferencer : IModuleReferencer
    {
        // Must be greater than GenericModuleReferencer's priority
        private const int NuGetModuleReferencerPriority = 100;

        private readonly NuGetApi nuget;

        public NuGetModuleReferencer(NuGetApi nuget)
        {
            this.nuget = nuget;
        }

        public bool CanReferenceModule(IPsiModule module, IPsiModule moduleToReference)
        {
            if (!IsProjectModule(module) || !IsAssemblyModule(moduleToReference))
                return false;

            var assemblyLocations = GetAllAssemblyLocations(module);
            return nuget.AreAnyAssemblyFilesNuGetPackages(assemblyLocations);
        }

        // ReSharper 7.1
        public bool ReferenceModule(IPsiModule module, IPsiModule moduleToReference)
        {
            if (!IsProjectModule(module) || !IsAssemblyModule(moduleToReference))
                return false;

            var assemblyLocations = GetAllAssemblyLocations(moduleToReference);
            var projectModule = (IProjectPsiModule)module;
            var packageLocation = nuget.InstallNuGetPackageFromAssemblyFiles(assemblyLocations, projectModule.Project);

            PokeReSharpersAssemblyReferences(module, assemblyLocations, packageLocation, projectModule);

            return !string.IsNullOrEmpty(packageLocation);
        }

        public bool ReferenceModuleWithType(IPsiModule module, ITypeElement typeToReference)
        {
            return ReferenceModule(module, typeToReference.Module);
        }

        private static bool IsProjectModule(IPsiModule module)
        {
            return module is IProjectPsiModule;
        }

        private static bool IsAssemblyModule(IPsiModule module)
        {
            return module is IAssemblyPsiModule;
        }

        private static IList<FileSystemPath> GetAllAssemblyLocations(IPsiModule psiModule)
        {
            var projectModelAssembly = psiModule.ContainingProjectModule as IAssembly;
            if (projectModelAssembly == null)
                return null;

            // ReSharper maintains a list of unique assemblies, and each assembly keeps a track of
            // all of the file copies of itself that the solution knows about. This list of file
            // locations includes sources for references (including NuGet packages), but can also
            // include outputs, e.g. if CopyLocal is set to True. The IAssemblyPsiModule.Assembly.Location
            // returns back the file location of the first copy of the assembly, but the order of
            // the list is undefined - it is entirely possible to get back a file location in a bin\Debug
            // folder. This doesn't help us when trying to add NuGet references - we need to look
            // at all of the locations to try and find the NuGet package location. So we use the
            // ProjectModel instead of the PSI, and get all file locations of the IAssembly
            return (from f in projectModelAssembly.GetFiles()
                    select f.Location).ToList();
        }

        private static void PokeReSharpersAssemblyReferences(IPsiModule module, IEnumerable<FileSystemPath> assemblyLocations, string packageLocation,
                                                             IProjectPsiModule projectModule)
        {
            if (string.IsNullOrEmpty(packageLocation))
                return;

            // TODO: I wish we didn't have to do this
            // When NuGet references the assemblies, they are queued up to be processed, but after
            // this method completes. Which means the import type part of the process fails to find
            // the type to import. We force an update which works through the system early. It would
            // be nice to find out if we can process the proper import notifications instead
            using (var cookie = module.GetSolution().CreateTransactionCookie(DefaultAction.Commit, "ReferenceModuleWithType", NullProgressIndicator.Instance))
            {
                var assemblyLocation = assemblyLocations.FirstOrDefault(l => l.FullPath.StartsWith(packageLocation, StringComparison.InvariantCultureIgnoreCase));
                if (!assemblyLocation.IsNullOrEmpty())
                    cookie.AddAssemblyReference(projectModule.Project, assemblyLocation);
            }
        }
    }
}
