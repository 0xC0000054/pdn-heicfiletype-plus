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

using HeicFileTypePlus.Exif;
using HeicFileTypePlus.Interop;
using PaintDotNet;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;
using System.Collections.Generic;
using System.IO;

namespace HeicFileTypePlus
{
    internal static class HeicSave
    {
        // The libheif x265 encoder cannot encode images
        // with dimensions that are smaller than this value.
        private const int MinimumEncodeSize = 64;

        public static void Save(
            Document input,
            Stream output,
            Surface scratchSurface,
            int quality,
            YUVChromaSubsampling chromaSubsampling,
            EncoderPreset preset,
            EncoderTuning tuning,
            int tuIntraDepth,
            ProgressEventHandler progressEventHandler)
        {
            if (input.Width < MinimumEncodeSize || input.Height < MinimumEncodeSize)
            {
                throw new FormatException($"The image must be at least { MinimumEncodeSize }x{ MinimumEncodeSize } pixels.");
            }

            scratchSurface.Clear();
            input.CreateRenderer().Render(scratchSurface);

            bool grayscale = IsGrayscaleImage(scratchSurface);

            EncoderOptions options = new()
            {
                quality = quality,
                // YUV 4:0:0 is always used for gray-scale images because it
                // produces the smallest file size with no quality loss.
                yuvFormat = grayscale ? YUVChromaSubsampling.Subsampling400 : chromaSubsampling,
                preset = preset,
                tuning = tuning,
                tuIntraDepth = tuIntraDepth
            };

            EncoderMetadata metadata = CreateEncoderMetadata(input);

            // Use BT.709 with sRGB transfer characteristics as the default.
            CICPColorData colorData = new()
            {
                colorPrimaries = CICPColorPrimaries.BT709,
                transferCharacteristics = CICPTransferCharacteristics.Srgb,
                matrixCoefficients = CICPMatrixCoefficients.BT709,
                fullRange = true
            };

            if (quality == 100 && !grayscale)
            {
                // The Identity matrix coefficient places the RGB values into the YUV planes without any conversion.
                // This reduces the compression efficiency, but allows for fully lossless encoding.

                options.yuvFormat = YUVChromaSubsampling.IdentityMatrix;
                colorData = new CICPColorData
                {
                    colorPrimaries = CICPColorPrimaries.BT709,
                    transferCharacteristics = CICPTransferCharacteristics.Srgb,
                    matrixCoefficients = CICPMatrixCoefficients.Identity,
                    fullRange = true
                };
            }
            else
            {
                string serializedCICPData = input.Metadata.GetUserValue(HeicMetadataNames.CICPMetadataName);
                if (serializedCICPData != null)
                {
                    CICPColorData? serializedColorData = CICPSerializer.TryDeserialize(serializedCICPData);

                    if (serializedColorData.HasValue)
                    {
                        colorData = serializedColorData.Value;
                    }
                }
            }

            using (HeifFileIO fileIO = new(output, leaveOpen: true))
            {
                HeicNative.SaveToFile(scratchSurface, options, metadata, ref colorData, fileIO, ReportProgress);
            }

            bool ReportProgress(double progress)
            {
                try
                {
                    progressEventHandler?.Invoke(null, new ProgressEventArgs(progress, true));
                    return true;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
        }

        private static EncoderMetadata CreateEncoderMetadata(Document doc)
        {
            byte[] iccProfileBytes = null;
            byte[] exifBytes = null;
            byte[] xmpBytes = null;

            Dictionary<ExifPropertyPath, ExifValue> exifMetadata = GetExifMetadataFromDocument(doc);

            if (exifMetadata != null)
            {
                Exif.ExifColorSpace exifColorSpace = Exif.ExifColorSpace.Srgb;

                ExifPropertyPath iccProfileKey = ExifPropertyKeys.Image.InterColorProfile.Path;

                if (exifMetadata.TryGetValue(iccProfileKey, out ExifValue iccProfileItem))
                {
                    iccProfileBytes = PaintDotNet.Collections.EnumerableExtensions.ToArrayEx(iccProfileItem.Data);
                    exifMetadata.Remove(iccProfileKey);
                    exifColorSpace = Exif.ExifColorSpace.Uncalibrated;
                }

                exifBytes = new ExifWriter(doc, exifMetadata, exifColorSpace).CreateExifBlob();
            }

            XmpPacket xmpPacket = doc.Metadata.TryGetXmpPacket();
            if (xmpPacket != null)
            {
                string packetAsString = xmpPacket.ToString(XmpPacketWrapperType.ReadOnly);

                xmpBytes = System.Text.Encoding.UTF8.GetBytes(packetAsString);
            }

            return new EncoderMetadata(iccProfileBytes, exifBytes, xmpBytes);
        }

        private static Dictionary<ExifPropertyPath, ExifValue> GetExifMetadataFromDocument(Document doc)
        {
            Dictionary<ExifPropertyPath, ExifValue> items = null;

            Metadata metadata = doc.Metadata;
            ExifPropertyItem[] exifProperties = metadata.GetExifPropertyItems();

            if (exifProperties.Length > 0)
            {
                items = new Dictionary<ExifPropertyPath, ExifValue>(exifProperties.Length);

                foreach (ExifPropertyItem property in exifProperties)
                {
                    items.TryAdd(property.Path, property.Value);
                }
            }

            return items;
        }

        private static unsafe bool IsGrayscaleImage(Surface surface)
        {
            for (int y = 0; y < surface.Height; y++)
            {
                ColorBgra* ptr = surface.GetRowPointerUnchecked(y);
                ColorBgra* ptrEnd = ptr + surface.Width;

                while (ptr < ptrEnd)
                {
                    if (!(ptr->R == ptr->G && ptr->G == ptr->B))
                    {
                        return false;
                    }

                    ptr++;
                }
            }

            return true;
        }
    }
}
