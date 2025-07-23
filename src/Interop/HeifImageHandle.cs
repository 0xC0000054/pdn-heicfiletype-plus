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

using PaintDotNet;
using System;

namespace HeicFileTypePlus.Interop
{
    internal sealed class HeifImageHandle : Disposable, IHeifImageHandle
    {
        private readonly SafeHeifImageHandle safeHeifImageHandle;
        private readonly ImageHandleInfo info;
        private readonly Lazy<CICPColorData> lazyCICPColorData;
        private readonly Lazy<HDRFormat> lazyHDRFormat;

        public HeifImageHandle(SafeHeifImageHandle safeHeifImageHandle, ImageHandleInfo info)
        {
            this.safeHeifImageHandle = safeHeifImageHandle ?? throw new ArgumentNullException(nameof(safeHeifImageHandle));
            this.info = info ?? throw new ArgumentNullException(nameof(info));
            this.lazyCICPColorData = new Lazy<CICPColorData>(CacheCICPColorData);
            this.lazyHDRFormat = new Lazy<HDRFormat>(CacheHDRFormat);
        }

        public int Width => this.info.width;

        public int Height => this.info.height;

        public int BitDepth => this.info.bitDepth;

        public CICPColorData CICPColorData => this.lazyCICPColorData.Value;

        public ImageHandleColorProfileType ColorProfileType => this.info.colorProfileType;

        public HDRFormat HDRFormat => this.lazyHDRFormat.Value;

        public bool HasAlphaChannel => this.info.hasAlphaChannel;

        public bool IsAlphaChannelPremultiplied => this.info.isAlphaChannelPremultiplied;

        public SafeHeifImageHandle SafeHeifImageHandle
        {
            get
            {
                ObjectDisposedException.ThrowIf(this.IsDisposed, this);

                return this.safeHeifImageHandle;
            }
        }

        public HeifImage Decode(HeifColorSpace colorSpace, HeifChroma chroma)
        {
            ObjectDisposedException.ThrowIf(this.IsDisposed, this);

            return HeicNative.DecodeImage(this, colorSpace, chroma);
        }

        public byte[]? GetExif()
        {
            return TryGetMetadata(MetadataType.Exif);
        }

        public byte[]? GetIccProfile()
        {
            byte[]? profileBytes = null;

            if (!this.IsDisposed && this.ColorProfileType == ImageHandleColorProfileType.Icc)
            {
                nuint size = HeicNative.GetICCProfileSize(this.safeHeifImageHandle);

                if (size > 0 && size <= int.MaxValue)
                {
                    profileBytes = new byte[size];

                    HeicNative.GetICCProfile(this.safeHeifImageHandle, profileBytes);
                }
            }

            return profileBytes;
        }

        public byte[]? GetXmp()
        {
            return TryGetMetadata(MetadataType.Xmp);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.safeHeifImageHandle.Dispose();
            }

            base.Dispose(disposing);
        }

        private CICPColorData CacheCICPColorData()
        {
            CICPColorData colorData;

            if (this.ColorProfileType == ImageHandleColorProfileType.Cicp)
            {
                HeicNative.GetCICPColorData(this.safeHeifImageHandle, out colorData);
            }
            else
            {
                // Return a default value if the image does not have CICP color data.
                colorData = new()
                {
                    colorPrimaries = CICPColorPrimaries.Unspecified,
                    transferCharacteristics = CICPTransferCharacteristics.Unspecified,
                    matrixCoefficients = CICPMatrixCoefficients.Unspecified,
                    fullRange = false,
                };
            }

            return colorData;
        }

        private HDRFormat CacheHDRFormat()
        {
            HDRFormat hdrFormat = HDRFormat.None;

            if (this.ColorProfileType == ImageHandleColorProfileType.Cicp)
            {
                CICPColorData colorData = this.CICPColorData;

                if (colorData.colorPrimaries == CICPColorPrimaries.BT2020
                    && colorData.transferCharacteristics == CICPTransferCharacteristics.Smpte2084)
                {
                    hdrFormat = HDRFormat.PQ;
                }
            }

            return hdrFormat;
        }

        private byte[]? TryGetMetadata(MetadataType metadataType)
        {
            byte[]? data = null;

            if (!this.IsDisposed)
            {
                if (HeicNative.GetMetadataId(this.safeHeifImageHandle, metadataType, out uint id))
                {
                    nuint size = HeicNative.GetMetadataSize(this.safeHeifImageHandle, id);

                    if (size > 0 && size <= int.MaxValue)
                    {
                        data = new byte[size];

                        HeicNative.GetMetadata(this.safeHeifImageHandle, id, data);
                    }
                }
            }

            return data;
        }

    }
}
