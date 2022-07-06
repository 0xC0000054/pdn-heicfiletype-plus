// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
// FileType plugin for Paint.NET.
//
// Copyright (C) 2020, 2021, 2022 Nicholas Hayes
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

    Status SetChromaSubsampling(heif_encoder* const encoder, YUVChromaSubsampling chroma)
    {
        const char* chromaString;

        switch (chroma)
        {
        case YUVChromaSubsampling::Subsampling400:
        case YUVChromaSubsampling::Subsampling420:
            chromaString = "420";
            break;
        case YUVChromaSubsampling::Subsampling422:
            chromaString = "422";
            break;
        case YUVChromaSubsampling::Subsampling444:
        case YUVChromaSubsampling::IdentityMatrix:
            chromaString = "444";
            break;
        default:
            return Status::UnknownYUVFormat;
        }

        return SetEncoderParameter(encoder, "chroma", chromaString);
    }

    Status ConfigureEncoderSettings(heif_encoder* const encoder, const EncoderOptions* const options)
    {
        Status status = Status::Ok;

        heif_error error;

        // LibHeif requires the lossy quality to be always be set, if it has
        // not been set the encoder will produce a corrupted image.
        error = heif_encoder_set_lossy_quality(encoder, options->quality);

        if (options->quality == 100 && error.code == heif_error_Ok)
        {
            error = heif_encoder_set_lossless(encoder, true);
        }

        if (error.code != heif_error_Ok)
        {
            switch (error.code)
            {
            case heif_error_Memory_allocation_error:
                status = Status::OutOfMemory;
                break;
            default:
                status = Status::EncodeFailed;
            }
        }

        if (status == Status::Ok)
        {
            status = SetChromaSubsampling(encoder, options->yuvFormat);

            if (status == Status::Ok)
            {
                status = SetEncoderParameter(encoder, "preset", GetPresetString(options->preset));

                if (status == Status::Ok)
                {
                    if (options->tuning != EncoderTuning::None)
                    {
                        status = SetEncoderParameter(encoder, "tune", GetTuningString(options->tuning));
                    }

                    if (status == Status::Ok)
                    {
                        status = SetEncoderParameter(encoder, "tu-intra-depth", options->tuIntraDepth);
                    }
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
                        break;
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

        Status status = Status::Ok;

        if (iccProfile && iccProfileSize)
        {
            status = AddICCProfileToImage(image, iccProfile, iccProfileSize);
        }

        if (status == Status::Ok)
        {
            // The CICP color data is always added to the image, it will be
            // stored in the HEVC VUI data if the image has an ICC color profile.
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

    try
    {
        ScopedHeifImage yuvImage;

        Status status = ConvertToHeifImage(input, colorData, options->yuvFormat, yuvImage);

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
