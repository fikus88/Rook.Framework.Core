using System.Collections.Generic;
using System.Linq;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Monitoring
{
    /// <summary>
    /// Collects build info labels from <see cref="IBuildInfoLabelProvider"/> implementations
    /// so they can be collated into th rook_service_build_info metric.
    /// </summary>
    /// <remarks>
    /// Create an implementation of <see cref="IBuildInfoLabelProvider"/> to specify custom
    /// labels and values for the rook_service_build_info metric. If duplicates are 
    /// found then <see cref="BuildInfoLabelCollector"/> will use the first one it finds,
    /// and will log out a warning with the duplicates.
    /// 
    /// Note: Implementors should not create <see cref="IBuildInfoLabelProvider"/>
    /// implementations that result in duplicate labels.  The load order is non-deterministic
    /// so implementors should not rely on <see cref="BuildInfoLabelCollector"/> discarding
    /// the correct metric.
    /// </remarks>
    public class BuildInfoLabelCollector : IBuildInfoLabelCollector
    {
        private readonly BuildInfoLabel[] _labels;
        private readonly ILogger _logger;

        public BuildInfoLabelCollector(ILogger logger, 
            IBuildInfoLabelProvider[] buildInfoLabelProviders)
        {
            _logger = logger;

            var allBuildLabels = buildInfoLabelProviders.SelectMany(p => p.GetBuildInfoLabels());
           
            _labels = GetUniqueLabelsByName(allBuildLabels).ToArray();
        }

        public string[] GetNames() => _labels.Select(l => l.Name).ToArray();

        public string[] GetValues() =>_labels.Select(l => l.Value).ToArray();

        private IEnumerable<BuildInfoLabel> GetUniqueLabelsByName(IEnumerable<BuildInfoLabel> labels)
        {
            // Group by the label name to get the unique labels  
            // A duplicate is where this grouping yields more than 1 value per label name.
            var groupedItems = from l in labels
                               group l by l.Name into labelGrouping
                               select new
                               {
                                   // We use the first one in the group as the real value
                                   Label = new BuildInfoLabel(labelGrouping.Key, labelGrouping.First().Value),

                                   // Anything after the first one is a duplicate
                                   Duplicates = labelGrouping.Skip(1).ToArray()
                               };
            
            var allDuplicates = groupedItems.SelectMany(g => g.Duplicates).ToArray();
            LogDuplicates(allDuplicates);

            return groupedItems.Select(g => g.Label);
        }

        private void LogDuplicates(BuildInfoLabel[] duplicates)
        {
            if (duplicates.Any())
            {
                var description = string.Join("|", duplicates.Select(d => d.ToString()));

                _logger.Warn($"{nameof(BuildInfoLabelCollector)}.{nameof(GetUniqueLabelsByName)}",
                    new LogItem("Event", "Ignoring duplicates build labels"),
                    new LogItem("Duplicates", description));
            }
        }
    }
}
