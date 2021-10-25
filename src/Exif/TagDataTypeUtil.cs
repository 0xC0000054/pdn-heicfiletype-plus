// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020, 2021 Nicholas Hayes
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
    internal static class TagDataTypeUtil
    {
        /// <summary>
        /// Determines whether the <see cref="TagDataType"/> is known to GDI+.
        /// </summary>
        /// <param name="type">The tag type.</param>
        /// <returns>
        /// <see langword="true"/> if the tag type is known to GDI+; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsKnownToGDIPlus(TagDataType type)
        {
            switch (type)
            {
                case TagDataType.Byte:
                case TagDataType.Ascii:
                case TagDataType.Short:
                case TagDataType.Long:
                case TagDataType.Rational:
                case TagDataType.Undefined:
                case TagDataType.SLong:
                case TagDataType.SRational:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the size in bytes of a <see cref="TagDataType"/> value.
        /// </summary>
        /// <param name="type">The tag type.</param>
        /// <returns>
        /// The size of the value in bytes.
        /// </returns>
        public static int GetSizeInBytes(TagDataType type)
        {
            switch (type)
            {
                case TagDataType.Byte:
                case TagDataType.Ascii:
                case TagDataType.Undefined:
                case TagDataType.SByte:
                    return 1;
                case TagDataType.Short:
                case TagDataType.SShort:
                    return 2;
                case TagDataType.Long:
                case TagDataType.SLong:
                case TagDataType.Float:
                case TagDataType.IFD:
                    return 4;
                case TagDataType.Rational:
                case TagDataType.SRational:
                case TagDataType.Double:
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
        public static bool ValueFitsInOffsetField(TagDataType type, uint count)
        {
            switch (type)
            {
                case TagDataType.Byte:
                case TagDataType.Ascii:
                case TagDataType.Undefined:
                case TagDataType.SByte:
                    return count <= 4;
                case TagDataType.Short:
                case TagDataType.SShort:
                    return count <= 2;
                case TagDataType.Long:
                case TagDataType.SLong:
                case TagDataType.Float:
                case TagDataType.IFD:
                    return count <= 1;
                case TagDataType.Rational:
                case TagDataType.SRational:
                case TagDataType.Double:
                default:
                    return false;
            }
        }
    }
}
