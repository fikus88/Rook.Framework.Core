using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Tests.Unit {
    [TestClass]
    public class LoggingTests
    {
        [TestMethod]
        public void CreateLogItemWithToString()
        {
            Guid blah = Guid.NewGuid();
            new LogItem("value", blah.ToString);
        }
    }
}