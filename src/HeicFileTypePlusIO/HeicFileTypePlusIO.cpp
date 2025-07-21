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

// HeicFileTypePlusIO.cpp : Defines the exported functions for the DLL.
//

#include "HeicFileTypePlusIO.h"
#include "HeicEncoder.h"
#include "HeicMetadata.h"
#include "HeicReader.h"
#include "HeicWriter.h"
#include <string>
#include <vector>

heif_context* __stdcall CreateContext()
{
    try
    {
        return heif_context_alloc();
    }
    catch (...)
    {
        return nullptr;
    }
}

bool __stdcall DeleteContext(heif_context* context)
{
    heif_context_free(context);

    return true;
}

bool __stdcall DeleteImageHandle(heif_image_handle* handle)
{
    heif_image_handle_release(handle);

    return true;
}

bool __stdcall DeleteImage(heif_image* handle)
{
    heif_image_release(handle);

    return true;
}

Status __stdcall LoadFileIntoContext(
    heif_context* context,
    IOCallbacks* callbacks,
    const CopyErrorDetails copyErrorDetails)
{
    return HeicReader::LoadFileIntoContext(context, callbacks, copyErrorDetails);
}

Status __stdcall GetPrimaryImage(
    heif_context* context,
    heif_image_handle** primaryImageHandle,
    ImageHandleInfo* info,
    const CopyErrorDetails copyErrorDetails)
{
    if (!context || !primaryImageHandle || !info)
    {
        return Status::NullParameter;
    }

    heif_error error = heif_context_get_primary_image_handle(context, primaryImageHandle);

    if (error.code != heif_error_Ok)
    {
        switch (error.code)
        {
        case heif_error_Memory_allocation_error:
            return Status::OutOfMemory;
        case heif_error_Unsupported_feature:
            if (copyErrorDetails)
            {
                copyErrorDetails(error.message);
            }
            return Status::UnsupportedFeature;
        case heif_error_Unsupported_filetype:
            return Status::UnsupportedFormat;
        case heif_error_Invalid_input:
        default:
            if (copyErrorDetails)
            {
                copyErrorDetails(error.message);
            }
            return Status::InvalidFile;
        }
    }

    info->width = heif_image_handle_get_width(*primaryImageHandle);
    info->height = heif_image_handle_get_height(*primaryImageHandle);
    info->bitDepth = heif_image_handle_get_luma_bits_per_pixel(*primaryImageHandle);

    switch (heif_image_handle_get_color_profile_type(*primaryImageHandle))
    {
    case heif_color_profile_type_prof:
    case heif_color_profile_type_rICC:
        info->colorProfileType = ImageHandleColorProfileType::Icc;
        break;
    case heif_color_profile_type_nclx:
        info->colorProfileType = ImageHandleColorProfileType::Cicp;
        break;
    case heif_color_profile_type_not_present:
    default:
        info->colorProfileType = ImageHandleColorProfileType::NotPresent;
        break;
    }

    info->hasAlpha = heif_image_handle_has_alpha_channel(*primaryImageHandle);

    return Status::Ok;
}

Status __stdcall DecodeImage(
    heif_image_handle* const imageHandle,
    heif_colorspace colorSpace,
    heif_chroma chroma,
    heif_image** outputImage,
    DecodedImageInfo* info)
{
    if (!imageHandle || !outputImage || !info)
    {
        return Status::NullParameter;
    }

    ScopedHeifDecodingOptions options(heif_decoding_options_alloc());

    if (!options)
    {
        return Status::OutOfMemory;
    }

    heif_error error = heif_decode_image(imageHandle, outputImage, colorSpace, chroma, options.get());

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

    info->colorSpace = heif_image_get_colorspace(*outputImage);
    info->chroma = heif_image_get_chroma_format(*outputImage);
    return Status::Ok;
}

uint8_t* __stdcall GetHeifImageChannel(heif_image* image, heif_channel channel, int* channelStride)
{
    return heif_image_get_plane(image, channel, channelStride);
}

Status __stdcall GetICCProfileSize(heif_image_handle* imageHandle, size_t* size)
{
    if (!imageHandle || !size)
    {
        return Status::NullParameter;
    }

    *size = heif_image_handle_get_raw_color_profile_size(imageHandle);

    return Status::Ok;
}

Status __stdcall GetICCProfile(heif_image_handle* imageHandle, uint8_t* buffer, size_t bufferSize)
{
    if (!imageHandle || !buffer)
    {
        return Status::NullParameter;
    }

    if (bufferSize < heif_image_handle_get_raw_color_profile_size(imageHandle))
    {
        return Status::BufferTooSmall;
    }

    heif_error error = heif_image_handle_get_raw_color_profile(imageHandle, buffer);

    if (error.code != heif_error_Ok)
    {
        switch (error.code)
        {
        case heif_error_Memory_allocation_error:
            return Status::OutOfMemory;
        default:
            return Status::ColorInformationError;
        }
    }

    return Status::Ok;
}

