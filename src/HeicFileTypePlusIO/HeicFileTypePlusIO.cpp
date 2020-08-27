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

// HeicFileTypePlusIO.cpp : Defines the exported functions for the DLL.
//

#include "HeicFileTypePlusIO.h"
#include "HeicDecoder.h"
#include "HeicEncoder.h"
#include "HeicMetadata.h"
#include "HeicReader.h"
#include "HeicWriter.h"
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

Status __stdcall LoadFileIntoContext(heif_context* context, IOCallbacks* callbacks)
{
    return HeicReader::LoadFileIntoContext(context, callbacks);
}

Status __stdcall GetPrimaryImage(heif_context* context, heif_image_handle** primaryImageHandle, PrimaryImageInfo* info)
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
            return Status::UnsupportedFeature;
        case heif_error_Unsupported_filetype:
            return Status::UnsupportedFormat;
        case heif_error_Invalid_input:
        default:
            return Status::InvalidFile;
        }
    }

    info->width = heif_image_handle_get_width(*primaryImageHandle);
    info->height = heif_image_handle_get_height(*primaryImageHandle);

    switch (heif_image_handle_get_color_profile_type(*primaryImageHandle))
    {
    case heif_color_profile_type_nclx:
        info->colorProfileType = ColorProfileType::CICP;
        break;
    case heif_color_profile_type_prof:
    case heif_color_profile_type_rICC:
        info->colorProfileType = ColorProfileType::ICC;
        break;
    case heif_color_profile_type_not_present:
    default:
        info->colorProfileType = ColorProfileType::None;
        break;
    }

    info->hasExif = HasExifMetadata(*primaryImageHandle);
    info->hasXmp = HasXmpMetadata(*primaryImageHandle);

    return Status::Ok;
}

Status __stdcall DecodeImage(heif_image_handle* imageHandle, BitmapData* output)
{
    return HeicDecoder::Decode(imageHandle, output);
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

    data->colorPrimaries = nclxProfile->color_primaries;
    data->transferCharacteristics = nclxProfile->transfer_characteristics;
    data->matrixCoefficients = nclxProfile->matrix_coefficients;
    data->fullRange = nclxProfile->full_range_flag;

    heif_nclx_color_profile_free(nclxProfile);

    return Status::Ok;
}

Status __stdcall GetMetadataSize(heif_image_handle* imageHandle, MetadataType type, size_t* size)
{
    if (!imageHandle || !size)
    {
        return Status::NullParameter;
    }

    Status status;
    heif_item_id id;

    switch (type)
    {
    case MetadataType::Exif:
        status = GetExifMetadataID(imageHandle, &id);
        break;
    case MetadataType::Xmp:
        status = GetXmpMetadataID(imageHandle, &id);
        break;
    default:
        return Status::InvalidParameter;
    }

    if (status == Status::Ok)
    {
        *size = heif_image_handle_get_metadata_size(imageHandle, id);
    }
    else
    {
        *size = 0;
    }

    return Status::Ok;
}

Status __stdcall GetMetadata(heif_image_handle* imageHandle, MetadataType type, uint8_t* buffer, size_t bufferSize)
{
    if (!imageHandle || !buffer)
    {
        return Status::NullParameter;
    }

    Status status;
    heif_item_id id;

    switch (type)
    {
    case MetadataType::Exif:
        status = GetExifMetadataID(imageHandle, &id);
        break;
    case MetadataType::Xmp:
        status = GetXmpMetadataID(imageHandle, &id);
        break;
    default:
        return Status::InvalidParameter;
    }

    if (status == Status::Ok)
    {
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
                status = Status::OutOfMemory;
                break;
            default:
                status = Status::MetadataError;
                break;
            }
        }
    }

    return status;
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


