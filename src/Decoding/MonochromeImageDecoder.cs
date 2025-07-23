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

using HeicFileTypePlus.Interop;
using PaintDotNet;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;

namespace HeicFileTypePlus.Decoding
{
    internal static class MonochromeImageDecoder
    {
        public static void SetImageData(
            IImagingFactory factory,
            HeifImage heifImage,
            Surface output)
        {
            int bitDepth = heifImage.BitDepth;
            bool hasAlpha = heifImage.HasAlphaChannel;
            bool isAlphaPremultiplied = heifImage.IsAlphaChannelPremultiplied;

            if (bitDepth == 8)
            {
                if (hasAlpha)
                {
                    SetImageDataGrayAlpha8(heifImage, output);

                    if (isAlphaPremultiplied)
                    {
                        output.ConvertFromPremultipliedAlpha();
                    }
                }
                else
                {
                    SetImageDataGray8(heifImage, output);
                }
            }
            else
            {
                if (bitDepth != 10 && bitDepth != 12 && bitDepth != 16)
                {
                    throw new FormatException($"Unsupported HEIF image bit depth: {bitDepth}.");
                }

                HDRFormat hdrFormat = heifImage.HDRFormat;

                // PQ is the only HEIF HDR format that supports monochrome images.
                if (hdrFormat == HDRFormat.PQ)
                {
                    if (hasAlpha)
                    {
                        using (IBitmap<ColorRgba128Float> tempImage = factory.CreateBitmap<ColorRgba128Float>(heifImage.Width, heifImage.Height))
                        {
                            SetImageDataGrayAlpha32(heifImage, tempImage);

                            if (isAlphaPremultiplied)
                            {
                                // Cast the image to ColorPrgba128Float to make Direct2D/WIC handle the conversion to straight alpha.
                                using (IBitmap<ColorPrgba128Float> asPrgba = tempImage.Cast<ColorPrgba128Float>())
                                {
                                    HighBitDepthConversion.HdrImageToBgra32(factory, asPrgba, hdrFormat, output);
                                }
                            }
                            else
                            {
                                HighBitDepthConversion.HdrImageToBgra32(factory, tempImage, hdrFormat, output);
                            }
                        }
                    }
                    else
                    {
                        using (IBitmap<ColorRgb96Float> tempImage = factory.CreateBitmap<ColorRgb96Float>(heifImage.Width, heifImage.Height))
                        {
                            SetImageDataGray32(heifImage, tempImage);
                            HighBitDepthConversion.HdrImageToBgra32(factory, tempImage, hdrFormat, output);
                        }
                    }
                }
                else
                {
                    if (hasAlpha)
                    {
                        using (IBitmap<ColorRgba64> tempImage = factory.CreateBitmap<ColorRgba64>(heifImage.Width, heifImage.Height))
                        {
                            SetImageDataGrayAlpha16(heifImage, tempImage);

                            if (isAlphaPremultiplied)
                            {
                                // Cast the image to ColorPrgba64 to make WIC handle the conversion to straight alpha.
                                using (IBitmap<ColorPrgba64> asPrgba = tempImage.Cast<ColorPrgba64>())
                                {
                                    HighBitDepthConversion.SdrImageToBgra32(factory, tempImage, output);
                                }
                            }
                            else
                            {
                                HighBitDepthConversion.SdrImageToBgra32(factory, tempImage, output);
                            }
                        }
                    }
                    else
                    {
                        using (IBitmap<ColorRgb48> tempImage = factory.CreateBitmap<ColorRgb48>(heifImage.Width, heifImage.Height))
                        {
                            SetImageDataGray16(heifImage, tempImage);
                            HighBitDepthConversion.SdrImageToBgra32(factory, tempImage, output);
                        }
                    }
                }
            }
        }

        private static unsafe void SetImageDataGray8(HeifImage heifImage, Surface output)
        {
            HeifImageChannel grayChannel = heifImage.GetChannel(HeifChannel.Y);

            byte* grayScan0 = grayChannel.scan0;
            nint grayStride = grayChannel.stride;

            RegionPtr<ColorBgra32> destRegion = output.AsRegionPtr().Cast<ColorBgra32>();

            int width = heifImage.Width;
            int height = heifImage.Height;

            for (int y = 0; y < height; y++)
            {
                byte* src = grayScan0 + (y * grayStride);
                ColorBgra32* dst = destRegion.Rows[y].Ptr;

                for (int x = 0; x < width; x++)
                {
                    byte gray = *src;

                    dst->B = gray;
                    dst->G = gray;
                    dst->R = gray;
                    dst->A = 255;

                    src++;
                    dst++;
                }
            }
        }

        private static unsafe void SetImageDataGrayAlpha8(HeifImage heifImage, Surface output)
        {
            HeifImageChannel grayChannel = heifImage.GetChannel(HeifChannel.Y);
            HeifImageChannel alphaChannel = heifImage.GetChannel(HeifChannel.Alpha);

            byte* grayScan0 = grayChannel.scan0;
            nint grayStride = grayChannel.stride;

            byte* alphaScan0 = alphaChannel.scan0;
            nint alphaStride = alphaChannel.stride;

            RegionPtr<ColorBgra32> destRegion = output.AsRegionPtr().Cast<ColorBgra32>();

            int width = heifImage.Width;
            int height = heifImage.Height;

            for (int y = 0; y < height; y++)
            {
                byte* graySrc = grayScan0 + (y * grayStride);
                byte* alphaSrc = alphaScan0 + (y * alphaStride);

                ColorBgra32* dst = destRegion.Rows[y].Ptr;

                for (int x = 0; x < width; x++)
                {
                    byte gray = *graySrc;

                    dst->B = gray;
                    dst->G = gray;
                    dst->R = gray;
                    dst->A = *alphaSrc;

                    graySrc++;
                    alphaSrc++;
                    dst++;
                }
            }
        }

