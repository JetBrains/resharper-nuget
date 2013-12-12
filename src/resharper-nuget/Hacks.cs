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
using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.UI.Tooltips;
#if RESHARPER_8
using JetBrains.ReSharper.Psi.Modules;
#else
using JetBrains.ReSharper.Psi;
#endif
using JetBrains.TextControl;
using JetBrains.Threading;
using JetBrains.UI.Application;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.NuGet
{
    internal static class Hacks
    {
        public static void PokeReSharpersAssemblyReferences(IPsiModule module,
                                                            IEnumerable<FileSystemPath> assemblyLocations,
                                                            FileSystemPath packageLocation,
                                                            IProjectPsiModule projectModule)
        {
            if (packageLocation.IsNullOrEmpty())
                return;

            // TODO: I wish we didn't have to do this
            // When NuGet references the assemblies, they are queued up to be processed, but after
            // this method completes. Which means the import type part of the process fails to find
            // the type to import. We force an update which works through the system early. It would
            // be nice to find out if we can process the proper import notifications instead
            using (var cookie = module.GetSolution()
                                      .CreateTransactionCookie(DefaultAction.Commit, "ReferenceModuleWithType",
                                                               NullProgressIndicator.Instance))
            {
                var assemblyLocation = assemblyLocations.FirstOrDefault(packageLocation.IsPrefixOf);
                if (!assemblyLocation.IsNullOrEmpty())
                    cookie.AddAssemblyReference(projectModule.Project, assemblyLocation);
            }
        }

        public static void HandleFailureToReference(FileSystemPath packageLocation, Lifetime lifetime, ITextControlManager textControlManager, IShellLocks shellLocks, ITooltipManager tooltipManager, IActionManager actionManager)
        {
            // TODO: Wish we didn't have to do this, either
            // If we failed to install the package, it's because something has gone wrong,
            // and we don't want the rest of the process to continue. Unfortunately, ReSharper
            // doesn't display any error messages, so we'll very hackily try and find the
            // current text editor, and display a tooltip with an error message.
            // (This replicates the experience when something else goes wrong in the context
            // actions that use ModuleReferencerService.) It's not a nice thing to do, but
            // it's safe with how ReSharper uses IModuleReferencer out of the box. If anyone
            // else uses it, and we go wrong, well, fingers crossed. We should either get
            // the tooltip as expected, multiple error messages, or no error messages. Hopefully,
            // we shouldn't see this very often, although I've probably just jinxed it now...
            //
            // Ideally, ReSharper should provide a better error mechanism as part of IModuleReferencer
            if (packageLocation.IsNullOrEmpty())
            {
                var textControl = textControlManager.FocusedTextControl.Value;
                if (textControl != null)
                {
                    shellLocks.Queue("Failed to import type",
                                     () => tooltipManager.ShowAtCaret(lifetime, "Failed to add NuGet package.", textControl, shellLocks, actionManager));
                }
            }
        }
    }
}