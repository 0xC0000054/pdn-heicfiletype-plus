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

#include "HeicEncoder.h"
#include "ChromaSubsampling.h"
#include "HeicMetadata.h"
#include "ProgressSteps.h"

namespace
{
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

    Status GetEncoder(heif_context* context, ScopedHeifEncoder& scopedEncoder)
    {
        if (!context)
        {
            return Status::NullParameter;
        }

        heif_encoder* encoder;

        heif_error error = heif_context_get_encoder_for_format(context, heif_compression_HEVC, &encoder);

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

        scopedEncoder.reset(encoder);
        return Status::Ok;
    }

    const char* GetPresetString(EncoderPreset preset)
    {
        switch (preset)
        {
        case EncoderPreset::UltraFast:
            return "ultrafast";
        case EncoderPreset::SuperFast:
            return "superfast";
        case EncoderPreset::VeryFast:
            return "veryfast";
        case EncoderPreset::Faster:
            return "faster";
        case EncoderPreset::Fast:
            return "fast";
        case EncoderPreset::Slow:
            return "slow";
        case EncoderPreset::Slower:
            return "slower";
        case EncoderPreset::VerySlow:
            return "veryslow";
        case EncoderPreset::Placebo:
            return "placebo";
        case EncoderPreset::Medium:
        default:
            return "medium";
        }
    }

    const char* GetTuningString(EncoderTuning tuning)
    {
        switch (tuning)
        {
        case EncoderTuning::PSNR:
            return "psnr";
        case EncoderTuning::FilmGrain:
            return "grain";
        case EncoderTuning::FastDecode:
            return "fastdecode";
        case EncoderTuning::SSIM:
        default:
            return "ssim";
        }
    }

    Status SetEncoderParameter(heif_encoder* const encoder, const char* const name, const char* const value)
    {
        heif_error error = heif_encoder_set_parameter_string(encoder, name, value);

        if (error.code != heif_error_Ok)
        {
            switch (error.code)
            {
            case heif_error_Memory_allocation_error:
                return Status::OutOfMemory;
            default:
                return Status::EncodeFailed;
            }
        }

        return Status::Ok;
    }

    Status SetEncoderParameter(heif_encoder* const encoder, const char* const name, int value)
    {
        heif_error error = heif_encoder_set_parameter_integer(encoder, name, value);

        if (error.code != heif_error_Ok)
        {
            switch (error.code)
            {
            case heif_error_Memory_allocation_error:
                return Status::OutOfMemory;
            default:
                return Status::EncodeFailed;
            }
        }

        return Status::Ok;
    }

    Status ConfigureEncoderSettings(heif_encoder* const encoder, const EncoderOptions* const options)
    {
        Status status = Status::Ok;

        heif_error error = heif_encoder_set_lossy_quality(encoder, options->quality);

        if (error.code != heif_error_Ok)
        {
            switch (error.code)
            {
            case heif_error_Memory_allocation_error:
                status = Status::OutOfMemory;
            default:
                status = Status::EncodeFailed;
            }
        }

        if (status == Status::Ok)
        {
            status = SetEncoderParameter(encoder, "preset", GetPresetString(options->preset));

            if (status == Status::Ok)
            {
                status = SetEncoderParameter(encoder, "tune", GetTuningString(options->tuning));

                if (status == Status::Ok)
                {
                    status = SetEncoderParameter(encoder, "tu-intra-depth", options->tuIntraDepth);
                }
            }
        }

        return status;
    }

    Status EncodeImage(
        heif_context* const context,
        heif_image* const image,
        const EncoderOptions* const  options,
        ScopedHeifImageHandle& encodedImage)
    {
        if (!context || !image || !options)
        {
            return Status::NullParameter;
        }

        ScopedHeifEncoder encoder;

        Status status = GetEncoder(context, encoder);

        if (status == Status::Ok)
        {
            status = ConfigureEncoderSettings(encoder.get(), options);

            if (status == Status::Ok)
            {
                heif_image_handle* outputImage;

                heif_error error = heif_context_encode_image(context, image, encoder.get(), nullptr, &outputImage);

                if (error.code != heif_error_Ok)
                {
                    switch (error.code)
                    {
                    case heif_error_Memory_allocation_error:
                        status = Status::OutOfMemory;
                    default:
                        status = Status::EncodeFailed;
                    }
                }

                if (status == Status::Ok)
                {
                    encodedImage.reset(outputImage);
                }
            }
        }

        return status;
    }

    Status AddColorProfile(
        heif_image* const image,
        const CICPColorData& cicp,
        const uint8_t* iccProfile,
        int iccProfileSize)
    {
        if (!image)
        {
            return Status::NullParameter;
        }

        Status status;

        if (iccProfile && iccProfileSize)
        {
            status = AddICCProfileToImage(image, iccProfile, iccProfileSize);
        }
        else
        {
            status = AddNclxProfileToImage(image, cicp);
        }

        return status;
    }

    Status AddExifAndXmpMetadata(
        heif_context* const context,
        heif_image_handle* const image,
        const EncoderMetadata* metadata)
    {
        if (!context || !image || !metadata)
        {
            return Status::NullParameter;
        }

        Status status = AddExifToImage(context, image, metadata->exif, metadata->exifSize);

        if (status == Status::Ok)
        {
            status = AddXmpToImage(context, image, metadata->xmp, metadata->xmpSize);
        }

        return status;
    }
}

Status HeicEncoder::Encode(
    heif_context* const context,
    const BitmapData* input,
    const EncoderOptions* options,
    const EncoderMetadata* metadata,
    const CICPColorData& colorData,
    const ProgressProc progressCallback)
{
    if (!context || !input || !options || !metadata)
    {
        return Status::NullParameter;
    }

    if (progressCallback)
    {
        if (!progressCallback(BeforeImageConversion))
        {
            return Status::UserCanceled;
        }
    }

    const bool hasTransparency = HasTransparency(input);

    try
    {
        ScopedHeifImage yuvImage;

        Status status = ConvertToHeifImage(input, hasTransparency, colorData, options->yuvFormat, yuvImage);

        if (status == Status::Ok)
        {
            if (progressCallback)
            {
                if (!progressCallback(BeforeCompression))
                {
                    return Status::UserCanceled;
                }
            }

            status = AddColorProfile(yuvImage.get(), colorData, metadata->iccProfile, metadata->iccProfileSize);

            if (status == Status::Ok)
            {
                ScopedHeifImageHandle encodedImage;

                status = EncodeImage(context, yuvImage.get(), options, encodedImage);

                if (status == Status::Ok)
                {
                    status = AddExifAndXmpMetadata(context, encodedImage.get(), metadata);

                    if (progressCallback && status == Status::Ok)
                    {
                        if (!progressCallback(AfterCompression))
                        {
                            status = Status::UserCanceled;
                        }
                    }
                }
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
        return Status::EncodeFailed;
    }
}
