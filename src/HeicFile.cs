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

using HeicFileTypePlus.Exif;
using HeicFileTypePlus.Interop;
using PaintDotNet;
using PaintDotNet.Imaging;
using System;
using System.Collections.Generic;
using System.IO;

namespace HeicFileTypePlus
{
    internal static class HeicFile
    {
        private const string CICPMetadataName = "HeicFileTypePlusCICPData";
        // The libheif x265 encoder cannot encode images
        // with dimensions that are smaller than this value.
        private const int MinimumEncodeSize = 64;

        public static Document Load(Stream input)
        {
            Document doc = null;

            using (HeifFileIO fileIO = new HeifFileIO(input, leaveOpen: true))
            using (SafeHeifContext context = HeicNative.CreateContext())
            {
                HeicNative.LoadFileIntoContext(context, fileIO);

                SafeHeifImageHandle primaryImageHandle = null;
                PrimaryImageInfo primaryImageInfo;
                Surface surface = null;
                bool disposeSurface = true;

                try
                {
                    HeicNative.GetPrimaryImage(context, out primaryImageHandle, out primaryImageInfo);

                    surface = new Surface(primaryImageInfo.width, primaryImageInfo.height);

                    HeicNative.DecodeImage(primaryImageHandle, surface);

                    doc = new Document(surface.Width, surface.Height);
                    AddMetadataToDocument(doc, primaryImageHandle, primaryImageInfo);
                    doc.Layers.Add(Layer.CreateBackgroundLayer(surface, true));
                    disposeSurface = false;
                }
                finally
                {
                    primaryImageHandle?.Dispose();

                    if (disposeSurface)
                    {
                        surface?.Dispose();
                    }
                }
            }

            return doc;
        }

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

            using (RenderArgs args = new RenderArgs(scratchSurface))
            {
                input.Render(args, true);
            }

            EncoderOptions options = new EncoderOptions
            {
                quality = quality,
                // YUV 4:0:0 is always used for gray-scale images because it
                // produces the smallest file size with no quality loss.
                yuvFormat = IsGrayscaleImage(scratchSurface) ? YUVChromaSubsampling.Subsampling400 : chromaSubsampling,
                preset = preset,
                tuning = tuning,
                tuIntraDepth = tuIntraDepth
            };

            EncoderMetadata metadata = CreateEncoderMetadata(input);

            // Use BT.709 with sRGB transfer characteristics as the default.
            CICPColorData colorData = new CICPColorData
            {
                colorPrimaries = CICPColorPrimaries.BT709,
                transferCharacteristics = CICPTransferCharacteristics.Srgb,
                matrixCoefficients = CICPMatrixCoefficients.BT709,
                fullRange = true
            };

            string serializedCICPData = input.Metadata.GetUserValue(CICPMetadataName);
            if (serializedCICPData != null)
            {
                CICPColorData? serializedColorData = CICPSerializer.TryDeserialize(serializedCICPData);

                if (serializedColorData.HasValue)
                {
                    colorData = serializedColorData.Value;
                }
            }

            using (HeifFileIO fileIO = new HeifFileIO(output, leaveOpen: true))
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

        private static void AddMetadataToDocument(Document document, SafeHeifImageHandle primaryImageHandle, PrimaryImageInfo primaryImageInfo)
        {
            if (primaryImageInfo.hasExif)
            {
                byte[] exifData = TryGetMetadata(primaryImageHandle, MetadataType.Exif);

                if (exifData != null)
                {
                    ExifValueCollection metadataEntries = TryParseExifData(exifData);

                    if (metadataEntries != null)
                    {
                        metadataEntries.Remove(MetadataKeys.Image.InterColorProfile);
                        // The HEIF specification states that the EXIF orientation tag is only
                        // informational and should not be used to rotate the image.
                        // See https://github.com/strukturag/libheif/issues/227#issuecomment-642165942
                        metadataEntries.Remove(MetadataKeys.Image.Orientation);

                        foreach (MetadataEntry item in metadataEntries)
                        {
                            document.Metadata.AddExifPropertyItem(item.CreateExifPropertyItem());
                        }
                    }
                }
            }

            if (primaryImageInfo.colorProfileType == ColorProfileType.ICC)
            {
                ulong size = HeicNative.GetICCProfileSize(primaryImageHandle);

                if (size > 0 && size <= int.MaxValue)
                {
                    byte[] iccProfile = new byte[size];
                    HeicNative.GetICCProfile(primaryImageHandle, iccProfile);

                    document.Metadata.AddExifPropertyItem(ExifSection.Image,
                                                          unchecked((ushort)ExifTagID.IccProfileData),
                                                          new ExifValue(ExifValueType.Undefined, iccProfile));
                }
            }
            else if (primaryImageInfo.colorProfileType == ColorProfileType.CICP)
            {
                CICPColorData colorData;
                HeicNative.GetCICPColorData(primaryImageHandle, out colorData);

                string serializedValue = CICPSerializer.TrySerialize(colorData);

                if (serializedValue != null)
                {
                    document.Metadata.SetUserValue(CICPMetadataName, serializedValue);
                }
            }

            if (primaryImageInfo.hasXmp)
            {
                byte[] xmpData = TryGetMetadata(primaryImageHandle, MetadataType.Xmp);

                if (xmpData != null)
                {
                    XmpPacket packet = XmpPacket.TryParse(xmpData);
                    if (packet != null)
                    {
                        document.Metadata.SetXmpPacket(packet);
                    }
                }
            }
        }

