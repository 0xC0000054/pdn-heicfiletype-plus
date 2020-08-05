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

#include "HeicDecoder.h"
#include "scoped.h"

namespace
{
    struct ColorRgb
    {
        uint8_t r;
        uint8_t g;
        uint8_t b;
    };

    struct ColorRgba
    {
        uint8_t r;
        uint8_t g;
        uint8_t b;
        uint8_t a;
    };

    void DecodeRGBImage(const heif_image* image, BitmapData* output)
    {
        int stride;

        const uint8_t* data = heif_image_get_plane_readonly(image, heif_channel_interleaved, &stride);

        for (int y = 0; y < output->height; y++)
        {
            const ColorRgb* src = reinterpret_cast<const ColorRgb*>(data + (static_cast<int64_t>(y) * stride));
            ColorBgra* dst = reinterpret_cast<ColorBgra*>(output->scan0 + (static_cast<int64_t>(y) * output->stride));

            for (int x = 0; x < output->width; x++)
            {
                dst->r = src->r;
                dst->g = src->g;
                dst->b = src->b;
                dst->a = 255;

                src++;
                dst++;
            }
        }
    }

    void DecodeRGBAImage(const heif_image* image, BitmapData* output)
    {
        int stride;

        const uint8_t* data = heif_image_get_plane_readonly(image, heif_channel_interleaved, &stride);

        for (int y = 0; y < output->height; y++)
        {
            const ColorRgba* src = reinterpret_cast<const ColorRgba*>(data + (static_cast<int64_t>(y) * stride));
            ColorBgra* dst = reinterpret_cast<ColorBgra*>(output->scan0 + (static_cast<int64_t>(y) * output->stride));

            for (int x = 0; x < output->width; x++)
            {
                dst->r = src->r;
                dst->g = src->g;
                dst->b = src->b;
                dst->a = src->a;

                src++;
                dst++;
            }
        }
    }

    Status DecodeHeifImage(
        const heif_image_handle* const imageHandle,
        heif_chroma outputFormat,
        const heif_decoding_options* const options,
        ScopedHeifImage& heifImage)
    {
        heif_image* image;

        heif_error error = heif_decode_image(imageHandle, &image, heif_colorspace_RGB, outputFormat, options);

        if (error.code != heif_error_Ok)
        {
            switch (error.code)
            {
            case heif_error_Memory_allocation_error:
                return Status::OutOfMemory;
            default:
                return Status::DecodeFailed;
            }
        }

        heifImage.reset(image);
        return Status::Ok;
    }
}

Status HeicDecoder::Decode(heif_image_handle* const imageHandle, BitmapData* output)
{
    if (!imageHandle || !output)
    {
        return Status::NullParameter;
    }

    try
    {
        ScopedHeifDecodingOptions options(heif_decoding_options_alloc());

        if (!options)
        {
            return Status::OutOfMemory;
        }

        options->convert_hdr_to_8bit = true;

        bool hasAlpha = heif_image_handle_has_alpha_channel(imageHandle);

        const heif_chroma outputFormat = hasAlpha ? heif_chroma_interleaved_RGBA : heif_chroma_interleaved_RGB;

        ScopedHeifImage image;

        Status status = DecodeHeifImage(imageHandle, outputFormat, options.get(), image);

        if (status == Status::Ok)
        {
            if (hasAlpha)
            {
                DecodeRGBAImage(image.get(), output);
            }
            else
            {
                DecodeRGBImage(image.get(), output);
            }
        }

        return status;
    }
    catch (const std::bad_alloc&)
    {
        return Status::OutOfMemory;
    }
    catch (...)
    {
        return Status::DecodeFailed;
    }

    return Status::Ok;
}
