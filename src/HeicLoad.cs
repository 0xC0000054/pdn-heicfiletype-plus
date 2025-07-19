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
using System;
using System.Collections.Generic;
using System.IO;

namespace HeicFileTypePlus
{
    internal static class HeicLoad
    {
        public static Document Load(Stream input)
        {
            Document doc = null;

            long originalStreamPosition = input.Position;


            try
            {
                using (HeifFileIO fileIO = new(input, leaveOpen: true))
                using (SafeHeifContext context = HeicNative.CreateContext())
                {
                    HeicNative.LoadFileIntoContext(context, fileIO);

                    HeifImageHandle primaryImageHandle = null;
                    Surface surface = null;
                    bool disposeSurface = true;

                    try
                    {
                        primaryImageHandle = HeicNative.GetPrimaryImage(context);

                        surface = new Surface(primaryImageHandle.Width, primaryImageHandle.Height);

                        HeicNative.DecodeImage(primaryImageHandle.SafeHeifImageHandle, surface);

                        doc = new Document(surface.Width, surface.Height);
                        AddMetadataToDocument(doc, primaryImageHandle);
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
            }
            catch (NoFtypeBoxException ex)
            {
                input.Position = originalStreamPosition;

                if (FormatDetection.IsCommonImageFormat(input))
                {
                    input.Position = originalStreamPosition;

                    using (System.Drawing.Image image = System.Drawing.Image.FromStream(input))
                    {
                        doc = Document.FromGdipImage(image);
                    }
                }
                else
                {
                    throw new FormatException(ex.Message);
                }
            }

            return doc;
        }


        private static void AddMetadataToDocument(Document document, HeifImageHandle primaryImageHandle)
        {
            byte[] exifData = primaryImageHandle.GetExif();

            if (exifData != null)
            {
                ExifValueCollection metadataEntries = TryParseExifData(exifData);

                if (metadataEntries != null)
                {
                    metadataEntries.Remove(ExifPropertyKeys.Image.InterColorProfile.Path);
                    // The HEIF specification states that the EXIF orientation tag is only
                    // informational and should not be used to rotate the image.
                    // See https://github.com/strukturag/libheif/issues/227#issuecomment-642165942
                    metadataEntries.Remove(ExifPropertyKeys.Image.Orientation.Path);

                    foreach (KeyValuePair<ExifPropertyPath, ExifValue> item in metadataEntries)
                    {
                        ExifPropertyPath path = item.Key;

                        document.Metadata.AddExifPropertyItem(path.Section, path.TagID, item.Value);
                    }
                }
            }

            byte[] iccProfile = primaryImageHandle.GetIccProfile();

            if (iccProfile != null)
            {
                document.Metadata.AddExifPropertyItem(ExifSection.Image,
                                                      ExifPropertyKeys.Image.InterColorProfile.Path.TagID,
                                                      new ExifValue(ExifValueType.Undefined, iccProfile));
            }

            string serializedCICPData = CICPSerializer.TrySerialize(primaryImageHandle.CICPColorData);

            if (serializedCICPData != null)
            {
                document.Metadata.SetUserValue(HeicMetadataNames.CICPMetadataName, serializedCICPData);
            }

            byte[] xmpData = primaryImageHandle.GetXmp();

            if (xmpData != null)
            {
                XmpPacket packet = XmpPacket.TryParse(xmpData);

                if (packet != null)
                {
                    document.Metadata.SetXmpPacket(packet);
                }
            }
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
                    using (MemoryStream stream = new(exifData, startIndex, dataLength))
                    {
                        metadataEntries = ExifParser.Parse(stream);
                    }
                }
            }

            return metadataEntries;

            static bool TryReadInt32BigEndian(byte[] bytes, int startOffset, out int value)
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
