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

namespace HeicFileTypePlus.Exif
{
    internal static class TiffConstants
    {
        internal const ushort BigEndianByteOrderMarker = 0x4d4d;
        internal const ushort LittleEndianByteOrderMarker = 0x4949;
        internal const ushort Signature = 42;

        internal static class Tags
        {
            internal const ushort StripOffsets = 273;
            internal const ushort RowsPerStrip = 278;
            internal const ushort StripByteCounts = 279;
            internal const ushort SubIFDs = 330;
            internal const ushort ThumbnailOffset = 513;
            internal const ushort ThumbnailLength = 514;
            internal const ushort ExifIFD = 34665;
            internal const ushort GpsIFD = 34853;
            internal const ushort InteropIFD = 40965;
        }

        internal static class Orientation
        {
            /// <summary>
            /// The 0th row is at the visual top of the image, and the 0th column is the visual left-hand side
            /// </summary>
            internal const ushort TopLeft = 1;

            /// <summary>
            /// The 0th row is at the visual top of the image, and the 0th column is the visual right-hand side.
            /// </summary>
            internal const ushort TopRight = 2;

            /// <summary>
            /// The 0th row represents the visual bottom of the image, and the 0th column represents the visual right-hand side.
            /// </summary>
            internal const ushort BottomRight = 3;

            /// <summary>
            /// The 0th row represents the visual bottom of the image, and the 0th column represents the visual left-hand side.
            /// </summary>
            internal const ushort BottomLeft = 4;

            /// <summary>
            /// The 0th row represents the visual left-hand side of the image, and the 0th column represents the visual top.
            /// </summary>
            internal const ushort LeftTop = 5;

            /// <summary>
            /// The 0th row represents the visual right-hand side of the image, and the 0th column represents the visual top.
            /// </summary>
            internal const ushort RightTop = 6;

            /// <summary>
            /// The 0th row represents the visual right-hand side of the image, and the 0th column represents the visual bottom.
            /// </summary>
            internal const ushort RightBottom = 7;

            /// <summary>
            /// The 0th row represents the visual left-hand side of the image, and the 0th column represents the visual bottom.
            /// </summary>
            internal const ushort LeftBottom = 8;
        }
    }
}
