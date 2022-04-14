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

#pragma once

#include "HeicFileTypePlusIO.h"

Status AddExifToImage(
    heif_context* const context,
    heif_image_handle* const image,
    const uint8_t* exif,
    int exifSize);

Status AddICCProfileToImage(heif_image* const image, const uint8_t* profile, int profileSize);

Status AddNclxProfileToImage(heif_image* const image, const CICPColorData& cicp);

Status AddXmpToImage(
    heif_context* const context,
    heif_image_handle* const image,
    const uint8_t* xmp,
    int xmpSize);

Status GetExifMetadataID(heif_image_handle* const handle, heif_item_id* exifId);

Status GetXmpMetadataID(heif_image_handle* const handle, heif_item_id* xmpId);

bool HasExifMetadata(heif_image_handle* const handle);

bool HasXmpMetadata(heif_image_handle* const handle);