        private static EncoderMetadata CreateEncoderMetadata(Document doc)
        {
            byte[] iccProfileBytes = null;
            byte[] exifBytes = null;
            byte[] xmpBytes = null;

            Dictionary<MetadataKey, MetadataEntry> exifMetadata = GetExifMetadataFromDocument(doc);

            if (exifMetadata != null)
            {
                Exif.ExifColorSpace exifColorSpace = Exif.ExifColorSpace.Srgb;

                MetadataKey iccProfileKey = MetadataKeys.Image.InterColorProfile;

                if (exifMetadata.TryGetValue(iccProfileKey, out MetadataEntry iccProfileItem))
                {
                    iccProfileBytes = iccProfileItem.GetData();
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

        private static Dictionary<MetadataKey, MetadataEntry> GetExifMetadataFromDocument(Document doc)
        {
            Dictionary<MetadataKey, MetadataEntry> items = null;

            Metadata metadata = doc.Metadata;

            ExifPropertyItem[] exifProperties = metadata.GetExifPropertyItems();

            if (exifProperties.Length > 0)
            {
                items = new Dictionary<MetadataKey, MetadataEntry>(exifProperties.Length);

                foreach (ExifPropertyItem property in exifProperties)
                {
                    MetadataSection section;
                    switch (property.Path.Section)
                    {
                        case ExifSection.Image:
                            section = MetadataSection.Image;
                            break;
                        case ExifSection.Photo:
                            section = MetadataSection.Exif;
                            break;
                        case ExifSection.Interop:
                            section = MetadataSection.Interop;
                            break;
                        case ExifSection.GpsInfo:
                            section = MetadataSection.Gps;
                            break;
                        default:
                            throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                              "Unexpected {0} type: {1}",
                                                                              nameof(ExifSection),
                                                                              (int)property.Path.Section));
                    }

                    MetadataKey metadataKey = new MetadataKey(section, property.Path.TagID);

                    if (!items.ContainsKey(metadataKey))
                    {
                        byte[] clonedData = PaintDotNet.Collections.EnumerableExtensions.ToArrayEx(property.Value.Data);

                        items.Add(metadataKey, new MetadataEntry(metadataKey, (TagDataType)property.Value.Type, clonedData));
                    }
                }
            }

            return items;
        }

        private static unsafe bool IsGrayscaleImage(Surface surface)
        {
            for (int y = 0; y < surface.Height; y++)
            {
                ColorBgra* ptr = surface.GetRowAddressUnchecked(y);
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

        private static byte[] TryGetMetadata(SafeHeifImageHandle primaryImageHandle, MetadataType metadataType)
        {
            byte[] data = null;

            ulong size = HeicNative.GetMetadataSize(primaryImageHandle, metadataType);

            if (size > 0 && size <= int.MaxValue)
            {
                data = new byte[size];

                HeicNative.GetMetadata(primaryImageHandle, metadataType, data);
            }

            return data;
        }

        private static ExifValueCollection TryParseExifData(byte[] exifData)
        {
            if (exifData is null)
            {
                return null;
            }

            ExifValueCollection metadataEntries = null;

            // The EXIF data block has a header that indicates the number of bytes
            // that come before the start of the TIFF header.
            // See ISO/IEC 23008-12:2017 section A.2.1.

            if (TryReadInt32BigEndian(exifData, 0, out int tiffStartOffset))
            {
                int startIndex = 4;
                if (tiffStartOffset > 0)
                {
                    startIndex += tiffStartOffset;
                }

                int dataLength = exifData.Length - startIndex;

                if (dataLength > 0)
                {
                    using (MemoryStream stream = new MemoryStream(exifData, startIndex, dataLength))
                    {
                        metadataEntries = ExifParser.Parse(stream);
                    }
                }
            }

            return metadataEntries;

            bool TryReadInt32BigEndian(byte[] bytes, int startOffset, out int value)
            {
                value = 0;

                if (bytes is null)
                {
                    return false;
                }

                if ((startOffset + sizeof(int)) > bytes.Length)
                {
                    return false;
                }

                value = (bytes[startOffset + 0] << 24) | (bytes[startOffset + 1] << 16) | (bytes[startOffset + 2] << 8) | bytes[startOffset + 3];
                return true;
            }
        }
    }
}
