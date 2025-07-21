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

namespace HeicFileTypePlus.ICCProfile
{
    [DebuggerDisplay("{ToString(), nq}")]
    internal readonly struct ProfileDateTime : IEquatable<ProfileDateTime>
    {
        public ProfileDateTime(ReadOnlySpan<byte> bytes)
        {
            this.Year = BinaryPrimitives.ReadUInt16BigEndian(bytes);
            this.Month = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
            this.Day = BinaryPrimitives.ReadUInt16BigEndian(bytes[4..]);
            this.Hour = BinaryPrimitives.ReadUInt16BigEndian(bytes[6..]);
            this.Minute = BinaryPrimitives.ReadUInt16BigEndian(bytes[8..]);
            this.Second = BinaryPrimitives.ReadUInt16BigEndian(bytes[10..]);
        }

        public ushort Year { get; }

        public ushort Month { get; }

        public ushort Day { get; }

        public ushort Hour { get; }

        public ushort Minute { get; }

        public ushort Second { get; }

        public override bool Equals(object obj) => obj is ProfileDateTime other && Equals(other);

        public bool Equals(ProfileDateTime other) => this.Year == other.Year &&
                                                     this.Month == other.Month &&
                                                     this.Day == other.Day &&
                                                     this.Hour == other.Hour &&
                                                     this.Minute == other.Minute &&
                                                     this.Second == other.Second;

        public override int GetHashCode() => HashCode.Combine(this.Year, this.Month, this.Day, this.Hour, this.Minute, this.Second);

        public DateTime ToDateTime() => new(this.Year, this.Month, this.Day, this.Hour, this.Minute, this.Second);

        public override string ToString() => ToDateTime().ToString();

        public static bool operator ==(ProfileDateTime left, ProfileDateTime right) => left.Equals(right);

        public static bool operator !=(ProfileDateTime left, ProfileDateTime right) => !(left == right);
    }
}
