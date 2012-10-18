using JetBrains.VsIntegration.Application;
using Microsoft.VisualStudio.ComponentModelHost;

namespace JetBrains.ReSharper.Plugins.NuGet
{
    [WrapVsInterfaces]
    public class ExposeNuGetServices : IExposeVsServices
    {
        public void Register(VsServiceProviderComponentContainer.VsServiceMap map)
        {
            map.QueryService<SComponentModel>().As<IComponentModel>();
        }
    }
}