using System.Collections.Generic;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Monitoring
{
    /// <summary>
    /// Provides the build info labels for Rook.Framework.Core
    /// </summary>
    public class BuildInfoLabelProvider : IBuildInfoLabelProvider
    {
        private static readonly BuildInfoLabel[] _labels = new BuildInfoLabel[]
        {
            new BuildInfoLabel("version", ServiceInfo.Version),
            new BuildInfoLabel("major_version", ServiceInfo.MajorVersion),
            new BuildInfoLabel("core_version", ServiceInfo.MicroServiceCoreVersion)
        };

        public IEnumerable<BuildInfoLabel> GetBuildInfoLabels() => _labels;
    }
}
