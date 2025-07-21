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

using HeicFileTypePlus.Decoding;
using HeicFileTypePlus.Exif;
using HeicFileTypePlus.ICCProfile;
using HeicFileTypePlus.Interop;
using PaintDotNet;
using PaintDotNet.Imaging;
using System;
using System.Buffers.Binary;
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
                using (IImagingFactory imagingFactory = ImagingFactory.CreateRef())
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

                        using (HeifImage image = primaryImageHandle.Decode(HeifColorSpace.Undefined, HeifChroma.Undefined))
                        {
                            switch (image.ColorSpace)
                            {
                                case HeifColorSpace.YCbCr:
                                    YCbCrImageDecoder.SetImageData(imagingFactory, primaryImageHandle, surface);
                                    break;
                                case HeifColorSpace.Rgb:
                                    RgbImageDecoder.SetImageData(imagingFactory, image, surface);
                                    break;
                                case HeifColorSpace.Monochrome:
                                    MonochromeImageDecoder.SetImageData(imagingFactory, image, surface);
                                    break;
                                case HeifColorSpace.Undefined:
                                default:
                                    throw new FormatException("Unknown HEIF image color space.");
                            }
                        }

                        doc = new Document(surface.Width, surface.Height);
                        AddMetadataToDocument(doc, primaryImageHandle, imagingFactory);
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


        private static void AddMetadataToDocument(
            Document document,
            HeifImageHandle primaryImageHandle,
            IImagingFactory imagingFactory)
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

            if (primaryImageHandle.HDRFormat != HDRFormat.None)
            {
                using (IColorContext context = imagingFactory.CreateColorContext(KnownColorSpace.DisplayP3))
                {
                    document.SetColorContext(context);
                }
            }
            else
            {
                ImageHandleColorProfileType profileType = primaryImageHandle.ColorProfileType;

                if (profileType == ImageHandleColorProfileType.Icc)
                {
                    byte[] iccProfile = primaryImageHandle.GetIccProfile();

                    if (iccProfile != null)
                    {
                        IColorContext colorContext = ColorContextUtil.TryCreateFromRgbProfile(iccProfile, imagingFactory);

                        if (colorContext != null)
                        {
                            try
                            {
                                document.SetColorContext(colorContext);
                            }
                            finally
                            {
                                colorContext.Dispose();
                            }
                        }
                    }
                }
                else if (profileType == ImageHandleColorProfileType.Cicp)
                {
                    CICPColorData colorData = primaryImageHandle.CICPColorData;

                    IColorContext colorContext = ColorContextUtil.TryCreateFromCICP(colorData, imagingFactory);

                    if (colorContext != null)
                    {
                        try
                        {
                            document.SetColorContext(colorContext);
                        }
                        finally
                        {
                            colorContext.Dispose();
                        }
                    }
                }
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

            if (TryReadInt32BigEndian(exifData, out int tiffStartOffset))
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

            static bool TryReadInt32BigEndian(ReadOnlySpan<byte> bytes, out int value)
            {
                value = 0;

                if (bytes.Length <= sizeof(int))
                {
                    return false;
                }

                value = BinaryPrimitives.ReadInt32BigEndian(bytes);
                return true;
            }
        }
    }
}
