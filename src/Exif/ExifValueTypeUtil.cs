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

using PaintDotNet.Imaging;

namespace HeicFileTypePlus.Exif
{
    internal static class ExifValueTypeUtil
    {
        /// <summary>
        /// Gets the size in bytes of a <see cref="TagDataType"/> value.
        /// </summary>
        /// <param name="type">The tag type.</param>
        /// <returns>
        /// The size of the value in bytes.
        /// </returns>
        public static int GetSizeInBytes(ExifValueType type)
        {
            switch (type)
            {
                case ExifValueType.Byte:
                case ExifValueType.Ascii:
                case ExifValueType.Undefined:
                case (ExifValueType)6: // SByte
                    return 1;
                case ExifValueType.Short:
                case ExifValueType.SShort:
                    return 2;
                case ExifValueType.Long:
                case ExifValueType.SLong:
                case ExifValueType.Float:
                case (ExifValueType)13: // IFD
                    return 4;
                case ExifValueType.Rational:
                case ExifValueType.SRational:
                case ExifValueType.Double:
                    return 8;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Determines whether the values fit in the offset field.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// <see langword="true"/> if the values fit in the offset field; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool ValueFitsInOffsetField(ExifValueType type, uint count)
        {
            switch (type)
            {
                case ExifValueType.Byte:
                case ExifValueType.Ascii:
                case ExifValueType.Undefined:
                case (ExifValueType)6: // SByte
                    return count <= 4;
                case ExifValueType.Short:
                case ExifValueType.SShort:
                    return count <= 2;
                case ExifValueType.Long:
                case ExifValueType.SLong:
                case ExifValueType.Float:
                case (ExifValueType)13: // IFD
                    return count <= 1;
                case ExifValueType.Rational:
                case ExifValueType.SRational:
                case ExifValueType.Double:
                default:
                    return false;
            }
        }
    }
}
