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

#pragma once

#include "libheif/heif.h"
#include <memory>

struct heif_image_deleter
{
    void operator()(heif_image* p) noexcept
    {
        heif_image_release(p);
    }
};

using ScopedHeifImage = std::unique_ptr<heif_image, heif_image_deleter>;

struct heif_image_handle_deleter
{
    void operator()(heif_image_handle* p) noexcept
    {
        heif_image_handle_release(p);
    }
};

using ScopedHeifImageHandle = std::unique_ptr<heif_image_handle, heif_image_handle_deleter>;

struct heif_decoding_options_deleter
{
    void operator()(heif_decoding_options* p) noexcept
    {
        heif_decoding_options_free(p);
    }
};

using ScopedHeifDecodingOptions = std::unique_ptr<heif_decoding_options, heif_decoding_options_deleter>;

struct heif_context_deleter
{
    void operator()(heif_context* p) noexcept
    {
        heif_context_free(p);
    }
};

using ScopedHeifContext = std::unique_ptr<heif_context, heif_context_deleter>;

struct heif_encoder_deleter
{
    void operator()(heif_encoder* p) noexcept
    {
        heif_encoder_release(p);
    }
};

using ScopedHeifEncoder = std::unique_ptr<heif_encoder, heif_encoder_deleter>;

