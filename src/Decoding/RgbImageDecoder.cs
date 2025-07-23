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
    internal static class RgbImageDecoder
    {
        public static void SetImageData(
            IImagingFactory factory,
            HeifImage heifImage,
            Surface output)
        {
            int bitDepth = heifImage.BitDepth;
            bool hasAlpha = heifImage.HasAlphaChannel;
            bool isAlphaPremultiplied = heifImage.IsAlphaChannelPremultiplied;
            bool isPlanar = heifImage.Chroma == HeifChroma.Yuv444;

            if (bitDepth == 8)
            {
                if (hasAlpha)
                {
                    if (isPlanar)
                    {
                        SetImageDataPlanarRgba32(heifImage, output);
                    }
                    else
                    {
                        SetImageDataInterleavedRgba32(heifImage, output);
                    }

                    if (isAlphaPremultiplied)
                    {
                        output.ConvertFromPremultipliedAlpha();
                    }
                }
                else
                {
                    if (isPlanar)
                    {
                        SetImageDataPlanarRgb24(heifImage, output);
                    }
                    else
                    {
                        SetImageDataInterleavedRgb24(heifImage, output);
                    }
                }
            }
            else
            {
                if (bitDepth != 10 && bitDepth != 12 && bitDepth != 16)
                {
                    throw new FormatException($"Unsupported HEIF image bit depth: {bitDepth}.");
                }

                HDRFormat hdrFormat = heifImage.HDRFormat;

                if (hdrFormat != HDRFormat.None)
                {
                    if (hasAlpha)
                    {
                        using (IBitmap<ColorRgba128Float> temp = factory.CreateBitmap<ColorRgba128Float>(heifImage.Width, heifImage.Height))
                        {
                            if (isPlanar)
                            {
                                SetImageDataPlanarRgba128(heifImage, temp);
                            }
                            else
                            {
                                SetImageDataInterleavedRgba128(heifImage, temp);
                            }

                            if (isAlphaPremultiplied)
                            {
                                // Cast the image to ColorPrgba128Float to make Direct2D/WIC handle the conversion to straight alpha.
                                using (IBitmap<ColorPrgba128Float> asPrgba = temp.Cast<ColorPrgba128Float>())
                                {
                                    HighBitDepthConversion.HdrImageToBgra32(factory, asPrgba, hdrFormat, output);
                                }
                            }
                            else
                            {
                                HighBitDepthConversion.HdrImageToBgra32(factory, temp, hdrFormat, output);
                            }
                        }
                    }
                    else
                    {
                        using (IBitmap<ColorRgb96Float> temp = factory.CreateBitmap<ColorRgb96Float>(heifImage.Width, heifImage.Height))
                        {
                            if (isPlanar)
                            {
                                SetImageDataPlanarRgb96(heifImage, temp);
                            }
                            else
                            {
                                SetImageDataInterleavedRgb96(heifImage, temp);
                            }

                            HighBitDepthConversion.HdrImageToBgra32(factory, temp, hdrFormat, output);
                        }
                    }
                }
                else
                {
                    if (hasAlpha)
                    {
                        using (IBitmap<ColorRgba64> temp = factory.CreateBitmap<ColorRgba64>(heifImage.Width, heifImage.Height))
                        {
                            if (isPlanar)
                            {
                                SetImageDataPlanarRgba64(heifImage, temp);
                            }
                            else
                            {
                                SetImageDataInterleavedRgba64(heifImage, temp);
                            }

                            if (isAlphaPremultiplied)
                            {
                                // Cast the image to ColorPrgba64 to make WIC handle the conversion to straight alpha.
                                using (IBitmap<ColorPrgba64> asPrgba64 = temp.Cast<ColorPrgba64>())
                                {
                                    HighBitDepthConversion.SdrImageToBgra32(factory, temp, output);
                                }
                            }
                            else
                            {
                                HighBitDepthConversion.SdrImageToBgra32(factory, temp, output);
                            }
                        }
                    }
                    else
                    {
                        using (IBitmap<ColorRgb48> temp = factory.CreateBitmap<ColorRgb48>(heifImage.Width, heifImage.Height))
                        {
                            if (isPlanar)
                            {
                                SetImageDataPlanarRgb48(heifImage, temp);
                            }
                            else
                            {
                                SetImageDataInterleavedRgb48(heifImage, temp);
                            }

                            HighBitDepthConversion.SdrImageToBgra32(factory, temp, output);
                        }
                    }
                }
            }
        }

        private static unsafe void SetImageDataInterleavedRgb24(HeifImage image, Surface output)
        {
            HeifImageChannel interleavedChannel = image.GetChannel(HeifChannel.Interleaved);

            int width = image.Width;
            int height = image.Height;

            RegionPtr<ColorRgb24> srcRegion = new((ColorRgb24*)interleavedChannel.scan0,
                                                   width,
                                                   height,
                                                   interleavedChannel.stride);
            RegionPtr<ColorBgra32> destRegion = output.AsRegionPtr().Cast<ColorBgra32>();

            for (int y = 0; y < height; y++)
            {
                ColorRgb24* src = srcRegion.Rows[y].Ptr;
                ColorBgra32* dst = destRegion.Rows[y].Ptr;

                for (int x = 0; x < width; x++)
                {
                    dst->R = src->R;
                    dst->G = src->G;
                    dst->B = src->B;
                    dst->A = 255;

                    src++;
                    dst++;
                }
            }
        }

        private static unsafe void SetImageDataInterleavedRgba32(HeifImage image, Surface output)
        {
            HeifImageChannel interleavedChannel = image.GetChannel(HeifChannel.Interleaved);

            int width = image.Width;
            int height = image.Height;

            RegionPtr<ColorRgba32> srcRegion = new((ColorRgba32*)interleavedChannel.scan0,
                                                   width,
                                                   height,
                                                   interleavedChannel.stride);
            RegionPtr<ColorBgra32> destRegion = output.AsRegionPtr().Cast<ColorBgra32>();


            PixelKernels.ConvertRgba32ToBgra32(destRegion, srcRegion);
        }

        private static unsafe void SetImageDataPlanarRgb24(HeifImage image, Surface output)
        {
            HeifImageChannel redChannel = image.GetChannel(HeifChannel.R);
            HeifImageChannel greenChannel = image.GetChannel(HeifChannel.G);
            HeifImageChannel blueChannel = image.GetChannel(HeifChannel.B);

            byte* redScan0 = redChannel.scan0;
            nint redStride = redChannel.stride;

            byte* greenScan0 = greenChannel.scan0;
            nint greenStride = greenChannel.stride;

            byte* blueScan0 = blueChannel.scan0;
            nint blueStride = blueChannel.stride;

            int width = image.Width;
            int height = image.Height;

            RegionPtr<ColorBgra32> destRegion = output.AsRegionPtr().Cast<ColorBgra32>();

            for (int y = 0; y < height; y++)
            {
                byte* redSrc = redScan0 + (y * redStride);
                byte* greenSrc = greenScan0 + (y * greenStride);
                byte* blueSrc = blueScan0 + (y * blueStride);
                ColorBgra32* dst = destRegion.Rows[y].Ptr;

                for (int x = 0; x < width; x++)
                {
                    dst->R = *redSrc;
                    dst->G = *greenSrc;
                    dst->B = *blueSrc;
                    dst->A = 255;

                    redSrc++;
                    greenSrc++;
                    blueSrc++;
                    dst++;
                }
            }
        }

        private static unsafe void SetImageDataPlanarRgba32(HeifImage image, Surface output)
        {
            HeifImageChannel redChannel = image.GetChannel(HeifChannel.R);
            HeifImageChannel greenChannel = image.GetChannel(HeifChannel.G);
            HeifImageChannel blueChannel = image.GetChannel(HeifChannel.B);
            HeifImageChannel alphaChannel = image.GetChannel(HeifChannel.Alpha);

            byte* redScan0 = redChannel.scan0;
            nint redStride = redChannel.stride;

            byte* greenScan0 = greenChannel.scan0;
            nint greenStride = greenChannel.stride;

            byte* blueScan0 = blueChannel.scan0;
            nint blueStride = blueChannel.stride;

            int width = image.Width;
            int height = image.Height;

            RegionPtr<ColorBgra32> destRegion = output.AsRegionPtr().Cast<ColorBgra32>();

            for (int y = 0; y < height; y++)
            {
                byte* redSrc = redScan0 + (y * redStride);
                byte* greenSrc = greenScan0 + (y * greenStride);
                byte* blueSrc = blueScan0 + (y * blueStride);
                ColorBgra32* dst = destRegion.Rows[y].Ptr;

                for (int x = 0; x < width; x++)
                {
                    dst->R = *redSrc;
                    dst->G = *greenSrc;
                    dst->B = *blueSrc;
                    dst->A = 255;

                    redSrc++;
                    greenSrc++;
                    blueSrc++;
                    dst++;
                }
            }
        }

        private static unsafe void SetImageDataInterleavedRgb48(HeifImage image, IBitmap<ColorRgb48> output)
        {
            HighBitDepthExpansionSDR highBitDepthExpansion = new(image.BitDepth);

            HeifImageChannel interleavedChannel = image.GetChannel(HeifChannel.Interleaved);

            int width = image.Width;
            int height = image.Height;

            RegionPtr<ColorRgb48> srcRegion = new((ColorRgb48*)interleavedChannel.scan0,
                                                   width,
                                                   height,
                                                   interleavedChannel.stride);

            using (IBitmapLock<ColorRgb48> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgb48> destRegion = bitmapLock.AsRegionPtr();

                for (int y = 0; y < height; y++)
                {
                    ColorRgb48* src = srcRegion.Rows[y].Ptr;
                    ColorRgb48* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = highBitDepthExpansion.GetExpandedValue(src->R);
                        dst->G = highBitDepthExpansion.GetExpandedValue(src->G);
                        dst->B = highBitDepthExpansion.GetExpandedValue(src->B);

                        src++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataInterleavedRgba64(HeifImage image, IBitmap<ColorRgba64> output)
        {
            HighBitDepthExpansionSDR highBitDepthExpansion = new(image.BitDepth);

            HeifImageChannel interleavedChannel = image.GetChannel(HeifChannel.Interleaved);

            int width = image.Width;
            int height = image.Height;

            RegionPtr<ColorRgba64> srcRegion = new((ColorRgba64*)interleavedChannel.scan0,
                                                   width,
                                                   height,
                                                   interleavedChannel.stride);

            using (IBitmapLock<ColorRgba64> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgba64> destRegion = bitmapLock.AsRegionPtr();

                for (int y = 0; y < height; y++)
                {
                    ColorRgba64* src = srcRegion.Rows[y].Ptr;
                    ColorRgba64* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = highBitDepthExpansion.GetExpandedValue(src->R);
                        dst->G = highBitDepthExpansion.GetExpandedValue(src->G);
                        dst->B = highBitDepthExpansion.GetExpandedValue(src->B);
                        dst->A = highBitDepthExpansion.GetExpandedValue(src->A);

                        src++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataPlanarRgb48(HeifImage image, IBitmap<ColorRgb48> output)
        {
            HighBitDepthExpansionSDR highBitDepthExpansion = new(image.BitDepth);

            HeifImageChannel redChannel = image.GetChannel(HeifChannel.R);
            HeifImageChannel greenChannel = image.GetChannel(HeifChannel.G);
            HeifImageChannel blueChannel = image.GetChannel(HeifChannel.B);

            byte* redScan0 = redChannel.scan0;
            nint redStride = redChannel.stride;

            byte* greenScan0 = greenChannel.scan0;
            nint greenStride = greenChannel.stride;

            byte* blueScan0 = blueChannel.scan0;
            nint blueStride = blueChannel.stride;

            int width = image.Width;
            int height = image.Height;

            using (IBitmapLock<ColorRgb48> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgb48> destRegion = bitmapLock.AsRegionPtr();

                for (int y = 0; y < height; y++)
                {
                    ushort* redSrc = (ushort*)(redScan0 + (y * redStride));
                    ushort* greenSrc = (ushort*)(greenScan0 + (y * greenStride));
                    ushort* blueSrc = (ushort*)(blueScan0 + (y * blueStride));
                    ColorRgb48* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = highBitDepthExpansion.GetExpandedValue(*redSrc);
                        dst->G = highBitDepthExpansion.GetExpandedValue(*greenSrc);
                        dst->B = highBitDepthExpansion.GetExpandedValue(*blueSrc);

                        redSrc++;
                        greenSrc++;
                        blueSrc++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataPlanarRgba64(HeifImage image, IBitmap<ColorRgba64> output)
        {
            HighBitDepthExpansionSDR highBitDepthExpansion = new(image.BitDepth);

            HeifImageChannel redChannel = image.GetChannel(HeifChannel.R);
            HeifImageChannel greenChannel = image.GetChannel(HeifChannel.G);
            HeifImageChannel blueChannel = image.GetChannel(HeifChannel.B);
            HeifImageChannel alphaChannel = image.GetChannel(HeifChannel.Alpha);

            byte* redScan0 = redChannel.scan0;
            nint redStride = redChannel.stride;

            byte* greenScan0 = greenChannel.scan0;
            nint greenStride = greenChannel.stride;

            byte* blueScan0 = blueChannel.scan0;
            nint blueStride = blueChannel.stride;

            byte* alphaScan0 = alphaChannel.scan0;
            nint alphaStride = alphaChannel.stride;

            int width = image.Width;
            int height = image.Height;

            using (IBitmapLock<ColorRgba64> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgba64> destRegion = bitmapLock.AsRegionPtr();

                for (int y = 0; y < height; y++)
                {
                    ushort* redSrc = (ushort*)(redScan0 + (y * redStride));
                    ushort* greenSrc = (ushort*)(greenScan0 + (y * greenStride));
                    ushort* blueSrc = (ushort*)(blueScan0 + (y * blueStride));
                    ushort* alphaSrc = (ushort*)(alphaScan0 + (y * alphaStride));
                    ColorRgba64* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = highBitDepthExpansion.GetExpandedValue(*redSrc);
                        dst->G = highBitDepthExpansion.GetExpandedValue(*greenSrc);
                        dst->B = highBitDepthExpansion.GetExpandedValue(*blueSrc);
                        dst->A = highBitDepthExpansion.GetExpandedValue(*alphaSrc);

                        redSrc++;
                        greenSrc++;
                        blueSrc++;
                        alphaSrc++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataInterleavedRgb96(HeifImage image, IBitmap<ColorRgb96Float> output)
        {
            HighBitDepthExpansionHDR highBitDepthExpansion = new(image.BitDepth);

            HeifImageChannel interleavedChannel = image.GetChannel(HeifChannel.Interleaved);

            int width = image.Width;
            int height = image.Height;

            RegionPtr<ColorRgb48> srcRegion = new((ColorRgb48*)interleavedChannel.scan0,
                                                   width,
                                                   height,
                                                   interleavedChannel.stride);

            using (IBitmapLock<ColorRgb96Float> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgb96Float> destRegion = bitmapLock.AsRegionPtr();

                for (int y = 0; y < height; y++)
                {
                    ColorRgb48* src = srcRegion.Rows[y].Ptr;
                    ColorRgb96Float* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = highBitDepthExpansion.GetExpandedValue(src->R);
                        dst->G = highBitDepthExpansion.GetExpandedValue(src->G);
                        dst->B = highBitDepthExpansion.GetExpandedValue(src->B);

                        src++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataInterleavedRgba128(HeifImage image, IBitmap<ColorRgba128Float> output)
        {
            HighBitDepthExpansionHDR highBitDepthExpansion = new(image.BitDepth);

            HeifImageChannel interleavedChannel = image.GetChannel(HeifChannel.Interleaved);

            int width = image.Width;
            int height = image.Height;

            RegionPtr<ColorRgba64> srcRegion = new((ColorRgba64*)interleavedChannel.scan0,
                                                   width,
                                                   height,
                                                   interleavedChannel.stride);

            using (IBitmapLock<ColorRgba128Float> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgba128Float> destRegion = bitmapLock.AsRegionPtr();

                for (int y = 0; y < height; y++)
                {
                    ColorRgba64* src = srcRegion.Rows[y].Ptr;
                    ColorRgba128Float* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = highBitDepthExpansion.GetExpandedValue(src->R);
                        dst->G = highBitDepthExpansion.GetExpandedValue(src->G);
                        dst->B = highBitDepthExpansion.GetExpandedValue(src->B);
                        dst->A = highBitDepthExpansion.GetExpandedValue(src->A);

                        src++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataPlanarRgb96(HeifImage image, IBitmap<ColorRgb96Float> output)
        {
            HighBitDepthExpansionHDR highBitDepthExpansion = new(image.BitDepth);

            HeifImageChannel redChannel = image.GetChannel(HeifChannel.R);
            HeifImageChannel greenChannel = image.GetChannel(HeifChannel.G);
            HeifImageChannel blueChannel = image.GetChannel(HeifChannel.B);

            byte* redScan0 = redChannel.scan0;
            nint redStride = redChannel.stride;

            byte* greenScan0 = greenChannel.scan0;
            nint greenStride = greenChannel.stride;

            byte* blueScan0 = blueChannel.scan0;
            nint blueStride = blueChannel.stride;

            int width = image.Width;
            int height = image.Height;

            using (IBitmapLock<ColorRgb96Float> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgb96Float> destRegion = bitmapLock.AsRegionPtr();

                for (int y = 0; y < height; y++)
                {
                    ushort* redSrc = (ushort*)(redScan0 + (y * redStride));
                    ushort* greenSrc = (ushort*)(greenScan0 + (y * greenStride));
                    ushort* blueSrc = (ushort*)(blueScan0 + (y * blueStride));
                    ColorRgb96Float* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = highBitDepthExpansion.GetExpandedValue(*redSrc);
                        dst->G = highBitDepthExpansion.GetExpandedValue(*greenSrc);
                        dst->B = highBitDepthExpansion.GetExpandedValue(*blueSrc);

                        redSrc++;
                        greenSrc++;
                        blueSrc++;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void SetImageDataPlanarRgba128(HeifImage image, IBitmap<ColorRgba128Float> output)
        {
            HighBitDepthExpansionHDR highBitDepthExpansion = new(image.BitDepth);

            HeifImageChannel redChannel = image.GetChannel(HeifChannel.R);
            HeifImageChannel greenChannel = image.GetChannel(HeifChannel.G);
            HeifImageChannel blueChannel = image.GetChannel(HeifChannel.B);
            HeifImageChannel alphaChannel = image.GetChannel(HeifChannel.Alpha);

            byte* redScan0 = redChannel.scan0;
            nint redStride = redChannel.stride;

            byte* greenScan0 = greenChannel.scan0;
            nint greenStride = greenChannel.stride;

            byte* blueScan0 = blueChannel.scan0;
            nint blueStride = blueChannel.stride;

            byte* alphaScan0 = alphaChannel.scan0;
            nint alphaStride = alphaChannel.stride;

            int width = image.Width;
            int height = image.Height;

            using (IBitmapLock<ColorRgba128Float> bitmapLock = output.Lock(BitmapLockOptions.ReadWrite))
            {
                RegionPtr<ColorRgba128Float> destRegion = bitmapLock.AsRegionPtr();

                for (int y = 0; y < height; y++)
                {
                    ushort* redSrc = (ushort*)(redScan0 + (y * redStride));
                    ushort* greenSrc = (ushort*)(greenScan0 + (y * greenStride));
                    ushort* blueSrc = (ushort*)(blueScan0 + (y * blueStride));
                    ushort* alphaSrc = (ushort*)(alphaScan0 + (y * alphaStride));
                    ColorRgba128Float* dst = destRegion.Rows[y].Ptr;

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = highBitDepthExpansion.GetExpandedValue(*redSrc);
                        dst->G = highBitDepthExpansion.GetExpandedValue(*greenSrc);
                        dst->B = highBitDepthExpansion.GetExpandedValue(*blueSrc);
                        dst->A = highBitDepthExpansion.GetExpandedValue(*alphaSrc);

                        redSrc++;
                        greenSrc++;
                        blueSrc++;
                        alphaSrc++;
                        dst++;
                    }
                }
            }
        }
    }
}
