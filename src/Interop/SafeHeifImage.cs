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

using Microsoft.Win32.SafeHandles;

namespace HeicFileTypePlus.Interop
{
    internal abstract class SafeHeifImage : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected SafeHeifImage(bool owns) : base(owns)
        {
        }
    }

    internal sealed class SafeHeifImageX64 : SafeHeifImage
    {
        public SafeHeifImageX64() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return HeicIO_x64.DeleteImage(this.handle);
        }
    }

    internal sealed class SafeHeifImageARM64 : SafeHeifImage
    {
        public SafeHeifImageARM64() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return HeicIO_ARM64.DeleteImage(this.handle);
        }
    }
}
