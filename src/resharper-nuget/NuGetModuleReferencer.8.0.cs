/*
 * Copyright 2012-2013 JetBrains
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

using JetBrains.Application.Components;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.NuGet
{
  // We need to tell ReSharper's component model that we can only run as a VS addin,
  // because we depend on an object that depends on an interface that is only available
  // in Visual Studio. In other words, If we rely on a component marked VS_ADDIN, we
  // must also be marked VS_ADDIN
  [ModuleReferencer(ProgramConfigurations = ProgramConfigurations.VS_ADDIN, Priority = NuGetModuleReferencerPriority)]
  public class NuGetModuleReferencer : IModuleReferencer
  {
    // ReSharper 8 flipped the priority comparison. It now has to be LESS
    // than the GenericModuleReferencer's priority, so it comes first
    private const int NuGetModuleReferencerPriority = -100;

    private readonly NuGetModuleReferencerImpl moduleReferencer;

    public NuGetModuleReferencer(NuGetModuleReferencerImpl moduleReferencer)
    {
      this.moduleReferencer = moduleReferencer;
    }

    public bool CanReferenceModule(IPsiModule module, IPsiModule moduleToReference, IModuleReferenceResolveContext context)
    {
      return moduleReferencer.CanReferenceModule(module, moduleToReference);
    }

    public bool ReferenceModule(IPsiModule module, IPsiModule moduleToReference)
    {
      return moduleReferencer.ReferenceModule(module, moduleToReference);
    }

    public bool ReferenceModuleWithType(IPsiModule module, ITypeElement typeToReference, IModuleReferenceResolveContext resolveContext)
    {
      return ReferenceModule(module, typeToReference.Module);
    }
  }
}
