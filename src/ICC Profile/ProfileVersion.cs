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
using System.Globalization;

namespace HeicFileTypePlus.ICCProfile
{
    [DebuggerDisplay("{ToString(), nq}")]
    internal readonly struct ProfileVersion : IEquatable<ProfileVersion>
    {
        private readonly uint packedVersion;

        public ProfileVersion(ReadOnlySpan<byte> bytes) => this.packedVersion = BinaryPrimitives.ReadUInt32BigEndian(bytes);

        public uint Major => (this.packedVersion >> 24) & 0xff;

        public uint Minor => HighNibble(this.packedVersion >> 16);

        public uint Fix => LowNibble(this.packedVersion >> 16);

        public override bool Equals(object obj) => obj is ProfileVersion other && Equals(other);

        public bool Equals(ProfileVersion other) => this.packedVersion == other.packedVersion;

        public override int GetHashCode() => unchecked(-1289286301 + this.packedVersion.GetHashCode());

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", this.Major, this.Minor, this.Fix);

        public static bool operator ==(ProfileVersion left, ProfileVersion right) => left.Equals(right);

        public static bool operator !=(ProfileVersion left, ProfileVersion right) => !(left == right);

        private static uint HighNibble(uint value) => (value >> 4) & 0xf;

        private static uint LowNibble(uint value) => value & 0xf;
    }
}
