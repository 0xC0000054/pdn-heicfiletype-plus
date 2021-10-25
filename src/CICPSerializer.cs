// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020, 2021 Nicholas Hayes
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
using System;
using System.Globalization;

namespace HeicFileTypePlus
{
    internal static class CICPSerializer
    {
        private const string ColorPrimariesPropertyName = "ColorPrimaries";
        private const string TransferCharacteristicsPropertyName = "TransferCharacteristics";
        private const string MatrixCoefficientsPropertyName = "MatrixCoefficients";

        public static CICPColorData? TryDeserialize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (!value.StartsWith("<CICP", StringComparison.Ordinal))
            {
                return null;
            }

            int colorPrimaries = GetPropertyValue(value, ColorPrimariesPropertyName);
            int transferCharacteristics = GetPropertyValue(value, TransferCharacteristicsPropertyName);
            int matrixCoefficients = GetPropertyValue(value, MatrixCoefficientsPropertyName);
            // We always use the full RGB/YUV color range.
            const bool fullRange = true;

            return new CICPColorData
            {
                colorPrimaries = (CICPColorPrimaries)colorPrimaries,
                transferCharacteristics = (CICPTransferCharacteristics)transferCharacteristics,
                matrixCoefficients = (CICPMatrixCoefficients)matrixCoefficients,
                fullRange = fullRange
            };
        }

        public static string TrySerialize(CICPColorData cicpColor)
        {
            // The identity matrix coefficient is never serialized.
            if (cicpColor.matrixCoefficients == CICPMatrixCoefficients.Identity)
            {
                return null;
            }

            if (cicpColor.colorPrimaries == CICPColorPrimaries.Unspecified ||
                cicpColor.transferCharacteristics == CICPTransferCharacteristics.Unspecified ||
                cicpColor.matrixCoefficients == CICPMatrixCoefficients.Unspecified)
            {
                return null;
            }

            int colorPrimaries = (int)cicpColor.colorPrimaries;
            int transferCharacteristics = (int)cicpColor.transferCharacteristics;
            int matrixCoefficients = (int)cicpColor.matrixCoefficients;

            return string.Format(CultureInfo.InvariantCulture,
                                 "<CICP {0}=\"{1}\" {2}=\"{3}\" {4}=\"{5}\"/>",
                                 ColorPrimariesPropertyName,
                                 colorPrimaries.ToString(CultureInfo.InvariantCulture),
                                 TransferCharacteristicsPropertyName,
                                 transferCharacteristics.ToString(CultureInfo.InvariantCulture),
                                 MatrixCoefficientsPropertyName,
                                 matrixCoefficients.ToString(CultureInfo.InvariantCulture));
        }

        private static int GetPropertyValue(string haystack, string propertyName)
        {
            string needle = propertyName + "=\"";

            int valueStartIndex = haystack.IndexOf(needle, StringComparison.Ordinal) + needle.Length;
            int valueEndIndex = haystack.IndexOf('"', valueStartIndex);

            string propertyValue = haystack.Substring(valueStartIndex, valueEndIndex - valueStartIndex);

            return int.Parse(propertyValue, CultureInfo.InvariantCulture);
        }
    }
}
