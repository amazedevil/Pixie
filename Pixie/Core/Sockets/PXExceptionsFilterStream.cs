using Pixie.Core.Common.Streams;
using Pixie.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pixie.Core.Sockets
{
    internal class PXExceptionsFilterStream : Stream
    {
        private enum InternalState
        {
            Normal,
            Error
        }

        public override bool CanRead => innerStream.CanRead;

        public override bool CanSeek => innerStream.CanSeek;

        public override bool CanWrite => innerStream.CanWrite;

        public override long Length => innerStream.Length;

        public override long Position { get => innerStream.Position; set => innerStream.Position = value; }

        private Stream innerStream;
        private volatile InternalState internalState = InternalState.Normal;

        public PXExceptionsFilterStream(Stream innerStream) {
            this.innerStream = innerStream;
        }

        public override void Close() {
            this.innerStream.Close();
        }

        public override void Flush() {
            innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return innerStream.Read(buffer, offset, count);
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            if (count == 0) {
                return 0;
            }

            return await WrapStreamFuncOperation(async delegate {
                var bytesRead = await innerStream.ReadAsync(buffer, offset, count, cancellationToken);

                if (bytesRead == 0) {
                    throw new PXConnectionClosedRemoteException();
                }

                return bytesRead;
            });
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            innerStream.Write(buffer, offset, count);
        }

        public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await WrapStreamActionOperation(async delegate {
                await innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            });
        }

        public void SwitchToErrorState() {
            internalState = InternalState.Error;
        }

        private async Task<T> WrapStreamFuncOperation<T>(Func<Task<T>> func) {
            T result = default;         
            
            await WrapStreamActionOperation(async delegate {
                result = await func();
            });

            return result;
        }

        private async Task WrapStreamActionOperation(Func<Task> action) {
            try {
                await action();
            } catch (ObjectDisposedException) {
                switch (internalState) {
                    case InternalState.Normal:
                        throw new PXConnectionClosedLocalException();
                    case InternalState.Error:
                        throw new PXConnectionLostException(this);
                }
            } catch (IOException) {
                throw new PXConnectionLostException(this);
            }
        }
    }
}
