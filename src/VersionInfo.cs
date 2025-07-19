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

using System;
using System.Text;

#nullable enable

namespace HeicFileTypePlus
{
    internal static class VersionInfo
    {
        private static readonly Lazy<string> libHeifVersion = new(GetLibHeifVersionString);
        private static readonly Lazy<string> pluginVersion = new(GetPluginVersion);

        private unsafe delegate nuint NativeStringFunction(byte* buffer, nuint length);

        public static string LibHeifVersion => libHeifVersion.Value;

        public static string PluginVersion => pluginVersion.Value;

        private static unsafe string GetLibHeifVersionString()
        {
            string data = string.Empty;

            string libheifVersion = GetVersionData(HeicNative.GetLiHeifVersionString);
            string libde265Version = GetVersionData(HeicNative.GetLibDe265VersionString);
            string x265Version = GetVersionData(HeicNative.GetX265VersionString);

            if (!string.IsNullOrEmpty(libheifVersion))
            {
                data = "libheif v" + libheifVersion;

                if (!string.IsNullOrEmpty(libde265Version))
                {
                    data += "\n";
                    data += libde265Version;
                }

                if (!string.IsNullOrEmpty(x265Version))
                {
                    data += "\n";
                    data += x265Version;
                }
            }

            return data;
        }

        private static string GetPluginVersion()
        {
            string version = typeof(VersionInfo).Assembly.GetName().Version!.ToString();

            return "HeicFileTypePlus v" + version;
        }

        private static unsafe string GetVersionData(NativeStringFunction nativeFunction)
        {
            string result = string.Empty;

            nuint requiredLength = nativeFunction(null, 0);

            if (requiredLength > 0)
            {
                if (requiredLength < 256)
                {
                    byte* buffer = stackalloc byte[(int)requiredLength];

                    nuint stringLength = nativeFunction(buffer, requiredLength);

                    result = Encoding.ASCII.GetString(buffer, (int)stringLength);
                }
                else if (requiredLength < int.MaxValue)
                {
                    byte[] buffer = new byte[(int)requiredLength];

                    fixed (byte* ptr = buffer)
                    {
                        nuint stringLength = nativeFunction(ptr, requiredLength);
                        result = Encoding.ASCII.GetString(ptr, (int)stringLength);
                    }
                }
            }

            return result;
        }
    }
}
