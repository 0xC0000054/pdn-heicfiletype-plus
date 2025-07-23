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
    internal sealed class HeifImage : Disposable
    {
        private readonly SafeHeifImage image;
        private readonly HeifImageInfo info;
        private readonly IHeifImageHandle imageHandle;

        public HeifImage(IHeifImageHandle imageHandle, SafeHeifImage image, HeifImageInfo info)
        {
            this.image = image ?? throw new ArgumentNullException(nameof(image));
            this.info = info ?? throw new ArgumentNullException(nameof(info));
            this.imageHandle = imageHandle ?? throw new ArgumentNullException(nameof(imageHandle));
        }

        public HeifColorSpace ColorSpace => this.info.colorSpace;

        public HeifChroma Chroma => this.info.chroma;

        public int Width => this.imageHandle.Width;

        public int Height => this.imageHandle.Height;

        public int BitDepth => this.imageHandle.BitDepth;

        public HDRFormat HDRFormat => this.imageHandle.HDRFormat;

        public bool HasAlphaChannel => this.imageHandle.HasAlphaChannel;

        public bool IsAlphaChannelPremultiplied => this.imageHandle.IsAlphaChannelPremultiplied;

        public unsafe HeifImageChannel GetChannel(HeifChannel channel)
        {
            byte* scan0 = HeicNative.GetHeifImageChannel(this.image, channel, out int stride);

            if (scan0 == null)
            {
                throw new FormatException($"The {channel} channel does not exist in the image.");
            }

            return new(scan0, stride);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.image.Dispose();
            }

            base.Dispose(disposing);
        }

    }
}
