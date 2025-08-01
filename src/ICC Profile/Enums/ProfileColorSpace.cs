﻿// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
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

namespace HeicFileTypePlus.ICCProfile
{
    internal enum ProfileColorSpace : uint
    {
        Xyz = 0x58595A20,              // 'XYZ '
        Lab = 0x4C616220,              // 'Lab '
        Luv = 0x4C757620,              // 'Luv '
        YCbCr = 0x59436272,            // 'YCbr'
        Yxy = 0x59787920,              // 'Yxy '
        Rgb = 0x52474220,              // 'RGB '
        Gray = 0x47524159,             // 'GRAY'
        Hsv = 0x48535620,              // 'HSV '
        Hls = 0x484C5320,              // 'HLS '
        Cmyk = 0x434D594B,             // 'CMYK'
        Cmy = 0x434D5920,              // 'CMY '
        OneChannel = 0x31434C52,        // '1CLR'
        TwoChannel = 0x32434C52,        // '2CLR'
        ThreeChannel = 0x33434C52,        // '3CLR'
        FourChannel = 0x34434C52,        // '4CLR'
        FiveChannel = 0x35434C52,        // '5CLR'
        SixChannel = 0x36434C52,        // '6CLR'
        SevenChannel = 0x37434C52,        // '7CLR'
        EightChannel = 0x38434C52,        // '8CLR'
        NineChannel = 0x39434C52,        // '9CLR'
        TenChannel = 0x41434C52,       // 'ACLR'
        ElevenChannel = 0x42434C52,       // 'BCLR'
        TwelveChannel = 0x43434C52,       // 'CCLR'
        ThirteenChannel = 0x44434C52,       // 'DCLR'
        FourteenChannel = 0x45434C52,       // 'ECLR'
        FifteenChannel = 0x46434C52,       // 'FCLR'
    }
}
