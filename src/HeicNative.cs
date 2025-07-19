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

using HeicFileTypePlus.Interop;
using PaintDotNet;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace HeicFileTypePlus
{
    internal static class HeicNative
    {
        internal static SafeHeifContext CreateContext()
        {
            SafeHeifContext context;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                context = HeicIO_x64.CreateContext();
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                context = HeicIO_ARM64.CreateContext();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (context is null || context.IsInvalid)
            {
                throw new InvalidOperationException("Unable to create the HEIC file context.");
            }

            return context;
        }

        internal static unsafe void LoadFileIntoContext(SafeHeifContext context, HeifFileIO fileIO)
        {
            Status status;

            HeicErrorDetails errorDetails = new();
            HeicErrorDetailsCopy copyErrorDetailsCallback = new(errorDetails.Copy);

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                status = HeicIO_x64.LoadFileIntoContext(context,
                                                        fileIO.IOCallbacksHandle,
                                                        copyErrorDetailsCallback);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                status = HeicIO_ARM64.LoadFileIntoContext(context,
                                                          fileIO.IOCallbacksHandle,
                                                          copyErrorDetailsCallback);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            GC.KeepAlive(copyErrorDetailsCallback);

            if (status != Status.Ok)
            {
                if (fileIO.CallbackExceptionInfo != null)
                {
                    fileIO.CallbackExceptionInfo.Throw();
                }
                else
                {
                    HandleReadError(status, errorDetails.Message);
                }
            }
        }

        internal static unsafe HeifImageHandle GetPrimaryImage(SafeHeifContext context)
        {
            SafeHeifImageHandle primaryImageHandle = null;
            ImageHandleInfo info = new();

            HeicErrorDetails errorDetails = new();
            HeicErrorDetailsCopy copyErrorDetailsCallback = new(errorDetails.Copy);

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                Status status = HeicIO_x64.GetPrimaryImage(context,
                                                           out SafeHeifImageHandleX64 handle,
                                                           info,
                                                           copyErrorDetailsCallback);

                if (status == Status.Ok)
                {
                    primaryImageHandle = handle;
                }
                else
                {
                    HandleReadError(status, errorDetails.Message);
                }
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                Status status = HeicIO_ARM64.GetPrimaryImage(context,
                                                             out SafeHeifImageHandleARM64 handle,
                                                             info,
                                                             copyErrorDetailsCallback);

                if (status == Status.Ok)
                {
                    primaryImageHandle = handle;
                }
                else
                {
                    HandleReadError(status, errorDetails.Message);
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
            GC.KeepAlive(copyErrorDetailsCallback);

            HeifImageHandle imageHandle = null;

            try
            {
                imageHandle = new(primaryImageHandle, info);
                primaryImageHandle = null;
            }
            finally
            {
                primaryImageHandle?.Dispose();
            }

            return imageHandle;
        }

        internal static unsafe void DecodeImage(SafeHeifImageHandle imageHandle, Surface surface)
        {
            BitmapData bitmapData = new()
            {
                scan0 = (byte*)surface.Scan0.VoidStar,
                width = surface.Width,
                height = surface.Height,
                stride = surface.Stride
            };

            Status status;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                status = HeicIO_x64.DecodeImage(imageHandle, ref bitmapData);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                status = HeicIO_ARM64.DecodeImage(imageHandle, ref bitmapData);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (status != Status.Ok)
            {
                HandleReadError(status);
            }
        }

        internal static nuint GetICCProfileSize(SafeHeifImageHandle imageHandle)
        {
            nuint size;
            Status status;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                status = HeicIO_x64.GetICCProfileSize(imageHandle, out size);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                status = HeicIO_ARM64.GetICCProfileSize(imageHandle, out size);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (status != Status.Ok)
            {
                HandleReadError(status);
            }

            return size;
        }

        internal static void GetICCProfile(SafeHeifImageHandle imageHandle, byte[] buffer)
        {
            Status status;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                status = HeicIO_x64.GetICCProfile(imageHandle, buffer, (uint)buffer.Length);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                status = HeicIO_ARM64.GetICCProfile(imageHandle, buffer, (uint)buffer.Length);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (status != Status.Ok)
            {
                HandleReadError(status);
            }
        }

        internal static bool GetMetadataId(SafeHeifImageHandle imageHandle, MetadataType type, out uint id)
        {
            bool result = false;
            Status status;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                status = HeicIO_x64.GetMetadataId(imageHandle, type, out id);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                status = HeicIO_ARM64.GetMetadataId(imageHandle, type, out id);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (status == Status.Ok)
            {
                result = true;
            }
            else if (status != Status.NoMatchingMetadata)
            {
                HandleReadError(status);
            }

            return result;
        }

        internal static nuint GetMetadataSize(SafeHeifImageHandle imageHandle, uint id)
        {
            nuint size;
            Status status;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                status = HeicIO_x64.GetMetadataSize(imageHandle, id, out size);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                status = HeicIO_ARM64.GetMetadataSize(imageHandle, id, out size);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (status != Status.Ok)
            {
                HandleReadError(status);
            }

            return size;
        }

        internal static void GetMetadata(SafeHeifImageHandle imageHandle, uint id, byte[] buffer)
        {
            Status status;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                status = HeicIO_x64.GetMetadata(imageHandle, id, buffer, (uint)buffer.Length);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                status = HeicIO_ARM64.GetMetadata(imageHandle, id, buffer, (uint)buffer.Length);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (status != Status.Ok)
            {
                HandleReadError(status);
            }
        }

        internal static unsafe void SaveToFile(Surface surface,
                                               EncoderOptions options,
                                               EncoderMetadata metadata,
                                               ref CICPColorData colorData,
                                               HeifFileIO fileIO,
                                               HeifProgressCallback progressCallback)
        {
            BitmapData bitmapData = new()
            {
                scan0 = (byte*)surface.Scan0.VoidStar,
                width = surface.Width,
                height = surface.Height,
                stride = surface.Stride
            };

            Status status;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                status = HeicIO_x64.SaveToFile(ref bitmapData, options, metadata, ref colorData, fileIO.IOCallbacksHandle, progressCallback);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                status = HeicIO_ARM64.SaveToFile(ref bitmapData, options, metadata, ref colorData, fileIO.IOCallbacksHandle, progressCallback);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (status != Status.Ok)
            {
                if (fileIO.CallbackExceptionInfo != null)
                {
                    fileIO.CallbackExceptionInfo.Throw();
                }
                else
                {
                    HandleWriteError(status);
                }
            }
        }

        internal static unsafe nuint GetLibDe265VersionString(byte* buffer, nuint length)
        {
            nuint result;
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                result = HeicIO_x64.GetLibDe265VersionString(buffer, length);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                result = HeicIO_ARM64.GetLibDe265VersionString(buffer, length);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return result;
        }

        internal static unsafe nuint GetLiHeifVersionString(byte* buffer, nuint length)
        {
            nuint result;
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                result = HeicIO_x64.GetLibHeifVersionString(buffer, length);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                result = HeicIO_ARM64.GetLibHeifVersionString(buffer, length);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return result;
        }
        internal static unsafe nuint GetX265VersionString(byte* buffer, nuint length)
        {
            nuint result;
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                result = HeicIO_x64.GetX265VersionString(buffer, length);
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                result = HeicIO_ARM64.GetX265VersionString(buffer, length);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return result;
        }

        private static void HandleReadError(Status status, string errorDetails = null)
        {
            switch (status)
            {
                case Status.Ok:
                    break;
                case Status.NullParameter:
                    throw new InvalidOperationException("A required native API parameter was null.");
                case Status.OutOfMemory:
                    throw new OutOfMemoryException();
                case Status.InvalidFile:
                    if (!string.IsNullOrWhiteSpace(errorDetails))
                    {
                        throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                                                                "The HEIC file is invalid.\nDetails: {0}",
                                                                errorDetails));
                    }
                    else
                    {
                        throw new FormatException("The HEIC file is invalid.");
                    }
                case Status.UnsupportedFeature:
                    if (!string.IsNullOrWhiteSpace(errorDetails))
                    {
                        throw new FormatException(string.Format(CultureInfo.CurrentCulture,
                                                                "The file uses features that are not supported.\nDetails: {0}",
                                                                errorDetails));
                    }
                    else
                    {
                        throw new FormatException("The file uses features that are not supported.");
                    }
                case Status.UnsupportedFormat:
                    throw new FormatException("The file uses a format that is not supported.");
                case Status.DecodeFailed:
                    throw new FormatException("Unable to decode the HEIC image.");
                case Status.BufferTooSmall:
                    throw new InvalidOperationException("A native API buffer was too small.");
                case Status.ColorInformationError:
                    throw new FormatException("Unable to get the image color information.");
                case Status.NoMatchingMetadata:
                    // Ignored.
                    break;
                case Status.MetadataError:
                    throw new FormatException("Unable to get the image metadata.");
                case Status.NoFtypBox:
                    throw new NoFtypeBoxException("The HEIC file is invalid: No 'ftyp' box.");
                case Status.UnknownError:
                default:
                    throw new FormatException("An unknown error occurred when loading the image.");
            }
        }

        private static void HandleWriteError(Status status)
        {
            switch (status)
            {
                case Status.Ok:
                    break;
                case Status.NullParameter:
                    throw new InvalidOperationException("A required native API parameter was null.");
                case Status.OutOfMemory:
                    throw new OutOfMemoryException();
                case Status.EncodeFailed:
                    throw new FormatException("Unable to encode the HEIC image.");
                case Status.BufferTooSmall:
                    throw new InvalidOperationException("A native API buffer was too small.");
                case Status.UnknownYUVFormat:
                    throw new FormatException("The YUV format is not supported by the encoder.");
                case Status.WriteError:
                    throw new IOException("An error occurred when writing the file.");
                case Status.ColorInformationError:
                    throw new FormatException("Unable to set the image color information.");
                case Status.UserCanceled:
                    throw new OperationCanceledException();
                case Status.MetadataError:
                    throw new FormatException("Unable to set the image metadata.");
                case Status.UnknownError:
                default:
                    throw new FormatException("An unknown error occurred when saving the image.");
            }
        }
    }
}
