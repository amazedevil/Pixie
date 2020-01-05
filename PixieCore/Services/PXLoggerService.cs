using DryIoc;
using System;

namespace Pixie.Core.Services
{
    public class PXLoggerService
    {
        [Flags]
        public enum LogLevel {
            None = 0,
            Error = 1,
            Info = 2,
            Debug = 4,
            Default = Error,
            All = Error | Info | Debug,
        }

        private IPXLogWriterService writer;

        public LogLevel Level { set; get; }

        internal PXLoggerService(IContainer container) {
            this.writer = container.Resolve<IPXLogWriterService>();
            this.Level = (LogLevel)container.Env().GetInt(PXEnvironmentService.ENV_PARAM_LOG_LEVEL, (int)LogLevel.Default);
        }

        public void Exception(Exception e) {
            if (Level.HasFlag(LogLevel.Error)) {
                this.writer?.Exception(e);
            }
        }

        public void Error(string s) {
            if (Level.HasFlag(LogLevel.Error)) {
                this.writer?.Error(s);
            }
        }

        public void Info(string s) {
            if (Level.HasFlag(LogLevel.Info)) {
                this.writer?.Info(s);
            }
        }

        public void Debug(string s) {
            if (Level.HasFlag(LogLevel.Debug)) {
                this.writer?.Debug(s);
            }
        }
    }
}
