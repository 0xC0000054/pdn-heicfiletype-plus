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
using System.Runtime.InteropServices;

namespace HeicFileTypePlus.ICCProfile.Numeric
{
    /// <summary>
    /// Represents a signed fixed-point value with 15 integer bits and 16 fractional bits.
    /// </summary>
    /// <seealso cref="IEquatable{S15Fixed16}" />
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    [DebuggerTypeProxy(typeof(DebugView))]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct S15Fixed16 : IEquatable<S15Fixed16>, IFormattable
    {
        private readonly int fixedValue;

        public S15Fixed16(ReadOnlySpan<byte> bytes)
            => this.fixedValue = BinaryPrimitives.ReadInt32BigEndian(bytes);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => ToString(NumberFormatInfo.InvariantInfo);

        public override bool Equals(object? obj) => obj is S15Fixed16 other && Equals(other);

        public bool Equals(S15Fixed16 other) => this.fixedValue == other.fixedValue;

        public override int GetHashCode() => unchecked(-970009898 + this.fixedValue.GetHashCode());

        public float ToFloat() => this.fixedValue / 65536.0f;

        public override string ToString() => ToString(NumberFormatInfo.CurrentInfo);

        public string ToString(IFormatProvider formatProvider) => ToString("G", formatProvider);

        public string ToString(string? format, IFormatProvider? formatProvider)
            => ToFloat().ToString(format, formatProvider);

        public static bool operator ==(S15Fixed16 left, S15Fixed16 right) => left.Equals(right);

        public static bool operator !=(S15Fixed16 left, S15Fixed16 right) => !(left == right);

        private sealed class DebugView
        {
            private readonly S15Fixed16 value;

            public DebugView(S15Fixed16 value) => this.value = value;

            public int FixedValue => this.value.fixedValue;

            public float FloatValue => this.value.ToFloat();
        }
    }
}
