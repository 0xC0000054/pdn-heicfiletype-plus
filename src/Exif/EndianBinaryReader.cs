﻿// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020, 2021, 2022, 2024, 2025 Nicholas Hayes
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

using PaintDotNet;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace HeicFileTypePlus.Exif
{
    // Adapted from 'Problem and Solution: The Terrible Inefficiency of FileStream and BinaryReader'
    // https://jacksondunstan.com/articles/3568

    internal sealed class EndianBinaryReader : Disposable
    {
#pragma warning disable IDE0032 // Use auto property
        private Stream stream;
        private int readOffset;
        private int readLength;

        private readonly byte[] buffer;
        private readonly int bufferSize;
        private readonly Endianess endianess;
        private readonly bool leaveOpen;
#pragma warning restore IDE0032 // Use auto property

        /// <summary>
        /// Initializes a new instance of the <see cref="EndianBinaryReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="byteOrder">The byte order of the stream.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        public EndianBinaryReader(Stream stream, Endianess byteOrder) : this(stream, byteOrder, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EndianBinaryReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="byteOrder">The byte order of the stream.</param>
        /// <param name="leaveOpen">
        /// <see langword="true"/> to leave the stream open after the EndianBinaryReader is disposed; otherwise, <see langword="false"/>
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        public EndianBinaryReader(Stream stream, Endianess byteOrder, bool leaveOpen)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.bufferSize = (int)Math.Min(stream.Length, 4096);
            this.buffer = ArrayPool<byte>.Shared.Rent(this.bufferSize);
            this.endianess = byteOrder;
            this.leaveOpen = leaveOpen;

            this.readOffset = 0;
            this.readLength = 0;
        }

        public Endianess Endianess => this.endianess;

        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        /// <value>
        /// The length of the stream.
        /// </value>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public long Length
        {
            get
            {
                VerifyNotDisposed();

                return this.stream.Length;
            }
        }

        /// <summary>
        /// Gets or sets the position in the stream.
        /// </summary>
        /// <value>
        /// The position in the stream.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">value is negative.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public long Position
        {
            get
            {
                VerifyNotDisposed();

                return this.stream.Position - this.readLength + this.readOffset;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                VerifyNotDisposed();

                long current = this.Position;

                if (value != current)
                {
                    long bufferStartOffset = current - this.readOffset;
                    long bufferEndOffset = bufferStartOffset + this.readLength;

                    // Avoid reading from the stream if the offset is within the current buffer.
                    if (value >= bufferStartOffset && value <= bufferEndOffset)
                    {
                        this.readOffset = (int)(value - bufferStartOffset);
                    }
                    else
                    {
                        // Invalidate the existing buffer.
                        this.readOffset = 0;
                        this.readLength = 0;
                        this.stream.Seek(value, SeekOrigin.Begin);
                    }
                }
            }
        }

        /// <summary>
        /// Reads the specified number of bytes from the stream, starting from a specified point in the byte array.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="offset">The starting offset in the array.</param>
        /// <param name="count">The count.</param>
        /// <returns>The number of bytes read from the stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public int Read(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            VerifyNotDisposed();

            if (count == 0)
            {
                return 0;
            }

            if ((this.readOffset + count) <= this.readLength)
            {
                Buffer.BlockCopy(this.buffer, this.readOffset, bytes, offset, count);
                this.readOffset += count;

                return count;
            }
            else
            {
                // Ensure that any bytes at the end of the current buffer are included.
                int bytesUnread = this.readLength - this.readOffset;

                if (bytesUnread > 0)
                {
                    Buffer.BlockCopy(this.buffer, this.readOffset, bytes, offset, bytesUnread);
                }

                // Invalidate the existing buffer.
                this.readOffset = 0;
                this.readLength = 0;

                int totalBytesRead = bytesUnread;

                totalBytesRead += this.stream.Read(bytes, offset + bytesUnread, count - bytesUnread);

                return totalBytesRead;
            }
        }

        /// <summary>
        /// Reads the next byte from the current stream.
        /// </summary>
        /// <returns>The next byte read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public byte ReadByte()
        {
            VerifyNotDisposed();

            EnsureBuffer(sizeof(byte));

            byte val = this.buffer[this.readOffset];
            this.readOffset += sizeof(byte);

            return val;
        }

        /// <summary>
        /// Reads the specified number of bytes from the stream.
        /// </summary>
        /// <param name="count">The number of bytes to read..</param>
        /// <returns>An array containing the specified bytes.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public byte[] ReadBytes(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            VerifyNotDisposed();

            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[count];

            if ((this.readOffset + count) <= this.readLength)
            {
                Buffer.BlockCopy(this.buffer, this.readOffset, bytes, 0, count);
                this.readOffset += count;
            }
            else
            {
                // Ensure that any bytes at the end of the current buffer are included.
                int bytesUnread = this.readLength - this.readOffset;

                if (bytesUnread > 0)
                {
                    Buffer.BlockCopy(this.buffer, this.readOffset, bytes, 0, bytesUnread);
                }

                int numBytesToRead = count - bytesUnread;
                int numBytesRead = bytesUnread;
                do
                {
                    int n = this.stream.Read(bytes, numBytesRead, numBytesToRead);

                    if (n == 0)
                    {
                        throw new EndOfStreamException();
                    }

                    numBytesRead += n;
                    numBytesToRead -= n;

                } while (numBytesToRead > 0);

                // Invalidate the existing buffer.
                this.readOffset = 0;
                this.readLength = 0;
            }

            return bytes;
        }

        /// <summary>
        /// Reads a 8-byte floating point value.
        /// </summary>
        /// <returns>The 8-byte floating point value.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public unsafe double ReadDouble()
        {
            ulong temp = ReadUInt64();

            return *(double*)&temp;
        }

        /// <summary>
        /// Reads a 2-byte signed integer.
        /// </summary>
        /// <returns>The 2-byte signed integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        /// <summary>
        /// Reads a 2-byte unsigned integer.
        /// </summary>
        /// <returns>The 2-byte unsigned integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public ushort ReadUInt16()
        {
            VerifyNotDisposed();

            EnsureBuffer(sizeof(ushort));

            ushort value = Unsafe.ReadUnaligned<ushort>(ref this.buffer[this.readOffset]);

            switch (this.endianess)
            {
                case Endianess.Big:
                    if (BitConverter.IsLittleEndian)
                    {
                        value = BinaryPrimitives.ReverseEndianness(value);
                    }
                    break;
                case Endianess.Little:
                    if (!BitConverter.IsLittleEndian)
                    {
                        value = BinaryPrimitives.ReverseEndianness(value);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unsupported byte order: " + this.endianess.ToString());
            }

            this.readOffset += sizeof(ushort);

            return value;
        }

        /// <summary>
        /// Reads a 4-byte signed integer.
        /// </summary>
        /// <returns>The 4-byte signed integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer.
        /// </summary>
        /// <returns>The 4-byte unsigned integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public uint ReadUInt32()
        {
            VerifyNotDisposed();

            EnsureBuffer(sizeof(uint));

            uint value = Unsafe.ReadUnaligned<uint>(ref this.buffer[this.readOffset]);

            switch (this.endianess)
            {
                case Endianess.Big:
                    if (BitConverter.IsLittleEndian)
                    {
                        value = BinaryPrimitives.ReverseEndianness(value);
                    }
                    break;
                case Endianess.Little:
                    if (!BitConverter.IsLittleEndian)
                    {
                        value = BinaryPrimitives.ReverseEndianness(value);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unsupported byte order: " + this.endianess.ToString());
            }

            this.readOffset += sizeof(uint);

            return value;
        }

        /// <summary>
        /// Reads a 4-byte floating point value.
        /// </summary>
        /// <returns>The 4-byte floating point value.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public unsafe float ReadSingle()
        {
            uint temp = ReadUInt32();

            return *(float*)&temp;
        }

        /// <summary>
        /// Reads a 8-byte signed integer.
        /// </summary>
        /// <returns>The 8-byte signed integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public long ReadInt64()
        {
            return (long)ReadUInt64();
        }

        /// <summary>
        /// Reads a 8-byte unsigned integer.
        /// </summary>
        /// <returns>The 8-byte unsigned integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public ulong ReadUInt64()
        {
            VerifyNotDisposed();

            EnsureBuffer(sizeof(ulong));

            ulong value = Unsafe.ReadUnaligned<ulong>(ref this.buffer[this.readOffset]);

            switch (this.endianess)
            {
                case Endianess.Big:
                    if (BitConverter.IsLittleEndian)
                    {
                        value = BinaryPrimitives.ReverseEndianness(value);
                    }
                    break;
                case Endianess.Little:
                    if (!BitConverter.IsLittleEndian)
                    {
                        value = BinaryPrimitives.ReverseEndianness(value);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unsupported byte order: " + this.endianess.ToString());
            }

            this.readOffset += sizeof(ulong);

            return value;
        }

        /// <summary>
        /// Reads an ASCII string from the stream.
        /// </summary>
        /// <param name="length">The length of the string.</param>
        /// <returns>The string.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public string ReadAsciiString(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            VerifyNotDisposed();

            if (length == 0)
            {
                return string.Empty;
            }

            EnsureBuffer(length);

            string value = System.Text.Encoding.ASCII.GetString(this.buffer, this.readOffset, length);

            this.readOffset += length;

            return value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.leaveOpen)
                {
                    this.stream.Dispose();
                }

                ArrayPool<byte>.Shared.Return(this.buffer);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Ensures that the buffer contains at least the number of bytes requested.
        /// </summary>
        /// <param name="count">The minimum number of bytes the buffer should contain.</param>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        private void EnsureBuffer(int count)
        {
            if ((this.readOffset + count) > this.readLength)
            {
                FillBuffer(count);
            }
        }

        /// <summary>
        /// Fills the buffer with at least the number of bytes requested.
        /// </summary>
        /// <param name="minBytes">The minimum number of bytes to place in the buffer.</param>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        private void FillBuffer(int minBytes)
        {
            int bytesUnread = this.readLength - this.readOffset;

            if (bytesUnread > 0)
            {
                Buffer.BlockCopy(this.buffer, this.readOffset, this.buffer, 0, bytesUnread);
            }

            int numBytesToRead = this.bufferSize - bytesUnread;
            int numBytesRead = bytesUnread;
            do
            {
                int n = this.stream.Read(this.buffer, numBytesRead, numBytesToRead);

                if (n == 0)
                {
                    throw new EndOfStreamException();
                }

                numBytesRead += n;
                numBytesToRead -= n;

            } while (numBytesRead < minBytes);

            this.readOffset = 0;
            this.readLength = numBytesRead;
        }

        /// <summary>
        /// Verifies that the <see cref="EndianBinaryReader"/> has not been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        private void VerifyNotDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(EndianBinaryReader));
            }
        }
    }
}
