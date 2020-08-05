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

#ifdef HEICFILETYPEPLUSIO_EXPORTS
#define HEICFILETYPEPLUSIO_API __declspec(dllexport)
#else
#define HEICFILETYPEPLUSIO_API __declspec(dllimport)
#endif

#include <stdint.h>
#include "libheif/heif.h"

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

typedef bool(__stdcall* ProgressProc)(double progress);

enum class Status
{
    Ok,
    NullParameter,
    InvalidParameter,
    OutOfMemory,
    InvalidFile,
    UnsupportedFeature,
    UnsupportedFormat,
    DecodeFailed,
    BufferTooSmall,
    ColorInformationError,
    NoMatchingMetadata,
    MetadataError,
    EncodeFailed,
    UnknownYUVFormat,
    WriteError,
    UserCanceled,
    UnknownError
};

struct IOCallbacks
{
    int32_t(__stdcall* Read)(void* buffer, const size_t count);
    int32_t(__stdcall* Write)(const void* buffer, const size_t count);
    int32_t(__stdcall* Seek)(const int64_t position);
    int64_t(__stdcall* GetPosition)();
    int64_t(__stdcall* GetSize)();
};

enum class ColorProfileType
{
    None,
    CICP,
    ICC
};

enum class YUVChromaSubsampling
{
    Subsampling400,
    Subsampling420,
    Subsampling422,
    Subsampling444
};

struct CICPColorData
{
    heif_color_primaries colorPrimaries;
    heif_transfer_characteristics transferCharacteristics;
    heif_matrix_coefficients matrixCoefficients;
    bool fullRange;
};

struct PrimaryImageInfo
{
    int width;
    int height;
    ColorProfileType colorProfileType;
    bool hasExif;
    bool hasXmp;
};

struct BitmapData
{
    uint8_t* scan0;
    int32_t width;
    int32_t height;
    int32_t stride;
};

struct ColorBgra
{
    uint8_t b;
    uint8_t g;
    uint8_t r;
    uint8_t a;
};

enum class MetadataType
{
    Exif,
    Xmp
};

enum class EncoderPreset
{
    UltraFast = 0,
    SuperFast,
    VeryFast,
    Faster,
    Fast,
    Medium,
    Slow,
    Slower,
    VerySlow,
    Placebo
};

enum class EncoderTuning
{
    PSNR = 0,
    SSIM,
    FilmGrain,
    FastDecode
};

struct EncoderOptions
{
    int quality;
    EncoderPreset preset;
    EncoderTuning tuning;
    int tuIntraDepth;
};

// This must be kept in sync with the NativeEncoderMetadata structure in EncoderMetadataCustomMarshaler.cs.
struct EncoderMetadata
{
    uint8_t* iccProfile;
    uint8_t* exif;
    uint8_t* xmp;
    int32_t iccProfileSize;
    int32_t exifSize;
    int32_t xmpSize;
};

HEICFILETYPEPLUSIO_API heif_context* __stdcall CreateContext();

HEICFILETYPEPLUSIO_API bool __stdcall DeleteContext(heif_context* context);

HEICFILETYPEPLUSIO_API bool __stdcall DeleteImageHandle(heif_image_handle* handle);

HEICFILETYPEPLUSIO_API Status __stdcall LoadFileIntoContext(heif_context* context, IOCallbacks* callbacks);

HEICFILETYPEPLUSIO_API Status __stdcall GetPrimaryImage(
    heif_context* context,
    heif_image_handle** primaryImageHandle,
    PrimaryImageInfo* info);

HEICFILETYPEPLUSIO_API Status __stdcall DecodeImage(heif_image_handle* imageHandle, BitmapData* output);

HEICFILETYPEPLUSIO_API Status __stdcall GetICCProfileSize(heif_image_handle* imageHandle, size_t* size);

HEICFILETYPEPLUSIO_API Status __stdcall GetICCProfile(heif_image_handle* imageHandle, uint8_t* buffer, size_t bufferSize);

HEICFILETYPEPLUSIO_API Status __stdcall GetCICPColorData(heif_image_handle* imageHandle, CICPColorData* data);

HEICFILETYPEPLUSIO_API Status __stdcall GetMetadataSize(heif_image_handle* imageHandle, MetadataType type, size_t* size);

HEICFILETYPEPLUSIO_API Status __stdcall GetMetadata(heif_image_handle* imageHandle, MetadataType type, uint8_t* buffer, size_t bufferSize);

HEICFILETYPEPLUSIO_API Status __stdcall SaveToFile(
    const BitmapData* input,
    const EncoderOptions* options,
    const EncoderMetadata* metadata,
    const CICPColorData* cicp,
    IOCallbacks* callbacks,
    const ProgressProc progress);


#ifdef __cplusplus
}
#endif // __cplusplus
