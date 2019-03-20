using System.Collections.Generic;

namespace Rook.Framework.Core.Monitoring
{
    /// <summary>
    /// Provider of build info labels for the microlise_service_build_info
    /// metric.
    /// </summary>
    public interface IBuildInfoLabelProvider
    {
        /// <summary>
        /// Get a collection of build info label names and values
        /// to include in the microlise_service_build_info metric
        /// </summary>
        IEnumerable<BuildInfoLabel> GetBuildInfoLabels();        
    }
}