        private static unsafe void SetImageDataGray16(HeifImage heifImage, IBitmap<ColorRgb48> output)
        {
            HighBitDepthExpansionSDR bitDepthExpansion = new(heifImage.BitDepth);

            HeifImageChannel grayChannel = heifImage.GetChannel(HeifChannel.Y);

            byte* grayScan0 = grayChannel.scan0;
            nint grayStride = grayChannel.stride;

            using (IBitmapLock<ColorRgb48> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgb48> destRegion = bitmapLock.AsRegionPtr();

                int width = heifImage.Width;
                int height = heifImage.Height;

                for (int y = 0; y < height; y++)
                {
                    ushort* src = (ushort*)(grayScan0 + (y * grayStride));
                    ColorRgb48* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        ushort gray = bitDepthExpansion.GetExpandedValue(*src);

                        dst->R = gray;
                        dst->G = gray;
                        dst->B = gray;

                        src++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataGrayAlpha16(HeifImage heifImage, IBitmap<ColorRgba64> output)
        {
            HighBitDepthExpansionSDR bitDepthExpansion = new(heifImage.BitDepth);

            HeifImageChannel grayChannel = heifImage.GetChannel(HeifChannel.Y);
            HeifImageChannel alphaChannel = heifImage.GetChannel(HeifChannel.Alpha);

            byte* grayScan0 = grayChannel.scan0;
            nint grayStride = grayChannel.stride;

            byte* alphaScan0 = alphaChannel.scan0;
            nint alphaStride = alphaChannel.stride;

            using (IBitmapLock<ColorRgba64> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgba64> destRegion = bitmapLock.AsRegionPtr();

                int width = heifImage.Width;
                int height = heifImage.Height;

                for (int y = 0; y < height; y++)
                {
                    ushort* graySrc = (ushort*)(grayScan0 + (y * grayStride));
                    ushort* alphaSrc = (ushort*)(alphaScan0 + (y * alphaStride));

                    ColorRgba64* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        ushort gray = bitDepthExpansion.GetExpandedValue(*graySrc);
                        ushort alpha = bitDepthExpansion.GetExpandedValue(*alphaSrc);

                        dst->R = gray;
                        dst->G = gray;
                        dst->B = gray;
                        dst->A = alpha;

                        graySrc++;
                        alphaSrc++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataGray32(HeifImage heifImage, IBitmap<ColorRgb96Float> output)
        {
            HighBitDepthExpansionHDR bitDepthExpansion = new(heifImage.BitDepth);

            HeifImageChannel grayChannel = heifImage.GetChannel(HeifChannel.Y);

            byte* grayScan0 = grayChannel.scan0;
            nint grayStride = grayChannel.stride;

            using (IBitmapLock<ColorRgb96Float> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgb96Float> destRegion = bitmapLock.AsRegionPtr();

                int width = heifImage.Width;
                int height = heifImage.Height;

                for (int y = 0; y < height; y++)
                {
                    ushort* src = (ushort*)(grayScan0 + (y * grayStride));
                    ColorRgb96Float* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        float gray = bitDepthExpansion.GetExpandedValue(*src);

                        dst->B = gray;
                        dst->G = gray;
                        dst->R = gray;

                        src++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataGrayAlpha32(HeifImage heifImage, IBitmap<ColorRgba128Float> output)
        {
            HighBitDepthExpansionHDR bitDepthExpansion = new(heifImage.BitDepth);

            HeifImageChannel grayChannel = heifImage.GetChannel(HeifChannel.Y);
            HeifImageChannel alphaChannel = heifImage.GetChannel(HeifChannel.Alpha);

            byte* grayScan0 = grayChannel.scan0;
            nint grayStride = grayChannel.stride;

            byte* alphaScan0 = alphaChannel.scan0;
            nint alphaStride = alphaChannel.stride;

            using (IBitmapLock<ColorRgba128Float> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgba128Float> destRegion = bitmapLock.AsRegionPtr();

                int width = heifImage.Width;
                int height = heifImage.Height;

                for (int y = 0; y < height; y++)
                {
                    ushort* graySrc = (ushort*)(grayScan0 + (y * grayStride));
                    ushort* alphaSrc = (ushort*)(alphaScan0 + (y * alphaStride));

                    ColorRgba128Float* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        float gray = bitDepthExpansion.GetExpandedValue(*graySrc);
                        float alpha = bitDepthExpansion.GetExpandedValue(*alphaSrc);

                        dst->B = gray;
                        dst->G = gray;
                        dst->R = gray;
                        dst->A = alpha;

                        graySrc++;
                        alphaSrc++;
                        dst++;
                    }
                }
            }
        }
    }
}
