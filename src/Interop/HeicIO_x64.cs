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
    internal static class HeicIO_x64
    {
        private const string DllName = "HeicFileTypePlusIO_x64.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern SafeHeifContextX64 CreateContext();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool DeleteContext(IntPtr context);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool DeleteImageHandle(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status LoadFileIntoContext(
            SafeHeifContext context,
            SafeHandle callbacks,
            [MarshalAs(UnmanagedType.FunctionPtr)] HeicErrorDetailsCopy copyErrorDetails);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status GetPrimaryImage(SafeHeifContext context,
                                                      out SafeHeifImageHandleX64 primaryImageHandle,
                                                      [In, Out] PrimaryImageInfo info,
                                                      [MarshalAs(UnmanagedType.FunctionPtr)] HeicErrorDetailsCopy copyErrorDetails);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status DecodeImage(SafeHeifImageHandle imageHandle, [In] ref BitmapData bitmapData);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status GetICCProfileSize(SafeHeifImageHandle imageHandle, out nuint size);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status GetICCProfile(SafeHeifImageHandle imageHandle, byte[] buffer, nuint bufferSize);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status GetCICPColorData(SafeHeifImageHandle imageHandle, out CICPColorData colorData);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status GetMetadataSize(SafeHeifImageHandle imageHandle, MetadataType metadataType, out nuint size);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status GetMetadata(SafeHeifImageHandle imageHandle, MetadataType metadataType, byte[] buffer, nuint bufferSize);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern Status SaveToFile([In] ref BitmapData bitmapData,
                                                 EncoderOptions options,
                                                 [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(EncoderMetadataCustomMarshaler))] EncoderMetadata metadata,
                                                 [In] ref CICPColorData colorData,
                                                 SafeHandle callbacks,
                                                 [MarshalAs(UnmanagedType.FunctionPtr)] HeifProgressCallback progressCallback);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe nuint GetLibDe265VersionString(byte* buffer, nuint length);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe nuint GetLibHeifVersionString(byte* buffer, nuint length);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe nuint GetX265VersionString(byte* buffer, nuint length);
    }
}
