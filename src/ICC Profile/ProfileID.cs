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
    internal sealed class ProfileID : IEquatable<ProfileID>
    {
        private readonly UInt128 profileID;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileID"/> class.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <exception cref="ArgumentException"><paramref name="span"/> must be at least 16 bytes in length.</exception>
        public ProfileID(ReadOnlySpan<byte> span)
        {
            this.profileID = BinaryPrimitives.ReadUInt128BigEndian(span);
        }

        public bool IsEmpty => this.profileID == 0;

        public override bool Equals(object obj) => Equals(obj as ProfileID);

        public bool Equals(ProfileID other) => other is not null && this.profileID.Equals(other.profileID);

        public override int GetHashCode() => unchecked(1584140826 + this.profileID.GetHashCode());

        public override string ToString()
        {
            return this.profileID.ToString("X");
        }

        public static bool operator ==(ProfileID left, ProfileID right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.profileID == right.profileID;
        }

        public static bool operator !=(ProfileID left, ProfileID right) => !(left == right);
    }
}
