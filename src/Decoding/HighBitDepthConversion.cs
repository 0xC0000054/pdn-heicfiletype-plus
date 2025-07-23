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
using PaintDotNet.Direct2D1;
using PaintDotNet.Direct2D1.Effects;
using PaintDotNet.Dxgi;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;

namespace HeicFileTypePlus.Decoding
{
    internal static class HighBitDepthConversion
    {
        public static unsafe void HdrImageToBgra32(IImagingFactory imagingFactory,
                                                   IBitmap input,
                                                   HDRFormat hdrFormat,
                                                   Surface output)
        {
            if (hdrFormat == HDRFormat.PQ)
            {
                ConvertHdrPQImageToBgra32(imagingFactory, input, output);
            }
            else
            {
                // Unsupported HDR format, load the image as SDR.
                SdrImageToBgra32(imagingFactory, input, output);
            }
        }

        public static unsafe void SdrImageToBgra32(IImagingFactory imagingFactory,
                                                   IBitmap input,
                                                   Surface output)
        {
            using (IBitmapSource<ColorBgra32> convertedImage = imagingFactory.CreateFormatConvertedBitmap<ColorBgra32>(input))
            {
                convertedImage.CopyPixels(output.AsRegionPtr().Cast<ColorBgra32>());
            }
        }

        private static void ConvertHdrPQImageToBgra32(IImagingFactory imagingFactory,
                                                      IBitmap input,
                                                      Surface output)
        {

            using (IColorContext dp3ColorContext = imagingFactory.CreateColorContext(KnownColorSpace.DisplayP3))
            using (IDirect2DFactory d2dFactory = Direct2DFactory.Create())
            {
                IBitmapSource? convertedInput = null;

                try
                {
                    PixelFormat pixelFormat = input.PixelFormat;

                    if (pixelFormat == PixelFormats.Rgba128Float || pixelFormat == PixelFormats.Prgba128Float)
                    {
                        convertedInput = input.CreateRef();
                    }
                    else
                    {
                        convertedInput = input.CreateFormatConverter<ColorRgba128Float>();
                    }

                    using (IBitmapSource<ColorPbgra32> dp3Image = PQToColorContext(convertedInput,
                                                                                   imagingFactory,
                                                                                   d2dFactory,
                                                                                   dp3ColorContext))
                    {
                        dp3Image.CopyPixels(output.AsRegionPtr().Cast<ColorPbgra32>());
                    }
                }
                finally
                {
                    convertedInput?.Dispose();
                }
            }

            static IBitmapSource<ColorPbgra32> PQToColorContext(
                IBitmapSource bitmap,
                IImagingFactory imagingFactory,
                IDirect2DFactory d2dFactory,
                IColorContext colorContext)
            {
                return d2dFactory.CreateBitmapSourceFromImage<ColorPbgra32>(
                    bitmap.Size,
                    DevicePixelFormats.Prgba128Float,
                    delegate (IDeviceContext dc)
                    {
                        dc.EffectBufferPrecision = BufferPrecision.Float32;
                        using IDeviceImage srcImage = dc.CreateImageFromBitmap(bitmap, null, BitmapImageOptions.UseStraightAlpha | BitmapImageOptions.DisableColorSpaceConversion);
                        using IDeviceColorContext srcColorContext = dc.CreateColorContext(DxgiColorSpace.RgbFullGamma2084NoneP2020);
                        using IDeviceColorContext dstColorContext = dc.CreateColorContext(colorContext);

                        ColorManagementEffect colorMgmtEffect = new(
                            dc,
                            srcImage,
                            srcColorContext,
                            dstColorContext,
                            ColorManagementAlphaMode.Straight);

                        return colorMgmtEffect;
                    });
            }
        }
    }
}
