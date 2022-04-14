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

#include "HeicMetadata.h"
#include <vector>


Status AddExifToImage(heif_context* const context, heif_image_handle* const image, const uint8_t* exif, int exifSize)
{
    if (!context || !image)
    {
        return Status::NullParameter;
    }

    if (exif && exifSize)
    {
        heif_error error = heif_context_add_exif_metadata(context, image,  exif, exifSize);

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
    }

    return Status::Ok;
}

Status AddICCProfileToImage(heif_image* const image, const uint8_t* profile, int profileSize)
{
    if (!image)
    {
        return Status::NullParameter;
    }

    if (profile && profileSize)
    {
        heif_error error = heif_image_set_raw_color_profile(image, "prof", profile, static_cast<size_t>(profileSize));

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
    }

    return Status::Ok;
}

Status AddNclxProfileToImage(heif_image* const image, const CICPColorData& cicp)
{
    if (!image)
    {
        return Status::NullParameter;
    }


    heif_color_profile_nclx profile = { 1,
                                        cicp.colorPrimaries,
                                        cicp.transferCharacteristics,
                                        cicp.matrixCoefficients,
                                        cicp.fullRange };

    heif_error error = heif_image_set_nclx_color_profile(image, &profile);

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


Status AddXmpToImage(heif_context* const context, heif_image_handle* const image, const uint8_t* xmp, int xmpSize)
{
    if (!context || !image)
    {
        return Status::NullParameter;
    }

    if (xmp && xmpSize)
    {
        heif_error error = heif_context_add_XMP_metadata(context, image, xmp, xmpSize);

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
    }

    return Status::Ok;
}

Status GetExifMetadataID(heif_image_handle* const handle, heif_item_id* exifId)
{
    if (!handle || !exifId)
    {
        return Status::NullParameter;
    }

    heif_item_id id;

    if (heif_image_handle_get_list_of_metadata_block_IDs(handle, "Exif", &id, 1) == 1)
    {
        *exifId = id;
        return Status::Ok;
    }

    return Status::NoMatchingMetadata;
}

Status GetXmpMetadataID(heif_image_handle* const handle, heif_item_id* xmpId)
{
    if (!handle || !xmpId)
    {
        return Status::NullParameter;
    }

    int mimeBlockCount = heif_image_handle_get_number_of_metadata_blocks(handle, "mime");

    if (mimeBlockCount == 0)
    {
        return Status::NoMatchingMetadata;
    }

    try
    {
        std::vector<heif_item_id> ids(mimeBlockCount);

        if (heif_image_handle_get_list_of_metadata_block_IDs(handle, "mime", ids.data(), mimeBlockCount) == mimeBlockCount)
        {
            for (size_t i = 0; i < ids.size(); i++)
            {
                const heif_item_id id = ids[i];
                const char* contentType = heif_image_handle_get_metadata_content_type(handle, id);

                if (strcmp(contentType, "application/rdf+xml") == 0)
                {
                    *xmpId = id;
                    return Status::Ok;
                }
            }
        }
    }
    catch (const std::bad_alloc&)
    {
        return Status::OutOfMemory;
    }
    catch (...)
    {
        return Status::MetadataError;
    }

    return Status::NoMatchingMetadata;
}

bool HasExifMetadata(heif_image_handle* const handle)
{
    return heif_image_handle_get_number_of_metadata_blocks(handle, "Exif") > 0;
}

bool HasXmpMetadata(heif_image_handle* const handle)
{
    int mimeBlockCount = heif_image_handle_get_number_of_metadata_blocks(handle, "mime");

    if (mimeBlockCount == 0)
    {
        return false;
    }

    try
    {
        std::vector<heif_item_id> ids(mimeBlockCount);

        if (heif_image_handle_get_list_of_metadata_block_IDs(handle, "mime", ids.data(), mimeBlockCount) == mimeBlockCount)
        {
            for (size_t i = 0; i < ids.size(); i++)
            {
                const char* contentType = heif_image_handle_get_metadata_content_type(handle, ids[i]);

                if (strcmp(contentType, "application/rdf+xml") == 0)
                {
                    return true;
                }
            }
        }
    }
    catch (...)
    {
        // Ignore any errors that std::vector throws.
        return false;
    }

    return false;
}
