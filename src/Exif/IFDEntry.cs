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

namespace HeicFileTypePlus.Exif
{
    internal readonly struct IFDEntry
        : IEquatable<IFDEntry>
    {
        public const int SizeOf = 12;

        public IFDEntry(EndianBinaryReader reader)
        {
            this.Tag = reader.ReadUInt16();
            this.Type = (TagDataType)reader.ReadUInt16();
            this.Count = reader.ReadUInt32();
            this.Offset = reader.ReadUInt32();
        }

        public IFDEntry(ushort tag, TagDataType type, uint count, uint offset)
        {
            this.Tag = tag;
            this.Type = type;
            this.Count = count;
            this.Offset = offset;
        }

        public ushort Tag { get; }

        public TagDataType Type { get; }

        public uint Count { get; }

        public uint Offset { get; }

        public override bool Equals(object obj)
        {
            return obj is IFDEntry entry && Equals(entry);
        }

        public bool Equals(IFDEntry other)
        {
            return this.Tag == other.Tag &&
                   this.Type == other.Type &&
                   this.Count == other.Count &&
                   this.Offset == other.Offset;
        }

        public override int GetHashCode()
        {
            int hashCode = 1198491158;

            unchecked
            {
                hashCode = (hashCode * -1521134295) + this.Tag.GetHashCode();
                hashCode = (hashCode * -1521134295) + this.Count.GetHashCode();
                hashCode = (hashCode * -1521134295) + this.Type.GetHashCode();
                hashCode = (hashCode * -1521134295) + this.Offset.GetHashCode();
            }

            return hashCode;
        }

        public void Write(System.IO.BinaryWriter writer)
        {
            writer.Write(this.Tag);
            writer.Write((ushort)this.Type);
            writer.Write(this.Count);
            writer.Write(this.Offset);
        }

        public static bool operator ==(IFDEntry left, IFDEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IFDEntry left, IFDEntry right)
        {
            return !(left == right);
        }
    }
}
