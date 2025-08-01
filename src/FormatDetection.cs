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

using PaintDotNet;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace HeicFileTypePlus
{
    internal static class FormatDetection
    {
        private static ReadOnlySpan<byte> BmpFileSignature => new byte[] { 0x42, 0x4D };

        private static ReadOnlySpan<byte> Gif87aFileSignature => new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 };

        private static ReadOnlySpan<byte> Gif89aFileSignature => new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };

        private static ReadOnlySpan<byte> JpegFileSignature => new byte[] { 0xff, 0xd8, 0xff };

        private static ReadOnlySpan<byte> PngFileSignature => new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        private static ReadOnlySpan<byte> TiffBigEndianFileSignature => new byte[] { 0x4d, 0x4d, 0x00, 0x2a };

        private static ReadOnlySpan<byte> TiffLittleEndianFileSignature => new byte[] { 0x49, 0x49, 0x2a, 0x00 };

        /// <summary>
        /// Attempts to get an <see cref="IFileTypeInfo"/> from the file signature.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>
        ///   An <see cref="IFileTypeInfo"/> instance if the file has the signature of a recognized image format;
        ///   otherwise, <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// Some applications may save other common image formats (e.g. JPEG or PNG) with a .heif file extension.
        /// </remarks>
        internal static IFileTypeInfo? TryGetFileTypeInfo(Stream stream, IServiceProvider? serviceProvider)
        {
            string name = TryGetFileTypeName(stream);

            IFileTypeInfo? fileTypeInfo = null;

            if (!string.IsNullOrEmpty(name))
            {
                IFileTypesService? fileTypesService = serviceProvider?.GetService<IFileTypesService>();

                if (fileTypesService != null)
                {
                    foreach (IFileTypeInfo item in fileTypesService.FileTypes)
                    {
                        if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                            && item.Options.SupportsLoading)
                        {
                            fileTypeInfo = item;
                            break;
                        }
                    }
                }
            }

            return fileTypeInfo;
        }

        [SkipLocalsInit]
        private static string TryGetFileTypeName(Stream stream)
        {
            string name = string.Empty;

            Span<byte> signature = stackalloc byte[8];
            stream.ReadExactly(signature);

            if (FileSignatureMatches(signature, JpegFileSignature))
            {
                name = "JPEG";
            }
            else if (FileSignatureMatches(signature, PngFileSignature))
            {
                name = "PNG";
            }
            else if (FileSignatureMatches(signature, BmpFileSignature))
            {
                name = "BMP";
            }
            else if (IsGifFileSignature(signature))
            {
                name = "GIF";
            }
            else if (IsTiffFileSignature(signature))
            {
                name = "TIFF";
            }

            return name;
        }

        private static bool FileSignatureMatches(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
            => data.Length >= signature.Length && data.Slice(0, signature.Length).SequenceEqual(signature);

        private static bool IsGifFileSignature(ReadOnlySpan<byte> data)
        {
            bool result = false;

            if (data.Length >= Gif87aFileSignature.Length)
            {
                ReadOnlySpan<byte> bytes = data.Slice(0, Gif87aFileSignature.Length);

                result = bytes.SequenceEqual(Gif87aFileSignature)
                      || bytes.SequenceEqual(Gif89aFileSignature);
            }

            return result;
        }

        private static bool IsTiffFileSignature(ReadOnlySpan<byte> data)
        {
            bool result = false;

            if (data.Length >= TiffBigEndianFileSignature.Length)
            {
                ReadOnlySpan<byte> bytes = data.Slice(0, TiffBigEndianFileSignature.Length);

                result = bytes.SequenceEqual(TiffBigEndianFileSignature)
                      || bytes.SequenceEqual(TiffLittleEndianFileSignature);
            }

            return result;
        }
    }
}
