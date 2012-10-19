using JetBrains.VsIntegration.Application;
using Microsoft.VisualStudio.ComponentModelHost;

namespace JetBrains.ReSharper.Plugins.NuGet
{
    [WrapVsInterfaces]
    public class ExposeNuGetServices : IExposeVsServices
    {
        public void Register(VsServiceProviderComponentContainer.VsServiceMap map)
        {
            if (!map.IsRegistered<IComponentModel>())
                map.QueryService<SComponentModel>().As<IComponentModel>();
        }
    }
}