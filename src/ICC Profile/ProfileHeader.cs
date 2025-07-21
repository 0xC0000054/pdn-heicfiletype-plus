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

using HeicFileTypePlus.ICCProfile.Numeric;
using System;
using System.Buffers.Binary;

namespace HeicFileTypePlus.ICCProfile
{
    internal sealed class ProfileHeader
    {
        public const int SizeOf = 128;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileHeader"/> structure.
        /// </summary>
        /// <param name="profileBytes">The profile bytes.</param>
        public ProfileHeader(ReadOnlySpan<byte> profileBytes)
        {
            this.Size = BinaryPrimitives.ReadUInt32BigEndian(profileBytes);
            this.CmmType = new ProfileSignature(profileBytes[4..]);
            this.Version = new ProfileVersion(profileBytes[8..]);
            this.DeviceClass = (ProfileClass)BinaryPrimitives.ReadUInt32BigEndian(profileBytes[12..]);
            this.ColorSpace = (ProfileColorSpace)BinaryPrimitives.ReadUInt32BigEndian(profileBytes[16..]);
            this.ConnectionSpace = (ProfileColorSpace)BinaryPrimitives.ReadUInt32BigEndian(profileBytes[20..]);
            this.DateTime = new ProfileDateTime(profileBytes[24..]);
            this.Signature = new ProfileSignature(profileBytes[36..]);
            this.Platform = (ProfilePlatform)BinaryPrimitives.ReadUInt32BigEndian(profileBytes[40..]);
            this.ProfileFlags = BinaryPrimitives.ReadUInt32BigEndian(profileBytes[44..]);
            this.Manufacturer = new ProfileSignature(profileBytes[48..]);
            this.Model = new ProfileSignature(profileBytes[52..]);
            this.Attributes = BinaryPrimitives.ReadUInt64BigEndian(profileBytes[56..]);
            this.RenderingIntent = (RenderingIntent)BinaryPrimitives.ReadUInt32BigEndian(profileBytes[64..]);
            this.Illuminant = new XYZNumber(profileBytes[68..]);
            this.Creator = new ProfileSignature(profileBytes[80..]);
            this.ID = new ProfileID(profileBytes.Slice(84, 16));
        }

        public uint Size { get; }

        public ProfileSignature CmmType { get; }

        public ProfileVersion Version { get; }

        public ProfileClass DeviceClass { get; }

        public ProfileColorSpace ColorSpace { get; }

        public ProfileColorSpace ConnectionSpace { get; }

        public ProfileDateTime DateTime { get; }

        public ProfileSignature Signature { get; }

        public ProfilePlatform Platform { get; }

        public uint ProfileFlags { get; }

        public ProfileSignature Manufacturer { get; }

        public ProfileSignature Model { get; }

        public ulong Attributes { get; }

        public RenderingIntent RenderingIntent { get; }

        public XYZNumber Illuminant { get; }

        public ProfileSignature Creator { get; }

        public ProfileID ID { get; }
    }
}
