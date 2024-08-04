// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020, 2021, 2022, 2024 Nicholas Hayes
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

namespace HeicFileTypePlus
{
    // This must be kept in sync with the YUVChromaSubsampling enumeration in HeicFileTypePlusIO.h.
    internal enum YUVChromaSubsampling
    {
        /// <summary>
        /// YUV 4:0:0
        /// </summary>
        /// <remarks>
        /// Used internally for gray-scale images, not shown to the user.
        /// </remarks>
        Subsampling400,

        /// <summary>
        /// YUV 4:2:0
        /// </summary>
        Subsampling420,

        /// <summary>
        /// YUV 4:2:2
        /// </summary>
        Subsampling422,

        /// <summary>
        /// YUV 4:4:4
        /// </summary>
        Subsampling444,

        /// <summary>
        /// The RGB color values are used directly, without YUV conversion.
        /// </summary>
        /// <remarks>
        /// Used internally for lossless RGB encoding, not shown to the user.
        /// </remarks>
        IdentityMatrix
    }
}
