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

namespace HeicFileTypePlus.Exif
{
    internal static class EndianUtil
    {
        public static ushort Swap(ushort value)
        {
            return (ushort)(((value & 0xff00) >> 8) | ((value & 0x00ff) << 8));
        }

        public static uint Swap(uint value)
        {
            return ((value & 0xff000000) >> 24) |
                   ((value & 0x00ff0000) >> 8 ) |
                   ((value & 0x0000ff00) << 8 ) |
                   ((value & 0x000000ff) << 24);
        }

        public static ulong Swap(ulong value)
        {
            return ((value & 0xff00000000000000) >> 56) |
                   ((value & 0x00ff000000000000) >> 40) |
                   ((value & 0x0000ff0000000000) >> 24) |
                   ((value & 0x000000ff00000000) >> 8 ) |
                   ((value & 0x00000000ff000000) << 8 ) |
                   ((value & 0x0000000000ff0000) << 24) |
                   ((value & 0x000000000000ff00) << 40) |
                   ((value & 0x00000000000000ff) << 56);
        }
    }
}
