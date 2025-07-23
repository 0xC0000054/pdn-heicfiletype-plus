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

using System;
using System.Runtime.InteropServices;

namespace HeicFileTypePlus.Interop
{
    internal sealed class EncoderMetadataCustomMarshaler : ICustomMarshaler
    {
        // This must be kept in sync with the EncoderMetadata structure in HeicFileTypePlusIO.h.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeEncoderMetadata
        {
            public IntPtr iccProfile;
            public IntPtr exif;
            public IntPtr xmp;
            public int iccProfileSize;
            public int exifSize;
            public int xmpSize;
        }

        private static readonly int NativeEncoderMetadataSize = Marshal.SizeOf(typeof(NativeEncoderMetadata));
        private static readonly EncoderMetadataCustomMarshaler instance = new();

#pragma warning disable IDE0060 // Remove unused parameter
        public static ICustomMarshaler GetInstance(string cookie)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            return instance;
        }

        private EncoderMetadataCustomMarshaler()
        {
        }

        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            unsafe
            {
                if (pNativeData != IntPtr.Zero)
                {
                    NativeEncoderMetadata* metadata = (NativeEncoderMetadata*)pNativeData;

                    if (metadata->iccProfile != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(metadata->iccProfile);
                    }

                    if (metadata->exif != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(metadata->exif);
                    }

                    if (metadata->xmp != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(metadata->xmp);
                    }

                    Marshal.FreeHGlobal(pNativeData);
                }
            }
        }

        public int GetNativeDataSize()
        {
            return NativeEncoderMetadataSize;
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj == null)
            {
                return IntPtr.Zero;
            }

            EncoderMetadata metadata = (EncoderMetadata)ManagedObj;

            IntPtr nativeStructure = Marshal.AllocHGlobal(NativeEncoderMetadataSize);

            unsafe
            {
                NativeEncoderMetadata* nativeMetadata = (NativeEncoderMetadata*)nativeStructure;

                if (metadata.iccProfile != null && metadata.iccProfile.Length > 0)
                {
                    nativeMetadata->iccProfile = Marshal.AllocHGlobal(metadata.iccProfile.Length);
                    Marshal.Copy(metadata.iccProfile, 0, nativeMetadata->iccProfile, metadata.iccProfile.Length);
                    nativeMetadata->iccProfileSize = metadata.iccProfile.Length;
                }
                else
                {
                    nativeMetadata->iccProfile = IntPtr.Zero;
                    nativeMetadata->iccProfileSize = 0;
                }

                if (metadata.exif != null && metadata.exif.Length > 0)
                {
                    nativeMetadata->exif = Marshal.AllocHGlobal(metadata.exif.Length);
                    Marshal.Copy(metadata.exif, 0, nativeMetadata->exif, metadata.exif.Length);
                    nativeMetadata->exifSize = metadata.exif.Length;
                }
                else
                {
                    nativeMetadata->exif = IntPtr.Zero;
                    nativeMetadata->exifSize = 0;
                }

                if (metadata.xmp != null && metadata.xmp.Length > 0)
                {
                    nativeMetadata->xmp = Marshal.AllocHGlobal(metadata.xmp.Length);
                    Marshal.Copy(metadata.xmp, 0, nativeMetadata->xmp, metadata.xmp.Length);
                    nativeMetadata->xmpSize = metadata.xmp.Length;
                }
                else
                {
                    nativeMetadata->xmp = IntPtr.Zero;
                    nativeMetadata->xmpSize = 0;
                }
            }

            return nativeStructure;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return null!;
        }
    }
}
