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

#include "HeicReader.h"
#include <stdexcept>

namespace
{
    int64_t get_position(void* userdata)
    {
        const IOCallbacks* callbacks = static_cast<IOCallbacks*>(userdata);

        return callbacks->GetPosition();
    }

    int read(void* data, size_t size, void* userdata)
    {
        const IOCallbacks* callbacks = static_cast<IOCallbacks*>(userdata);

        return callbacks->Read(data, size);
    }

    int seek(int64_t position, void* userdata)
    {
        const IOCallbacks* callbacks = static_cast<IOCallbacks*>(userdata);

        return callbacks->Seek(position);
    }

    heif_reader_grow_status wait_for_file_size(int64_t target_size, void* userdata)
    {
        const IOCallbacks* callbacks = static_cast<IOCallbacks*>(userdata);
        const int64_t length = callbacks->GetSize();

        return target_size > length ? heif_reader_grow_status_size_beyond_eof : heif_reader_grow_status_size_reached;
    }
}

Status HeicReader::LoadFileIntoContext(
    heif_context* const context,
    IOCallbacks* const callbacks,
    const CopyErrorDetails copyErrorDetails)
{
    if (!context || !callbacks)
    {
        return Status::NullParameter;
    }

    Status status = Status::Ok;

    static heif_reader reader = { 1, get_position, read, seek, wait_for_file_size };

    try
    {
        const heif_error error = heif_context_read_from_reader(context, &reader, callbacks, nullptr);

        if (error.code != heif_error_Ok)
        {
            switch (error.code)
            {
            case heif_error_Memory_allocation_error:
                status = Status::OutOfMemory;
                break;
            case heif_error_Unsupported_feature:
                if (copyErrorDetails)
                {
                    copyErrorDetails(error.message);
                }
                status = Status::UnsupportedFeature;
                break;
            case heif_error_Unsupported_filetype:
                status = Status::UnsupportedFormat;
                break;
            case heif_error_Invalid_input:
                if (error.subcode == heif_suberror_No_ftyp_box)
                {
                    status = Status::NoFtypBox;
                    break;
                }
                [[fallthrough]];
            default:
                if (copyErrorDetails)
                {
                    copyErrorDetails(error.message);
                }
                status = Status::InvalidFile;
                break;
            }
        }
    }
    catch (const std::bad_alloc&)
    {
        return Status::OutOfMemory;
    }
    catch (...)
    {
        return Status::UnknownError;
    }

    return status;
}

