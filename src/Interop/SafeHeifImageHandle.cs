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

using Microsoft.Win32.SafeHandles;

namespace HeicFileTypePlus.Interop
{
    internal abstract class SafeHeifImageHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected SafeHeifImageHandle(bool ownsHandle) : base(ownsHandle)
        {
        }
    }

    internal sealed class SafeHeifImageHandleX64 : SafeHeifImageHandle
    {
        public SafeHeifImageHandleX64() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return HeicIO_x64.DeleteImageHandle(this.handle);
        }
    }

    internal sealed class SafeHeifImageHandleX86 : SafeHeifImageHandle
    {
        public SafeHeifImageHandleX86() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return HeicIO_x86.DeleteImageHandle(this.handle);
        }
    }

    internal sealed class SafeHeifImageHandleARM64 : SafeHeifImageHandle
    {
        public SafeHeifImageHandleARM64() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return HeicIO_ARM64.DeleteImageHandle(this.handle);
        }
    }
}
