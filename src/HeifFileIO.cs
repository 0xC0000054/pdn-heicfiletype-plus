// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020, 2021, 2022 Nicholas Hayes
//
// pdn-heicfiletype-plus is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// pdn-heicfiletype-plus is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using HeicFileTypePlus.Interop;
using PaintDotNet;
using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace HeicFileTypePlus
{
    internal sealed class HeifFileIO : Disposable
    {
        // 81920 is the largest multiple of 4096 that is below the large object heap threshold.
        private const int MaxBufferSize = 81920;
        private const int Success = 0;
        private const int Failure = 1;

        private SafeCoTaskMemHandle ioCallbacksHandle;
        private Stream stream;
        private readonly bool leaveOpen;
        private readonly HeicIOCallbackRead read;
        private readonly HeicIOCallbackWrite write;
        private readonly HeicIOCallbackSeek seek;
        private readonly HeicIOCallbackGetPosition getPosition;
        private readonly HeicIOCallbackGetSize getSize;
        private readonly byte[] streamBuffer;

        public HeifFileIO(Stream stream, bool leaveOpen)
        {
            if (stream is null)
            {
                ExceptionUtil.ThrowArgumentNullException(nameof(stream));
            }

            this.stream = stream;
            this.leaveOpen = leaveOpen;
            this.read = Read;
            this.write = Write;
            this.seek = Seek;
            this.getPosition = GetPosition;
            this.getSize = GetSize;
            this.streamBuffer = new byte[MaxBufferSize];
            // The callbacks structure is allocated in unmanaged memory because some
            // of the native code will keep a copy for use across calls.
            // This mainly affects file loading, where the callbacks must remain valid
            // for the lifetime of the SafeHeifContext handle.
            this.ioCallbacksHandle = SafeCoTaskMemHandle.Allocate(IOCallbacks.SizeOf);

            unsafe
            {
                IOCallbacks* callbacks = (IOCallbacks*)this.ioCallbacksHandle.DangerousGetHandle();
                callbacks->Read = Marshal.GetFunctionPointerForDelegate(this.read);
                callbacks->Write = Marshal.GetFunctionPointerForDelegate(this.write);
                callbacks->Seek = Marshal.GetFunctionPointerForDelegate(this.seek);
                callbacks->GetPosition = Marshal.GetFunctionPointerForDelegate(this.getPosition);
                callbacks->GetSize = Marshal.GetFunctionPointerForDelegate(this.getSize);
            }
        }

        public SafeHandle IOCallbacksHandle
        {
            get
            {
                if (this.IsDisposed)
                {
                    ExceptionUtil.ThrowObjectDisposedException(nameof(HeifFileIO));
                }

                return this.ioCallbacksHandle;
            }
        }

        public ExceptionDispatchInfo CallbackExceptionInfo
        {
            get;
            private set;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.ioCallbacksHandle != null)
                {
                    this.ioCallbacksHandle.Dispose();
                    this.ioCallbacksHandle = null;
                }

                if (this.stream != null)
                {
                    if (!this.leaveOpen)
                    {
                        this.stream.Dispose();
                    }
                    this.stream = null;
                }
            }

            base.Dispose(disposing);
        }

        private unsafe int Read(IntPtr buffer, nuint count)
        {
            try
            {
                nuint totalBytesRead = 0;

                while (totalBytesRead < count)
                {
                    int streamBytesRead = this.stream.Read(this.streamBuffer,
                                                           0,
                                                           (int)Math.Min(MaxBufferSize, count - totalBytesRead));

                    if (streamBytesRead == 0)
                    {
                        throw new EndOfStreamException();
                    }

                    ReadOnlySpan<byte> source = new(this.streamBuffer, 0, streamBytesRead);

                    source.CopyTo(new Span<byte>((byte*)buffer + totalBytesRead, streamBytesRead));

                    totalBytesRead += (nuint)streamBytesRead;
                }

                return Success;
            }
            catch (Exception ex)
            {
                this.CallbackExceptionInfo = ExceptionDispatchInfo.Capture(ex);
                return Failure;
            }
        }

        private unsafe int Write(IntPtr buffer, nuint count)
        {
            try
            {
                nuint offset = 0;

                while (offset < count)
                {
                    int copySize = (int)Math.Min(MaxBufferSize, count - offset);

                    new ReadOnlySpan<byte>((byte*)buffer + offset, copySize).CopyTo(this.streamBuffer);

                    this.stream.Write(this.streamBuffer, 0, copySize);

                    offset += (nuint)copySize;
                }

                return Success;
            }
            catch (Exception ex)
            {
                this.CallbackExceptionInfo = ExceptionDispatchInfo.Capture(ex);
                return Failure;
            }
        }

        private int Seek(long position)
        {
            int result = Success;

            try
            {
                this.stream.Seek(position, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                this.CallbackExceptionInfo = ExceptionDispatchInfo.Capture(ex);
                result = Failure;
            }

            return result;
        }

        private long GetPosition()
        {
            return this.stream.Position;
        }

        private long GetSize()
        {
            return this.stream.Length;
        }
    }
}
