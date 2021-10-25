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

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace HeicFileTypePlus.Interop
{
    internal sealed class SafeCoTaskMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeCoTaskMemHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        public static SafeCoTaskMemHandle Allocate(int size)
        {
            SafeCoTaskMemHandle handle;

            IntPtr memory = IntPtr.Zero;
            try
            {
                memory = Marshal.AllocCoTaskMem(size);

                handle = new SafeCoTaskMemHandle(memory, true);

                memory = IntPtr.Zero;
            }
            finally
            {
                if (memory != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(memory);
                }
            }

            return handle;
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeCoTaskMem(this.handle);
            return true;
        }
    }
}
