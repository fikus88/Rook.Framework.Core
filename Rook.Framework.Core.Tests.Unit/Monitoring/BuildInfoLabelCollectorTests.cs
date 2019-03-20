using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Collections.Generic;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;

namespace Rook.Framework.Core.Tests.Unit.Monitoring
{
    [TestClass]
    public class BuildInfoLabelCollectorTests
    {
        [TestMethod]
        public void GetNames_returns_label_names_from_providers()
        {
            var firstProvider = Mock.Of<IBuildInfoLabelProvider>(
               p => p.GetBuildInfoLabels() == new[] { new BuildInfoLabel("version", "1.2.3"), new BuildInfoLabel("major_version", "1") }
               );

            var secondProvider = Mock.Of<IBuildInfoLabelProvider>(
                p => p.GetBuildInfoLabels() == new[] { new BuildInfoLabel("api_version", "4.5.6") }
                );

            var expected = new[] { "version", "major_version", "api_version" };

            var sut = new BuildInfoLabelCollector(Mock.Of<ILogger>(), 
                new[] { firstProvider, secondProvider });
            var actual = sut.GetNames();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetValues_returns_label_values_from_providers()
        {
            var firstProvider = Mock.Of<IBuildInfoLabelProvider>(
               p => p.GetBuildInfoLabels() == new[] { new BuildInfoLabel("version", "1.2.3"), new BuildInfoLabel("major_version", "1") }
               );

            var secondProvider = Mock.Of<IBuildInfoLabelProvider>(
                p => p.GetBuildInfoLabels() == new[] { new BuildInfoLabel("api_version", "4.5.6") }
                );

            var expected = new[] { "1.2.3", "1", "4.5.6" };

            var sut = new BuildInfoLabelCollector(Mock.Of<ILogger>(), 
                new[] { firstProvider, secondProvider });
            var actual = sut.GetValues();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Duplicate_label_names_prefererence_is_first_found()
        {
            var firstProvider = Mock.Of<IBuildInfoLabelProvider>(
                p => p.GetBuildInfoLabels() == new[] { new BuildInfoLabel("version", "1.2.3"), new BuildInfoLabel("major_version", "1") }
                );

            var secondProvider = Mock.Of<IBuildInfoLabelProvider>(
                p => p.GetBuildInfoLabels() == new[] { new BuildInfoLabel("version", "4.5.6") }
                );

            var expected = new[] { "1.2.3", "1" };

            var sut = new BuildInfoLabelCollector(Mock.Of<ILogger>(), 
                new[] { firstProvider, secondProvider });
            var actual = sut.GetValues();

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Duplicate_label_names_are_logged()
        {
            
            var logEntries = new List<LogEntry>();

            var stubLogger = new Mock<ILogger>();

            // Capture Warn logs:
            stubLogger.Setup(l => l.Warn(It.IsAny<string>(), It.IsAny<LogItem[]>())).Callback<string, LogItem[]>(
                (op, items) => logEntries.Add(new LogEntry(op, items))
                );

            var firstProvider = Mock.Of<IBuildInfoLabelProvider>(
                p => p.GetBuildInfoLabels() == new[] { new BuildInfoLabel("version", "1.2.3"), new BuildInfoLabel("api_version", "4.5.6") }
                );

            var secondProvider = Mock.Of<IBuildInfoLabelProvider>(
                p => p.GetBuildInfoLabels() == new[] { new BuildInfoLabel("version", "7.8.9"), new BuildInfoLabel("api_version", "10.11") }
                );

            var expected = new LogEntry(

                "BuildInfoLabelCollector.GetUniqueLabelsByName",
                 new[]
                    {
                        new LogItem("Event", "Ignoring duplicates build labels"),
                        new LogItem("Duplicates", "version=7.8.9|api_version=10.11")
                    }
            );

            var sut = new BuildInfoLabelCollector(stubLogger.Object, 
                new[] { firstProvider, secondProvider });

            Assert.AreEqual(1, logEntries.Count, "Expected 1 Warn log entry");

            var actualLogEntry = logEntries[0];

            Assert.AreEqual(expected.Operation, actualLogEntry.Operation, "Operation");
            Assert.AreEqual(expected.LogItems.Length, actualLogEntry.LogItems.Length, "LogItems.Length");

            Assert.AreEqual(expected.LogItems.Single(i => i.Key == "Event").Value(), actualLogEntry.LogItems.Single(i => i.Key == "Event").Value(), "Event LogItem");
            Assert.AreEqual(expected.LogItems.Single(i => i.Key == "Duplicates").Value(), actualLogEntry.LogItems.Single(i => i.Key == "Duplicates").Value(), "Duplicates LogItem");
        }

        private class LogEntry
        {
            public LogEntry(string operation, LogItem[] items)
            {
                Operation = operation;
                LogItems = items;
            }

            public string Operation { get; }
            public LogItem[] LogItems { get; }
        }
    }
}
