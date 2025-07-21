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

namespace HeicFileTypePlus.ICCProfile
{
    internal enum ProfileClass : uint
    {
        Input = 0x73636E72,       // 'scnr'
        Display = 0x6D6E7472,     // 'mntr'
        Output = 0x70727472,      // 'prtr'
        Link = 0x6C696E6B,        // 'link'
        Abstract = 0x61627374,    // 'abst'
        ColorSpace = 0x73706163,  // 'spac'
        NamedColor = 0x6e6d636c   // 'nmcl'
    }
}
