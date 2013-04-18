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

using JetBrains.VsIntegration.Application;

namespace JetBrains.ReSharper.Plugins.NuGet
{
    public static class VsServiceMapExtensions
    {
#if RESHARPER_8
        public static bool IsRegistered<T>(this VsServiceProviderResolver.VsServiceMap map)
#else
        public static bool IsRegistered<T>(this VsServiceProviderComponentContainer.VsServiceMap map)
#endif
        {
            return map.Resolve(typeof (T)) != null;
        }
    }
}