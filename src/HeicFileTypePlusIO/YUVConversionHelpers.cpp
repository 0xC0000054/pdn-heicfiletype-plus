// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020, 2021, 2022, 2024 Nicholas Hayes
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
//
// Portions of this file has been adapted from libavif, https://github.com/AOMediaCodec/libavif
/*
    Copyright 2019 Joe Drago. All rights reserved.

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:

    1. Redistributions of source code must retain the above copyright notice, this
    list of conditions and the following disclaimer.

    2. Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
    AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
    OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
    OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

#include "YUVConversionHelpers.h"
#include <memory>

namespace
{
    struct avifColourPrimariesTable
    {
        heif_color_primaries colourPrimariesEnum;
        const char* name;
        float primaries[8]; // rX, rY, gX, gY, bX, bY, wX, wY
    };
    static const struct avifColourPrimariesTable avifColourPrimariesTables[] = {
        { heif_color_primaries_ITU_R_BT_709_5, "BT.709", { 0.64f, 0.33f, 0.3f, 0.6f, 0.15f, 0.06f, 0.3127f, 0.329f } },
        { heif_color_primaries_ITU_R_BT_470_6_System_M, "BT.470-6 System M", { 0.67f, 0.33f, 0.21f, 0.71f, 0.14f, 0.08f, 0.310f, 0.316f } },
        { heif_color_primaries_ITU_R_BT_470_6_System_B_G, "BT.470-6 System BG", { 0.64f, 0.33f, 0.29f, 0.60f, 0.15f, 0.06f, 0.3127f, 0.3290f } },
        { heif_color_primaries_ITU_R_BT_601_6, "BT.601", { 0.630f, 0.340f, 0.310f, 0.595f, 0.155f, 0.070f, 0.3127f, 0.3290f } },
        { heif_color_primaries_SMPTE_240M, "SMPTE 240M", { 0.630f, 0.340f, 0.310f, 0.595f, 0.155f, 0.070f, 0.3127f, 0.3290f } },
    };
    static const int avifColourPrimariesTableSize = sizeof(avifColourPrimariesTables) / sizeof(avifColourPrimariesTables[0]);

    void avifNclxColourPrimariesGetValues(heif_color_primaries ancp, float outPrimaries[8]) {
        for (int i = 0; i < avifColourPrimariesTableSize; ++i) {
            if (avifColourPrimariesTables[i].colourPrimariesEnum == ancp) {
                memcpy(outPrimaries, avifColourPrimariesTables[i].primaries, sizeof(avifColourPrimariesTables[i].primaries));
                return;
            }
        }

        // if we get here, the color primaries are unknown. Just return a reasonable default.
        memcpy(outPrimaries, avifColourPrimariesTables[0].primaries, sizeof(avifColourPrimariesTables[0].primaries));
    }

    struct avifMatrixCoefficientsTable
    {
        heif_matrix_coefficients matrixCoefficientsEnum;
        const char* name;
        const float kr;
        const float kb;
    };

    // https://www.itu.int/rec/T-REC-H.273-201612-I/en
    static const struct avifMatrixCoefficientsTable matrixCoefficientsTables[] = {
        { heif_matrix_coefficients_ITU_R_BT_709_5, "BT.709", 0.2126f, 0.0722f },
        { heif_matrix_coefficients_US_FCC_T47, "FCC USFC 73.682", 0.30f, 0.11f },
        { heif_matrix_coefficients_ITU_R_BT_470_6_System_B_G, "BT.470-6 System BG", 0.299f, 0.114f },
        { heif_matrix_coefficients_ITU_R_BT_601_6, "BT.601", 0.299f, 0.144f },
        { heif_matrix_coefficients_SMPTE_240M, "SMPTE ST 240", 0.212f, 0.087f },
    };

    static const int avifMatrixCoefficientsTableSize = sizeof(matrixCoefficientsTables) / sizeof(matrixCoefficientsTables[0]);

    bool calcYUVInfoFromCICP(const CICPColorData& cicp, float coeffs[3]) {

        for (int i = 0; i < avifMatrixCoefficientsTableSize; ++i) {
            const struct avifMatrixCoefficientsTable* const table = &matrixCoefficientsTables[i];
            if (table->matrixCoefficientsEnum == cicp.matrixCoefficients) {
                coeffs[0] = table->kr;
                coeffs[2] = table->kb;
                coeffs[1] = 1.0f - coeffs[0] - coeffs[2];
                return true;
            }
        }

        return false;
    }
}

void GetYUVCoefficiants(const CICPColorData& colorInfo, YUVCoefficiants& yuvData)
{
    // sRGB (BT.709) defaults
    float kr = 0.2126f;
    float kb = 0.0722f;
    float kg = 1.0f - kr - kb;

    float coeffs[3];

    if (calcYUVInfoFromCICP(colorInfo, coeffs))
    {
        kr = coeffs[0];
        kg = coeffs[1];
        kb = coeffs[2];
    }

    yuvData.kr = kr;
    yuvData.kg = kg;
    yuvData.kb = kb;
}
