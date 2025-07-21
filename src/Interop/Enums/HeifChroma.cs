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


namespace HeicFileTypePlus.Interop
{
    // These values must match the heif_chroma enumeration in heif.h.

    public enum HeifChroma
    {
        Undefined = 99,
        Monochrome = 0,
        Yuv420 = 1,
        Yuv422 = 2,
        Yuv444 = 3,
        InterleavedRgb24 = 10,
        InterleavedRgba32 = 11,
        InterleavedRgb48BE = 12,
        InterleavedRgba64BE = 13,
        InterleavedRgb48LE = 14,
        InterleavedRgba64LE = 15
    }
}
