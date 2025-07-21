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

using System;

namespace HeicFileTypePlus.Decoding
{
    internal sealed class HighBitDepthExpansionHDR
    {
        private readonly int maxValue;
        private readonly float[] lookupTable;

        public HighBitDepthExpansionHDR(int bitDepth)
        {
            if (bitDepth != 10 && bitDepth != 12 && bitDepth != 16)
            {
                throw new FormatException($"Unsupported HEIF image bit depth: {bitDepth}.");
            }

            int count = 1 << bitDepth;
            this.maxValue = count - 1;

            this.lookupTable = new float[count];

            // Reciprocal multiplication is used to avoid having to repeatedly divide
            // by the max value.
            float depthRangeToFloatMultiplier = 1f / this.maxValue;

            for (int i = 0; i < count; i++)
            {
                // Map the (possibly) limited range value to a float in the range of 0.0-1.0.
                // Most high bit depth HEIF images use 10-bit or 12-bit.

                this.lookupTable[i] = i * depthRangeToFloatMultiplier;
            }
        }

        public float GetExpandedValue(ushort limitedRangeValue)
        {
            int clampedValue = limitedRangeValue & this.maxValue;

            return this.lookupTable[clampedValue];
        }
    }
}
