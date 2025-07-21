// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
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

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace HeicFileTypePlus.ICCProfile
{
    [DebuggerDisplay("{ToString(), nq}")]
    internal readonly struct ProfileSignature : IEquatable<ProfileSignature>
    {
        public ProfileSignature(ReadOnlySpan<byte> bytes) => this.Value = BinaryPrimitives.ReadUInt32BigEndian(bytes);

        public uint Value { get; }

        public override bool Equals(object obj) => obj is ProfileSignature other && Equals(other);

        public bool Equals(ProfileSignature other) => this.Value == other.Value;

        public override int GetHashCode() => unchecked(-1937169414 + this.Value.GetHashCode());

        public override string ToString()
        {
           StringBuilder builder = new(32);

            uint value = this.Value;

            builder.Append('\'');

            for (int i = 0; i <= 3; i++)
            {
                int shift = BitConverter.IsLittleEndian ? (3 - i) * 8 : i * 8;

                uint c = (value >> shift) & 0xff;

                // Ignore any bytes that are not printable ASCII characters
                // because they can not be displayed in the debugger watch windows.

                if (c >= 0x20 && c <= 0x7e)
                {
                    builder.Append((char)c);
                }
            }

            _ = builder.AppendFormat("\' (0x{0:X8})", value);

            return builder.ToString();
        }

        public static bool operator ==(ProfileSignature left, ProfileSignature right) => left.Equals(right);

        public static bool operator !=(ProfileSignature left, ProfileSignature right) => !(left == right);
    }
}
