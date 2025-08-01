﻿// This file is part of pdn-heicfiletype-plus, a libheif-based HEIC
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
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.IO;

namespace HeicFileTypePlus
{
    public sealed class HeicFileTypePlusFactory : IFileTypeFactory2
    {
        public FileType[] GetFileTypeInstances(IFileTypeHost host)
        {
            return new[] { new HeicFileTypePlusPlugin(host) };
        }
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    internal sealed class HeicFileTypePlusPlugin : PropertyBasedFileType
    {
        private static readonly string[] FileExtensions = [".heic", ".heif", ".hif"];

        private readonly IServiceProvider serviceProvider;

        internal HeicFileTypePlusPlugin(IFileTypeHost host)
            : base(
                "HEIC",
                new FileTypeOptions
                {
                    LoadExtensions = FileExtensions,
                    SaveExtensions = FileExtensions,
                    SupportsCancellation = true,
                    SupportsLayers = false
                })
        {
            this.serviceProvider = host.Services;
        }

        private enum PropertyNames
        {
            Quality,
            Preset,
            Tuning,
            TUIntraDepth,
            YUVChromaSubsampling,
            ForumLink,
            GitHubLink,
            PluginVersion,
            LibHeifVersion
        }

        /// <summary>
        /// Add properties to the dialog
        /// </summary>
        public override PropertyCollection OnCreateSavePropertyCollection()
        {
            Property[] props = new Property[]
            {
                new Int32Property(PropertyNames.Quality, 90, 0, 100, false),
                CreateChromaSubsampling(),
                StaticListChoiceProperty.CreateForEnum(PropertyNames.Preset, EncoderPreset.Medium),
                CreateTuning(),
                new Int32Property(PropertyNames.TUIntraDepth, 1, 1, 4, false),
                new UriProperty(PropertyNames.ForumLink, new Uri("https://forums.getpaint.net/topic/116873-heic-filetype-plus/")),
                new UriProperty(PropertyNames.GitHubLink, new Uri("https://github.com/0xC0000054/pdn-heicfiletype-plus")),
                new StringProperty(PropertyNames.PluginVersion),
                new StringProperty(PropertyNames.LibHeifVersion),
            };

            return new PropertyCollection(props);

            static StaticListChoiceProperty CreateChromaSubsampling()
            {
                // The list is created manually because some of the YUVChromaSubsampling enumeration values
                // are used for internal signaling.

                object[] valueChoices = new object[]
                {
                    YUVChromaSubsampling.Subsampling420,
                    YUVChromaSubsampling.Subsampling422,
                    YUVChromaSubsampling.Subsampling444
                };

                int defaultChoiceIndex = Array.IndexOf(valueChoices, YUVChromaSubsampling.Subsampling422);

                return new StaticListChoiceProperty(PropertyNames.YUVChromaSubsampling, valueChoices, defaultChoiceIndex);
            }

            static StaticListChoiceProperty CreateTuning()
            {
                // The list is created manually so that the None value appears first.

                object[] valueChoices = new object[]
                {
                    EncoderTuning.None,
                    EncoderTuning.PSNR,
                    EncoderTuning.SSIM,
                    EncoderTuning.FilmGrain,
                    EncoderTuning.FastDecode,
                };

                int defaultChoiceIndex = 0;

                return new StaticListChoiceProperty(PropertyNames.Tuning, valueChoices, defaultChoiceIndex);
            }
        }

        /// <summary>
        /// Adapt properties in the dialog (DisplayName, ...)
        /// </summary>
        public override ControlInfo OnCreateSaveConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultSaveConfigUI(props);

            PropertyControlInfo chromaSubsamplingInfo = configUI.FindControlForPropertyName(PropertyNames.YUVChromaSubsampling)!;
            chromaSubsamplingInfo.ControlProperties[ControlInfoPropertyNames.DisplayName]!.Value = "Chroma Subsampling";
            chromaSubsamplingInfo.SetValueDisplayName(YUVChromaSubsampling.Subsampling420, "4:2:0 (Best Compression)");
            chromaSubsamplingInfo.SetValueDisplayName(YUVChromaSubsampling.Subsampling422, "4:2:2");
            chromaSubsamplingInfo.SetValueDisplayName(YUVChromaSubsampling.Subsampling444, "4:4:4 (Best Quality)");

            PropertyControlInfo presetInfo = configUI.FindControlForPropertyName(PropertyNames.Preset)!;
            presetInfo.ControlProperties[ControlInfoPropertyNames.DisplayName]!.Value = "Encoding Speed / Quality";
            presetInfo.SetValueDisplayName(EncoderPreset.SuperFast, "Super Fast");
            presetInfo.SetValueDisplayName(EncoderPreset.VeryFast, "Very Fast");
            presetInfo.SetValueDisplayName(EncoderPreset.Faster, "Faster");
            presetInfo.SetValueDisplayName(EncoderPreset.Fast, "Fast");
            presetInfo.SetValueDisplayName(EncoderPreset.Medium, "Medium");
            presetInfo.SetValueDisplayName(EncoderPreset.Slow, "Slow");
            presetInfo.SetValueDisplayName(EncoderPreset.Slower, "Slower");
            presetInfo.SetValueDisplayName(EncoderPreset.VerySlow, "Very Slow");
            presetInfo.SetValueDisplayName(EncoderPreset.Placebo, "Placebo");

            configUI.SetPropertyControlValue(PropertyNames.Quality, ControlInfoPropertyNames.DisplayName, "Quality");

            PropertyControlInfo tuningInfo = configUI.FindControlForPropertyName(PropertyNames.Tuning)!;
            tuningInfo.ControlProperties[ControlInfoPropertyNames.DisplayName]!.Value = "Tuning";
            tuningInfo.SetValueDisplayName(EncoderTuning.None, "None");
            tuningInfo.SetValueDisplayName(EncoderTuning.PSNR, "PSNR");
            tuningInfo.SetValueDisplayName(EncoderTuning.SSIM, "SSIM");
            tuningInfo.SetValueDisplayName(EncoderTuning.FilmGrain, "Film Grain");
            tuningInfo.SetValueDisplayName(EncoderTuning.FastDecode, "Fast Decode");

            configUI.SetPropertyControlValue(PropertyNames.TUIntraDepth, ControlInfoPropertyNames.DisplayName, "TU Intra Depth");

            PropertyControlInfo forumLinkInfo = configUI.FindControlForPropertyName(PropertyNames.ForumLink)!;
            forumLinkInfo.ControlProperties[ControlInfoPropertyNames.DisplayName]!.Value = "More Info";
            forumLinkInfo.ControlProperties[ControlInfoPropertyNames.Description]!.Value = "Forum Discussion";

            PropertyControlInfo githubLinkInfo = configUI.FindControlForPropertyName(PropertyNames.GitHubLink)!;
            githubLinkInfo.ControlProperties[ControlInfoPropertyNames.DisplayName]!.Value = string.Empty;
            githubLinkInfo.ControlProperties[ControlInfoPropertyNames.Description]!.Value = "GitHub"; // GitHub is a brand name that should not be localized.

            PropertyControlInfo pluginVersionInfo = configUI.FindControlForPropertyName(PropertyNames.PluginVersion)!;
            pluginVersionInfo.ControlType.Value = PropertyControlType.Label;
            pluginVersionInfo.ControlProperties[ControlInfoPropertyNames.DisplayName]!.Value = string.Empty;
            pluginVersionInfo.ControlProperties[ControlInfoPropertyNames.Description]!.Value = VersionInfo.PluginVersion;

            PropertyControlInfo libheifVersionInfo = configUI.FindControlForPropertyName(PropertyNames.LibHeifVersion)!;
            libheifVersionInfo.ControlType.Value = PropertyControlType.Label;
            libheifVersionInfo.ControlProperties[ControlInfoPropertyNames.DisplayName]!.Value = string.Empty;
            libheifVersionInfo.ControlProperties[ControlInfoPropertyNames.Description]!.Value = VersionInfo.LibHeifVersion;

            return configUI;
        }

