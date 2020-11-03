// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020 Nicholas Hayes
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

#include <stdint.h>
#include <math.h>
#include "ChromaSubsampling.h"
#include "YUVConversionHelpers.h"
#include <array>

namespace
{
    struct ColorRgb24Float
    {
        float r;
        float g;
        float b;
    };

    struct YUVBlock
    {
        float y;
        float u;
        float v;
    };

    enum class YuvChannel
    {
        Y,
        U,
        V
    };

    float avifRoundf(float v)
    {
        return floorf(v + 0.5f);
    }

    uint8_t yuvToUNorm(YuvChannel chan, float v)
    {
        if (chan != YuvChannel::Y)
        {
            v += 0.5f;
        }

        if (v < 0.0f)
        {
            v = 0.0f;
        }
        else if (v > 1.0f)
        {
            v = 1.0f;
        }

        return  static_cast<uint8_t>(avifRoundf(v * 255.0f));
    }

    constexpr std::array<float, 256> BuildUint8ToFloatLookupTable()
    {
        std::array<float, 256> table = {};

        for (size_t i = 0; i < table.size(); ++i)
        {
            table[i] = static_cast<float>(i) / 255.0f;
        }

        return table;
    }

    void ColorToIdentity8(
        const BitmapData* bgraImage,
        uint8_t* yPlane,
        size_t yPlaneStride,
        uint8_t* uPlane,
        size_t uPlaneStride,
        uint8_t* vPlane,
        size_t vPlaneStride)
    {
        for (int32_t y = 0; y < bgraImage->height; ++y)
        {
            const ColorBgra* src = reinterpret_cast<const ColorBgra*>(bgraImage->scan0 + (static_cast<int64_t>(y) * bgraImage->stride));
            uint8_t* dstY = &yPlane[y * yPlaneStride];
            uint8_t* dstU = &uPlane[y * uPlaneStride];
            uint8_t* dstV = &vPlane[y * vPlaneStride];

            for (int32_t x = 0; x < bgraImage->width; ++x)
            {
                // RGB -> Identity GBR conversion
                // Formulas 41-43 from https://www.itu.int/rec/T-REC-H.273-201612-I/en

                *dstY = src->g;
                *dstU = src->b;
                *dstV = src->r;

                ++src;
                ++dstY;
                ++dstU;
                ++dstV;
            }
        }
    }

    void ColorToYUV8(
        const BitmapData* bgraImage,
        const CICPColorData& colorInfo,
        YUVChromaSubsampling yuvFormat,
        uint8_t* yPlane,
        intptr_t yPlaneStride,
        uint8_t* uPlane,
        intptr_t uPlaneStride,
        uint8_t* vPlane,
        intptr_t vPlaneStride)
    {
        YUVCoefficiants yuvCoefficiants;
        GetYUVCoefficiants(colorInfo, yuvCoefficiants);

        const float kr = yuvCoefficiants.kr;
        const float kg = yuvCoefficiants.kg;
        const float kb = yuvCoefficiants.kb;

        YUVBlock yuvBlock[2][2];
        ColorRgb24Float rgbPixel;

        static constexpr std::array<float, 256> uint8ToFloatTable = BuildUint8ToFloatLookupTable();

        for (int32_t imageY = 0; imageY < bgraImage->height; imageY += 2)
        {
            for (int32_t imageX = 0; imageX < bgraImage->width; imageX += 2)
            {
                int32_t blockWidth = 2, blockHeight = 2;
                if ((imageX + 1) >= bgraImage->width)
                {
                    blockWidth = 1;
                }
                if ((imageY + 1) >= bgraImage->height)
                {
                    blockHeight = 1;
                }

                // Convert an entire 2x2 block to YUV, and populate any fully sampled channels as we go
                for (int32_t blockY = 0; blockY < blockHeight; ++blockY)
                {
                    for (int32_t blockX = 0; blockX < blockWidth; ++blockX)
                    {
                        int32_t x = imageX + blockX;
                        int32_t y = imageY + blockY;

                        // Unpack RGB into normalized float

                        const ColorBgra* pixel = reinterpret_cast<const ColorBgra*>(bgraImage->scan0 + (static_cast<int64_t>(y) * bgraImage->stride) + (x * sizeof(ColorBgra)));

                        rgbPixel.r = uint8ToFloatTable[pixel->r];
                        rgbPixel.g = uint8ToFloatTable[pixel->g];
                        rgbPixel.b = uint8ToFloatTable[pixel->b];

                        // RGB -> YUV conversion
                        float Y = (kr * rgbPixel.r) + (kg * rgbPixel.g) + (kb * rgbPixel.b);
                        yuvBlock[blockX][blockY].y = Y;
                        yuvBlock[blockX][blockY].u = (rgbPixel.b - Y) / (2 * (1 - kb));
                        yuvBlock[blockX][blockY].v = (rgbPixel.r - Y) / (2 * (1 - kr));

                        yPlane[x + (y * yPlaneStride)] = yuvToUNorm(YuvChannel::Y, yuvBlock[blockX][blockY].y);

                        if (yuvFormat == YUVChromaSubsampling::Subsampling444)
                        {
                            // YUV444, full chroma
                            uPlane[x + (y * uPlaneStride)] = yuvToUNorm(YuvChannel::U, yuvBlock[blockX][blockY].u);
                            vPlane[x + (y * vPlaneStride)] = yuvToUNorm(YuvChannel::V, yuvBlock[blockX][blockY].v);
                        }

                    }
                }

                // Populate any subsampled channels with averages from the 2x2 block
                if (yuvFormat == YUVChromaSubsampling::Subsampling420)
                {
                    // YUV420, average 4 samples (2x2)

                    float sumU = 0.0f;
                    float sumV = 0.0f;
                    for (int32_t bJ = 0; bJ < blockHeight; ++bJ)
                    {
                        for (int32_t bI = 0; bI < blockWidth; ++bI)
                        {
                            sumU += yuvBlock[bI][bJ].u;
                            sumV += yuvBlock[bI][bJ].v;
                        }
                    }
                    float totalSamples = static_cast<float>(blockWidth * blockHeight);
                    float avgU = sumU / totalSamples;
                    float avgV = sumV / totalSamples;

                    int32_t x = imageX >> 1;
                    int32_t y = imageY >> 1;

                    uPlane[x + (y * uPlaneStride)] = yuvToUNorm(YuvChannel::U, avgU);
                    vPlane[x + (y * vPlaneStride)] = yuvToUNorm(YuvChannel::V, avgV);

                }
                else if (yuvFormat == YUVChromaSubsampling::Subsampling422)
                {
                    // YUV422, average 2 samples (1x2), twice

                    for (int32_t blockY = 0; blockY < blockHeight; ++blockY) {
                        float sumU = 0.0f;
                        float sumV = 0.0f;
                        for (int32_t blockX = 0; blockX < blockWidth; ++blockX) {
                            sumU += yuvBlock[blockX][blockY].u;
                            sumV += yuvBlock[blockX][blockY].v;
                        }
                        float totalSamples = static_cast<float>(blockWidth);
                        float avgU = sumU / totalSamples;
                        float avgV = sumV / totalSamples;

                        int32_t x = imageX >> 1;
                        int32_t y = imageY + blockY;

                        uPlane[x + (y * uPlaneStride)] = yuvToUNorm(YuvChannel::U, avgU);
                        vPlane[x + (y * vPlaneStride)] = yuvToUNorm(YuvChannel::V, avgV);
                    }
                }
            }
        }
    }

