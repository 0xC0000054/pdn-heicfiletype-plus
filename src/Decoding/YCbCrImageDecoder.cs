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

namespace HeicFileTypePlus.Decoding
{
    internal static class YCbCrImageDecoder
    {
        public static void SetImageData(
            IImagingFactory factory,
            HeifImageHandle imageHandle,
            Surface output)
        {
            int bitDepth = imageHandle.BitDepth;

            HeifChroma decodeFormat;

            if (imageHandle.HasAlphaChannel)
            {
                decodeFormat = bitDepth == 8 ? HeifChroma.InterleavedRgba32 : HeifChroma.InterleavedRgba64LE;
            }
            else
            {
                decodeFormat = bitDepth == 8 ? HeifChroma.InterleavedRgb24 : HeifChroma.InterleavedRgb48LE;
            }

            using (HeifImage image = imageHandle.Decode(HeifColorSpace.Rgb, decodeFormat))
            {
                RgbImageDecoder.SetImageData(factory, image, output);
            }
        }
    }
}
