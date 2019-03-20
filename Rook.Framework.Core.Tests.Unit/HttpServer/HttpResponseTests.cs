using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServer;

namespace Rook.Framework.Core.Tests.Unit.HttpServer
{
    [TestClass]
    public class HttpResponseTests
    {
        private Mock<ILogger> _mockLogger;
        private HttpResponse _sut;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();

            _sut = new HttpResponse(Mock.Of<IDateTimeProvider>(), _mockLogger.Object);
        }

        [TestMethod]
        public void WriteToStream_logs_WARN_if_socket_aborted()
        {
            var expectedLogItems = new[]
            {
                 new LogItem("Event","WriteToStream failed. Socket aborted"),
                 new LogItem("Exception", Path.DirectorySeparatorChar=='/'?"Software caused connection abort":"An established connection was aborted by the software in your host machine"),
                 new LogItem("StackTrace", default(string))
            };

            var ns = new FakeNetworkStream().WithInnerException(new SocketException(10053));

            _sut.SetStringContent("Hello Stephen, this is Clem Fandango. Can you hear me Stephen?");

            try
            {
                _sut.WriteToStream(ns);
            }
            catch (Exception e)
            {
                Assert.Fail("WriteToStream should not re-throw SocketExceptions when the error code is SocketError.ConnectionAborted",e.Message);
            }

            _mockLogger.Verify(x => x.Warn("HttpResponse.WriteToStream", It.Is<LogItem[]>(i => CheckLogItems(i, expectedLogItems))), Times.Once);
        }

        [TestMethod]
        public void WriteToStream_rethrows_socket_errors_if_not_aborted()
        {
            var ns = new FakeNetworkStream().WithInnerException(new SocketException(10050));

            _sut.SetStringContent("Hello Stephen, this is Clem Fandango. Can you hear me Stephen?");

            Assert.ThrowsException<IOException>(() => _sut.WriteToStream(ns));
        }

        [TestMethod]
        public void WriteToStream_rethrows_non_socket_errors()
        {
            var ns = new FakeNetworkStream();

            _sut.SetStringContent("Hello Stephen, this is Clem Fandango. Can you hear me Stephen?");

            Assert.ThrowsException<IOException>(() => _sut.WriteToStream(ns));
        }


        private static bool CheckLogItems(LogItem[] actual, LogItem[] expected)
        {
            if (actual.SequenceEqual(expected, new LogItemComparer()))
                return true;

            for (int i = 0; i < actual.Length; i++)
                if (actual[i].Value() != expected[i].Value())
                    Console.WriteLine(
                        $"LogItem Element {i} differs, actual:\r\n{actual[i].Value()}\r\nexpected:\r\n{expected[i].Value()}");

            return false;
        }

        private class LogItemComparer : IEqualityComparer<LogItem>
        {
            public bool Equals(LogItem x, LogItem y)
            {
                return x.Key == y.Key && x.Value() == y.Value();
            }

            public int GetHashCode(LogItem obj)
            {
                return 0;
            }
        }

        public class FakeNetworkStream : Stream
        {
            private Exception _innerException;

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => true;

            public override long Length => 0;

            public override long Position { get => 0; set => throw new System.NotImplementedException(); }

            public override void Flush()
            {
                throw new System.NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new System.NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new System.NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new System.NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new IOException("", _innerException);
            }

            public FakeNetworkStream WithInnerException(Exception exception)
            {
                _innerException = exception;

                return this;
            }
        }
    }
}
