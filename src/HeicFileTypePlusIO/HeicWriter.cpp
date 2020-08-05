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

#include "HeicWriter.h"
#include "ProgressSteps.h"

namespace
{
    heif_error Write(heif_context* ctx,
        const void* data,
        size_t size,
        void* userdata)
    {
        static heif_error Success = { heif_error_Ok, heif_suberror_Unspecified, "Success" };
        static heif_error WriteError = { heif_error_Encoding_error, heif_suberror_Cannot_write_output_data, "Write error" };

        const IOCallbacks* callbacks = static_cast<IOCallbacks*>(userdata);

        return callbacks->Write(data, size) == 0 ? Success : WriteError;
    }
}

Status HeicWriter::SaveToFile(heif_context* const context, IOCallbacks* const callbacks, const ProgressProc progressCallback)
{
    if (!context || !callbacks)
    {
        return Status::NullParameter;
    }

    static heif_writer writer = { 1, Write };

    heif_error error = heif_context_write(context, &writer, callbacks);

    if (error.code != heif_error_Ok)
    {
        switch (error.code)
        {
        case heif_error_Memory_allocation_error:
            return Status::OutOfMemory;
        default:
            return Status::WriteError;
        }
    }

    if (progressCallback)
    {
        if (!progressCallback(AfterFileWrite))
        {
            return Status::UserCanceled;
        }
    }

    return Status::Ok;
}