    void MonoToY8(
        const BitmapData* bgraImage,
        uint8_t* yPlane,
        intptr_t yPlaneStride)
    {
        for (int32_t y = 0; y < bgraImage->height; ++y)
        {
            const ColorBgra* src = reinterpret_cast<const ColorBgra*>(bgraImage->scan0 + (static_cast<int64_t>(y) * bgraImage->stride));
            uint8_t* dst = &yPlane[y * yPlaneStride];

            for (int32_t x = 0; x < bgraImage->width; ++x)
            {
                *dst = src->r;

                src++;
                dst++;
            }
        }
    }

    void AlphaToA8(
        const BitmapData* bgraImage,
        uint8_t* yPlane,
        intptr_t yPlaneStride)
    {
        for (int32_t y = 0; y < bgraImage->height; ++y)
        {
            const ColorBgra* src = reinterpret_cast<const ColorBgra*>(bgraImage->scan0 + (static_cast<int64_t>(y) * bgraImage->stride));
            uint8_t* dst = &yPlane[y * yPlaneStride];

            for (int32_t x = 0; x < bgraImage->width; ++x)
            {
                *dst = src->a;

                src++;
                dst++;
            }
        }
    }

    Status CreateHeifImage(int width, int height, heif_colorspace colorspace, heif_chroma chroma, ScopedHeifImage& image)
    {
        heif_image* heifImage = nullptr;
        heif_error error = heif_image_create(width, height, colorspace, chroma, &heifImage);

        if (error.code != heif_error_Ok)
        {
            switch (error.code)
            {
            case heif_error_Memory_allocation_error:
                return Status::OutOfMemory;
            default:
                return Status::UnknownError;
            }
        }

        image.reset(heifImage);
        return Status::Ok;
    }

    Status AddPlane(heif_image* image, heif_channel channel, int width, int height)
    {
        heif_error error = heif_image_add_plane(image, channel, width, height, 8);

        if (error.code != heif_error_Ok)
        {
            switch (error.code)
            {
            case heif_error_Memory_allocation_error:
                return Status::OutOfMemory;
            default:
                return Status::UnknownError;
            }
        }

        return Status::Ok;
    }

    int GetChromaPlaneHeight(int imageHeight, heif_chroma chroma)
    {
        switch (chroma)
        {

        case heif_chroma_420:
            return imageHeight / 2;
            break;
        case heif_chroma_422:
        case heif_chroma_444:
        default:
            return imageHeight;
        }
    }

    int GetChromaPlaneWidth(int imageWidth, heif_chroma chroma)
    {
        switch (chroma)
        {

        case heif_chroma_420:
        case heif_chroma_422:
            return imageWidth / 2;
            break;
        case heif_chroma_444:
        default:
            return imageWidth;
        }
    }

