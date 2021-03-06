﻿using DryIoc;
using Moq;
using NUnit.Framework;
using Pixie.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PixieTests
{
    class LoggerTests
    {
        [Test]
        public void LogLevelTest() {
            Container container = new Container();
            var logWriterMock = new Mock<IPXLogWriterService>();

            container.Use(logWriterMock.Object);

            PXLoggerService logger = new PXLoggerService(container);

            //bools - error, exception, info, debug
            Dictionary<PXLoggerService.LogLevel, bool[]> cases = new Dictionary<PXLoggerService.LogLevel, bool[]>() {
                { PXLoggerService.LogLevel.None, new bool[] { false, false, false, false } },
                { PXLoggerService.LogLevel.Error, new bool[] { true, true, false, false } },
                { PXLoggerService.LogLevel.Info, new bool[] { false, false, true, false } },
                { PXLoggerService.LogLevel.Debug, new bool[] { false, false, false, true } },
                { PXLoggerService.LogLevel.All, new bool[] { true, true, true, true } }
            };

            foreach (var c in cases) {
                logger.Level = c.Key;

                var exception = new Exception();

                logger.Error("error");
                logger.Exception(exception);
                logger.Info("info");
                logger.Debug("debug");

                //since logging is asynchronous, we may need time for writing to be done
                Thread.Sleep(250);

                logWriterMock.Verify(m => m.Error(It.Is<string>(s => s == "error")), c.Value[0] ? (Func<Times>)Times.Once : Times.Never);
                logWriterMock.Verify(m => m.Exception(It.Is<Exception>(e => ReferenceEquals(e, exception))), c.Value[1] ? (Func<Times>)Times.Once : Times.Never);
                logWriterMock.Verify(m => m.Info(It.Is<string>(s => s == "info")), c.Value[2] ? (Func<Times>)Times.Once : Times.Never);
                logWriterMock.Verify(m => m.Debug(It.Is<string>(s => s == "debug")), c.Value[3] ? (Func<Times>)Times.Once : Times.Never);

                logWriterMock.Reset();
            }
        }
    }
}
