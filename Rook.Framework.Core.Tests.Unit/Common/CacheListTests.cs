using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Tests.Unit.Common
{
    [TestClass]
    public class CacheListTests
    {
        [TestCleanup]
        public void Teardown()
        {
            _getterFuncCounter = 0;
        }

        [Ignore] //ignored due to the fragility of the test.
        [TestMethod]
        public void GetKey_WithTouchDisabled_UpdatesTheCacheWhenTimeoutHasExpiredAndUsesCacheWithinTimeoutPeriod()
        {
            const string key = "we dont use different keys in this example";
            const bool enableTouch = false;
            
            var sut = new CacheList<string, int>(_getterFunc, TimeSpan.FromSeconds(1), enableTouch);

            //initial call sets cache record.
            var resultAfterFirstCall = sut[key];
            Assert.AreEqual(1, resultAfterFirstCall);
            
            //tenth of a second wait should result in same 'cached result'
            System.Threading.Thread.Sleep(100);
            var resultAfterSecondCall = sut[key];
            Assert.AreEqual(1, resultAfterSecondCall);

            //tenth of a second wait should result in same 'cached result'
            System.Threading.Thread.Sleep(100);
            var resultAfterThirdCall = sut[key];
            Assert.AreEqual(1, resultAfterThirdCall);

            //whole second wait should mean cache is expired and updated
            System.Threading.Thread.Sleep(1000);
            var resultAfterFourthCall = sut[key];
            Assert.AreEqual(2, resultAfterFourthCall);

            //tenth of a second wait should result in same 'cached result'
            System.Threading.Thread.Sleep(100);
            var resultAfterFifthCall = sut[key];
            Assert.AreEqual(2, resultAfterFifthCall);
        }
        
        private int _getterFuncCounter;
        private int _getterFunc(string key)
        {
            //every time this getter is called, the counter is incremented.
            return _getterFuncCounter = ++_getterFuncCounter;
        }
    }
}