    Status CreateImagePlanes(heif_image* image, int width, int height, heif_colorspace colorspace, heif_chroma chroma, bool hasTransparency)
    {
        Status status = AddPlane(image, heif_channel_Y, width, height);

        if (status == Status::Ok)
        {
            if (colorspace == heif_colorspace_YCbCr)
            {
                const int chromaPlaneWidth = GetChromaPlaneWidth(width, chroma);
                const int chromaPlaneHeight = GetChromaPlaneHeight(height, chroma);

                status = AddPlane(image, heif_channel_Cb, chromaPlaneWidth, chromaPlaneHeight);

                if (status == Status::Ok)
                {
                    status = AddPlane(image, heif_channel_Cr, chromaPlaneWidth, chromaPlaneHeight);
                }
            }

            if (hasTransparency && status == Status::Ok)
            {
                status = AddPlane(image, heif_channel_Alpha, width, height);
            }
        }

        return status;
    }

    bool HasTransparency(const BitmapData* image)
    {
        for (int32_t y = 0; y < image->height; y++)
        {
            ColorBgra* ptr = reinterpret_cast<ColorBgra*>(image->scan0 + (static_cast<intptr_t>(y) * image->stride));

            for (int32_t x = 0; x < image->width; x++)
            {
                if (ptr->a < 255)
                {
                    return true;
                }

                ptr++;
            }
        }

        return false;
    }
}


Status ConvertToHeifImage(
    const BitmapData* bgraImage,
    const CICPColorData& colorInfo,
    YUVChromaSubsampling yuvFormat,
    ScopedHeifImage& convertedImage)
{
    heif_colorspace colorspace;
    heif_chroma chroma;

    switch (yuvFormat)
    {
    case YUVChromaSubsampling::Subsampling400:
        chroma = heif_chroma_monochrome;
        colorspace = heif_colorspace_monochrome;
        break;
    case YUVChromaSubsampling::Subsampling420:
        chroma = heif_chroma_420;
        colorspace = heif_colorspace_YCbCr;
        break;
    case YUVChromaSubsampling::Subsampling422:
        chroma = heif_chroma_422;
        colorspace = heif_colorspace_YCbCr;
        break;
    case YUVChromaSubsampling::Subsampling444:
    case YUVChromaSubsampling::IdentityMatrix:
        chroma = heif_chroma_444;
        colorspace = heif_colorspace_YCbCr;
        break;
    default:
        return Status::UnknownYUVFormat;
    }

    ScopedHeifImage heifImage;

    Status status = CreateHeifImage(bgraImage->width, bgraImage->height, colorspace, chroma, heifImage);

    if (status == Status::Ok)
    {
        const bool hasTransparency = HasTransparency(bgraImage);

        status = CreateImagePlanes(heifImage.get(), bgraImage->width, bgraImage->height, colorspace, chroma, hasTransparency);

        if (status == Status::Ok)
        {
            if (colorspace == heif_colorspace_monochrome)
            {
                int yPlaneStride;
                uint8_t* yPlane = heif_image_get_plane(heifImage.get(), heif_channel_Y, &yPlaneStride);

                MonoToY8(
                    bgraImage,
                    yPlane,
                    static_cast<intptr_t>(yPlaneStride));
            }
            else
            {
                int yPlaneStride;
                uint8_t* yPlane = heif_image_get_plane(heifImage.get(), heif_channel_Y, &yPlaneStride);
                int uPlaneStride;
                uint8_t* uPlane = heif_image_get_plane(heifImage.get(), heif_channel_Cb, &uPlaneStride);
                int vPlaneStride;
                uint8_t* vPlane = heif_image_get_plane(heifImage.get(), heif_channel_Cr, &vPlaneStride);

                if (yuvFormat == YUVChromaSubsampling::IdentityMatrix)
                {
                    // The IdentityMatrix format places the RGB values into the YUV planes
                    // without any conversion.
                    // This reduces the compression efficiency, but allows for fully lossless encoding.
                    ColorToIdentity8(
                        bgraImage,
                        yPlane,
                        static_cast<intptr_t>(yPlaneStride),
                        uPlane,
                        static_cast<intptr_t>(uPlaneStride),
                        vPlane,
                        static_cast<intptr_t>(vPlaneStride));
                }
                else
                {
                    ColorToYUV8(
                        bgraImage,
                        colorInfo,
                        yuvFormat,
                        yPlane,
                        static_cast<intptr_t>(yPlaneStride),
                        uPlane,
                        static_cast<intptr_t>(uPlaneStride),
                        vPlane,
                        static_cast<intptr_t>(vPlaneStride));
                }
            }

            if (hasTransparency)
            {
                int alphaPlaneStride;
                uint8_t* alphaPlane = heif_image_get_plane(heifImage.get(), heif_channel_Alpha, &alphaPlaneStride);

                AlphaToA8(
                    bgraImage,
                    alphaPlane,
                    static_cast<intptr_t>(alphaPlaneStride));
            }

            convertedImage.swap(heifImage);
        }
    }

    return status;
}
