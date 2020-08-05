// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020 Nicholas Hayes
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

namespace HeicFileTypePlus.Interop
{
    // These values are from the ITU-T H.273 (2016) specification.
    // https://www.itu.int/rec/T-REC-H.273-201612-I/en

    internal enum CICPMatrixCoefficients
    {
        /// <summary>
        /// Identity matrix
        /// </summary>
        Identity = 0,

        /// <summary>
        /// BT.709
        /// </summary>
        BT709 = 1,

        /// <summary>
        /// Unspecified
        /// </summary>
        Unspecified = 2,

        /// <summary>
        /// For future use
        /// </summary>
        Reserved3 = 3,

        /// <summary>
        /// US FCC 73.628
        /// </summary>
        FCC = 4,

        /// <summary>
        /// BT.470 System B, G (historical)
        /// </summary>
        BT470BG = 5,

        /// <summary>
        /// BT.601
        /// </summary>
        BT601 = 6,

        /// <summary>
        /// SMPTE 240 M
        /// </summary>
        Smpte240 = 7,

        /// <summary>
        /// YCgCo
        /// </summary>
        YCgCo = 8
    }
}
