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

using System;
using System.Diagnostics;

namespace HeicFileTypePlus.Exif
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    internal readonly struct MetadataKey
        : IEquatable<MetadataKey>
    {
        public MetadataKey(MetadataSection section, ushort tagId)
        {
            this.Section = section;
            this.TagId = tagId;
        }

        public MetadataSection Section { get; }

        public ushort TagId { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return string.Format("{0}, Tag# {1} (0x{1:X})", this.Section, this.TagId);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataKey other && Equals(other);
        }

        public bool Equals(MetadataKey other)
        {
            return this.Section == other.Section && this.TagId == other.TagId;
        }

        public override int GetHashCode()
        {
            int hashCode = -2103575766;

            unchecked
            {
                hashCode = (hashCode * -1521134295) + this.Section.GetHashCode();
                hashCode = (hashCode * -1521134295) + this.TagId.GetHashCode();
            }

            return hashCode;
        }

        public static bool operator ==(MetadataKey left, MetadataKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MetadataKey left, MetadataKey right)
        {
            return !(left == right);
        }
    }
}