        /// <summary>
        /// Saves a document to a stream respecting the properties
        /// </summary>
        protected override void OnSaveT(Document input, Stream output, PropertyBasedSaveConfigToken token, Surface scratchSurface, ProgressEventHandler progressCallback)
        {
            int quality = token.GetProperty<Int32Property>(PropertyNames.Quality)!.Value;
            YUVChromaSubsampling chromaSubsampling = (YUVChromaSubsampling)token.GetProperty(PropertyNames.YUVChromaSubsampling)!.Value!;
            EncoderPreset preset = (EncoderPreset)token.GetProperty(PropertyNames.Preset)!.Value!;
            EncoderTuning tuning = (EncoderTuning)token.GetProperty(PropertyNames.Tuning)!.Value!;
            int tuIntraDepth = token.GetProperty<Int32Property>(PropertyNames.TUIntraDepth)!.Value;

            HeicSave.Save(input, output, scratchSurface, quality, chromaSubsampling, preset, tuning, tuIntraDepth, progressCallback);
        }

        /// <summary>
        /// Creates a document from a stream
        /// </summary>
        protected override Document OnLoad(Stream input)
        {
            Document doc;

            long originalStreamPosition = input.Position;

            try
            {
                doc = HeicLoad.Load(input);
            }
            catch (NoFtypeBoxException ex)
            {
                input.Position = originalStreamPosition;

                IFileTypeInfo? fileTypeInfo = FormatDetection.TryGetFileTypeInfo(input, this.serviceProvider);

                if (fileTypeInfo != null)
                {
                    input.Position = originalStreamPosition;
                    doc = fileTypeInfo.GetInstance().Load(input);
                }
                else
                {
                    throw new FormatException(ex.Message);
                }
            }

            return doc;
        }
    }
}
