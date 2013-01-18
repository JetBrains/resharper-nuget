using System;
using System.Xml.Serialization;
using JetBrains.UI.Updates;
using JetBrains.VSIntegration.Updates;

namespace JetBrains.ReSharper.Plugins.NuGet.Updates
{
    [XmlType]
    [XmlRoot("PluginLocalInfo")]
    [Serializable]
    public class PluginLocalEnvironmentInfo
    {
        [XmlElement]
        public UpdateLocalEnvironmentInfoVs LocalEnvironment = new UpdateLocalEnvironmentInfoVs();

        [XmlElement]
        public UpdateLocalEnvironmentInfo.VersionSubInfo PluginVersion = new UpdateLocalEnvironmentInfo.VersionSubInfo();
    }
}