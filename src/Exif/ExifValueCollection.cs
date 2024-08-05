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

using PaintDotNet.Imaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#nullable enable

namespace HeicFileTypePlus.Exif
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ExifValueCollectionDebugView))]
    internal sealed class ExifValueCollection
        : IEnumerable<KeyValuePair<ExifPropertyPath, ExifValue>>
    {
        private readonly Dictionary<ExifPropertyPath, ExifValue> exifMetadata;

        public ExifValueCollection(Dictionary<ExifPropertyPath, ExifValue> items)
        {
            this.exifMetadata = items ?? throw new ArgumentNullException(nameof(items));
        }

        public int Count => this.exifMetadata.Count;

        public void Remove(ExifPropertyPath key) => this.exifMetadata.Remove(key);

        public IEnumerator<KeyValuePair<ExifPropertyPath, ExifValue>> GetEnumerator()
            => this.exifMetadata.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.exifMetadata.GetEnumerator();

        private sealed class ExifValueCollectionDebugView
        {
            private readonly ExifValueCollection collection;

            public ExifValueCollectionDebugView(ExifValueCollection collection)
            {
                this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<ExifPropertyPath, ExifValue>[] Items
            {
                get
                {
                    return this.collection.exifMetadata.ToArray();
                }
            }
        }
    }
}
