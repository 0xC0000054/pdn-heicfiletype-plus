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
using System.Diagnostics;
using System.Globalization;

namespace HeicFileTypePlus.ICCProfile.Numeric
{
    [DebuggerDisplay("{ToString(), nq}")]
    internal readonly struct XYZNumber : IEquatable<XYZNumber>
    {
        public XYZNumber(ReadOnlySpan<byte> bytes)
        {
            this.X = new S15Fixed16(bytes);
            this.Y = new S15Fixed16(bytes[4..]);
            this.Z = new S15Fixed16(bytes[8..]);
        }

        public S15Fixed16 X { get; }

        public S15Fixed16 Y { get; }

        public S15Fixed16 Z { get; }

        public override bool Equals(object? obj) => obj is XYZNumber other && Equals(other);

        public bool Equals(XYZNumber other) => this.X == other.X && this.Y == other.Y && this.Z == other.Z;

        public override int GetHashCode() => HashCode.Combine(this.X, this.Y, this.Z);

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                                                           "X = {0}, Y = {1}, Z = {2}",
                                                           this.X.ToString(CultureInfo.InvariantCulture),
                                                           this.Y.ToString(CultureInfo.InvariantCulture),
                                                           this.Z.ToString(CultureInfo.InvariantCulture));

        public static bool operator ==(XYZNumber left, XYZNumber right) => left.Equals(right);

        public static bool operator !=(XYZNumber left, XYZNumber right) => !(left == right);
    }
}