Status __stdcall GetCICPColorData(heif_image_handle* imageHandle, CICPColorData* data)
{
    if (!imageHandle || !data)
    {
        return Status::NullParameter;
    }

    heif_color_profile_nclx* nclxProfile;

    heif_error error = heif_image_handle_get_nclx_color_profile(imageHandle, &nclxProfile);

    if (error.code == heif_error_Ok)
    {
        data->colorPrimaries = nclxProfile->color_primaries;
        data->transferCharacteristics = nclxProfile->transfer_characteristics;
        data->matrixCoefficients = nclxProfile->matrix_coefficients;
        data->fullRange = nclxProfile->full_range_flag;

        heif_nclx_color_profile_free(nclxProfile);
    }
    else if (error.code == heif_error_Color_profile_does_not_exist)
    {
        data->colorPrimaries = heif_color_primaries_unspecified;
        data->transferCharacteristics = heif_transfer_characteristic_unspecified;
        data->matrixCoefficients = heif_matrix_coefficients_unspecified;
        data->fullRange = false;
    }
    else
    {
        switch (error.code)
        {
        case heif_error_Memory_allocation_error:
            return Status::OutOfMemory;
        default:
            return Status::ColorInformationError;
        }
    }

    return Status::Ok;
}

Status __stdcall GetMetadataId(heif_image_handle* imageHandle, MetadataType type, heif_item_id* id)
{
    if (!imageHandle || !id)
    {
        return Status::NullParameter;
    }

    Status status;

    switch (type)
    {
    case MetadataType::Exif:
        status = GetExifMetadataID(imageHandle, id);
        break;
    case MetadataType::Xmp:
        status = GetXmpMetadataID(imageHandle, id);
        break;
    default:
        status = Status::InvalidParameter;
        break;
    }

    return status;
}

Status __stdcall GetMetadataSize(heif_image_handle* imageHandle, heif_item_id id, size_t* size)
{
    if (!imageHandle || !size)
    {
        return Status::NullParameter;
    }

    *size = heif_image_handle_get_metadata_size(imageHandle, id);
    return Status::Ok;
}

Status __stdcall GetMetadata(heif_image_handle* imageHandle, heif_item_id id, uint8_t* buffer, size_t bufferSize)
{
    if (!imageHandle || !buffer)
    {
        return Status::NullParameter;
    }

    if (bufferSize < heif_image_handle_get_metadata_size(imageHandle, id))
    {
        return Status::BufferTooSmall;
    }

    heif_error error = heif_image_handle_get_metadata(imageHandle, id, buffer);

    if (error.code != heif_error_Ok)
    {
        switch (error.code)
        {
        case heif_error_Memory_allocation_error:
            return Status::OutOfMemory;
        default:
            return Status::MetadataError;
        }
    }

    return Status::Ok;
}

Status __stdcall SaveToFile(
    const BitmapData* input,
    const EncoderOptions* options,
    const EncoderMetadata* metadata,
    const CICPColorData* colorData,
    IOCallbacks* callbacks,
    const ProgressProc progress)
{
    if (!input || !options || !metadata || !colorData || !callbacks)
    {
        return Status::NullParameter;
    }

    try
    {
        ScopedHeifContext context(heif_context_alloc());

        Status status = HeicEncoder::Encode(context.get(), input, options, metadata, *colorData, progress);

        if (status == Status::Ok)
        {
            status = HeicWriter::SaveToFile(context.get(), callbacks, progress);
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

size_t __stdcall GetLibDe265VersionString(char* buffer, size_t length)
{
    size_t result = 0;

    const heif_decoder_descriptor* libde265Descriptor[1];

    if (heif_get_decoder_descriptors(heif_compression_HEVC, libde265Descriptor, 1) == 1)
    {
        const char* const name = heif_decoder_descriptor_get_name(*libde265Descriptor);

        if (name)
        {
            const size_t stringLength = std::char_traits<char>::length(name);

            if (buffer)
            {
                if (length >= stringLength)
                {
                    memcpy_s(buffer, length, name, stringLength);
                    result = std::min(length, stringLength);
                }
            }
            else
            {
                result = stringLength;
            }
        }
    }

    return result;
}

size_t __stdcall GetLibHeifVersionString(char* buffer, size_t length)
{
    constexpr size_t stringLength = std::char_traits<char>::length(LIBHEIF_VERSION);

    if (buffer)
    {
        memcpy_s(buffer, length, LIBHEIF_VERSION, stringLength);
    }

    return stringLength;
}

size_t __stdcall GetX265VersionString(char* buffer, size_t length)
{
    size_t result = 0;

    const heif_encoder_descriptor* x265Descriptor[1];

    if (heif_get_encoder_descriptors(heif_compression_HEVC, nullptr, x265Descriptor, 1) == 1)
    {
        const char* const name = heif_encoder_descriptor_get_name(*x265Descriptor);

        if (name)
        {
            const size_t stringLength = std::char_traits<char>::length(name);

            if (buffer)
            {
                if (length >= stringLength)
                {
                    memcpy_s(buffer, length, name, stringLength);
                    result = std::min(length, stringLength);
                }
            }
            else
            {
                result = stringLength;
            }
        }
    }

    return result;
}


