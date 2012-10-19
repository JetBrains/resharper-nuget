using JetBrains.VsIntegration.Application;

namespace JetBrains.ReSharper.Plugins.NuGet
{
    public static class VsServiceMapExtensions
    {
        public static bool IsRegistered<T>(this VsServiceProviderComponentContainer.VsServiceMap map)
        {
            return map.Resolve(typeof (T)) != null;
        }
    }
}