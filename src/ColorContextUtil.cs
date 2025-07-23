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

using HeicFileTypePlus.ICCProfile;
using HeicFileTypePlus.Interop;
using PaintDotNet.Imaging;
using System;

namespace HeicFileTypePlus
{
    internal static class ColorContextUtil
    {
        public static IColorContext? TryCreateFromCICP(CICPColorData colorData, IImagingFactory imagingFactory)
        {
            IColorContext? colorContext = null;

            if (colorData.colorPrimaries == CICPColorPrimaries.BT709)
            {
                switch (colorData.transferCharacteristics)
                {
                    case CICPTransferCharacteristics.Linear:
                        colorContext = imagingFactory.CreateColorContext(KnownColorSpace.ScRgb);
                        break;
                    case CICPTransferCharacteristics.Srgb:
                        colorContext = imagingFactory.CreateColorContext(KnownColorSpace.Srgb);
                        break;
                }
            }
            else if (colorData.colorPrimaries == CICPColorPrimaries.Smpte432)
            {
                // DisplayP3 uses SMPTE EG 432-1 primaries with the sRGB transfer curve.
                if (colorData.transferCharacteristics == CICPTransferCharacteristics.Srgb)
                {
                    colorContext = imagingFactory.CreateColorContext(KnownColorSpace.DisplayP3);
                }
            }

            return colorContext;
        }

        public static IColorContext? TryCreateFromRgbProfile(ReadOnlySpan<byte> profileBytes, IImagingFactory imagingFactory)
        {
            IColorContext? colorContext = null;

            if (profileBytes.Length > ProfileHeader.SizeOf
                && new ProfileHeader(profileBytes).ColorSpace == ProfileColorSpace.Rgb)
            {
                try
                {
                    colorContext = imagingFactory.CreateColorContext(profileBytes);
                }
                catch (Exception)
                {
                    // Ignore it.
                }
            }

            return colorContext;
        }
    }
}
