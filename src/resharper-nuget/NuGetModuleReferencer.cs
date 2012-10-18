using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Module;
using JetBrains.Util;

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
            return IsNuGetAssembly(moduleToReference);
        }

        public bool ReferenceModuleWithType(IPsiModule module, ITypeElement typeToReference)
        {
            var assemblyModule = typeToReference.Module as IAssemblyPsiModule;
            var projectModule = module as IProjectPsiModule;
            if (assemblyModule == null || projectModule == null)
                return false;

            if (nuget.InstallAssemblyAsNuGetPackage(assemblyModule.Assembly.Location, projectModule.Project))
            {
                // TODO: I wish we didn't have to do this
                // When NuGet references the assemblies, they are queued up to be processed, but after
                // this method completes. Which means the import type part of the process fails to find
                // the type to import. We force an update which works through the system early. It would
                // be nice to find out if we can process the proper import notifications instead
                using (var cookie = module.GetSolution().CreateTransactionCookie(DefaultAction.Commit, "ReferenceModuleWithType", NullProgressIndicator.Instance))
                    cookie.AddModuleReference(projectModule.Project, assemblyModule.ContainingProjectModule);
                return true;
            }

            return false;
        }

        private bool IsNuGetAssembly(IPsiModule module)
        {
            var assemblyModule = module as IAssemblyPsiModule;
            if (assemblyModule == null)
                return false;

            return nuget.IsNuGetPackageAssembly(assemblyModule.Assembly.Location);
        }
    }
}
