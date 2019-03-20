using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Tests.Unit {
    [TestClass]
    public class AutoDictionaryTests
    {
        [TestMethod]
        public void ConstructAutoDictionaryFromNormalDictionary()
        {
            Dictionary<string, bool> input = new Dictionary<string, bool>() {
                {"hello", true},
                {"world", false}
            };

            AutoDictionary<string, bool> tested = new AutoDictionary<string, bool>(input);
            Assert.AreEqual(true, tested["hello"]);
            Assert.AreEqual(false, tested["world"]);
            Assert.AreEqual(2, tested.Count);
        }

        [TestMethod]
        public void CanRemoveFromAutoDictionary()
        {
            AutoDictionary<string, bool> tested = new AutoDictionary<string, bool>();
            tested["Hello"] = true;
            Assert.AreEqual(true, tested["Hello"]);
            tested.Remove("Hello");
            Assert.AreEqual(false, tested["Hello"]);
        }

        [TestMethod]
        public void CanTryGetValueFromAutoDictionary()
        {
            AutoDictionary<string, bool> tested = new AutoDictionary<string, bool>();
            tested["Hello"] = true;
            Assert.AreEqual(true, tested.TryGetValue("Hello", out bool outvalue));
            Assert.AreEqual(true, outvalue);
        }

        [TestMethod]
        public void CanTryGetValueFromAutoDictionaryWhenNoEntryExists()
        {
            AutoDictionary<string, bool> tested = new AutoDictionary<string, bool>();
            Assert.AreEqual(true, tested.TryGetValue("Hello", out bool outvalue));
            Assert.AreEqual(false, outvalue);
        }

        [TestMethod]
        public void AutoDictionaryKeysAndValuesCollectionsAreCorrect()
        {
            AutoDictionary<string, bool> tested = new AutoDictionary<string, bool>() {
                {"hello", true},
                {"world", false}
            };

            Assert.AreEqual(2, tested.Keys.Count);
            Assert.AreEqual(2, tested.Values.Count);
            Assert.AreEqual("hello", tested.Keys.First());
            Assert.AreEqual("world", tested.Keys.Skip(1).First());
            Assert.AreEqual(true, tested.Values.First());
            Assert.AreEqual(false, tested.Values.Skip(1).First());
        }

        [TestMethod]
        public void CanAddToAutoDictionary()
        {
            AutoDictionary<string, bool> tested = new AutoDictionary<string, bool>();
            tested.Add("hello", true);
            Assert.AreEqual(true, tested["hello"]);
        }

        [TestMethod]
        public void CanAddKeyValuePairToAutoDictionary()
        {
            AutoDictionary<string, bool> tested = new AutoDictionary<string, bool>();
            tested.Add(new KeyValuePair<string, bool>("hello", true));
            Assert.AreEqual(true, tested["hello"]);
        }

        [TestMethod]
        public void CanClearAutoDictionary()
        {
            AutoDictionary<string, bool> tested = new AutoDictionary<string, bool>() {
                {"hello", true},
                {"world", true}
            };
            tested.Clear();
            Assert.AreEqual(0, tested.Count);
            Assert.AreEqual(false, tested["hello"]);
            Assert.AreEqual(false, tested.ContainsKey("hello"));
            Assert.AreEqual(false, tested["world"]);
        }

        
    }
}