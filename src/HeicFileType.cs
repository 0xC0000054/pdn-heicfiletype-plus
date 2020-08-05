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

using PaintDotNet;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System.IO;

namespace HeicFileTypePlus
{
    public sealed class HeicFileTypePlusFactory : IFileTypeFactory
    {
        public FileType[] GetFileTypeInstances()
        {
            return new[] { new HeicFileTypePlusPlugin() };
        }
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    internal sealed class HeicFileTypePlusPlugin : PropertyBasedFileType
    {
        internal HeicFileTypePlusPlugin()
            : base(
                "HEIC",
                new FileTypeOptions
                {
                    LoadExtensions = new string[] { ".heic" },
                    SaveExtensions = new string[] { ".heic" },
                    SupportsCancellation = true,
                    SupportsLayers = false
                })
        {
        }

        private enum PropertyNames
        {
            Quality,
            Preset,
            Tuning,
            TUIntraDepth
        }

        /// <summary>
        /// Add properties to the dialog
        /// </summary>
        public override PropertyCollection OnCreateSavePropertyCollection()
        {
            Property[] props = new Property[]
            {
                new Int32Property(PropertyNames.Quality, 90, 0, 100, false),
                StaticListChoiceProperty.CreateForEnum(PropertyNames.Preset, EncoderPreset.Medium),
                StaticListChoiceProperty.CreateForEnum(PropertyNames.Tuning, EncoderTuning.SSIM),
                new Int32Property(PropertyNames.TUIntraDepth, 1, 1, 4, false)
            };

            return new PropertyCollection(props);
        }

        /// <summary>
        /// Adapt properties in the dialog (DisplayName, ...)
        /// </summary>
        public override ControlInfo OnCreateSaveConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultSaveConfigUI(props);

            PropertyControlInfo presetInfo = configUI.FindControlForPropertyName(PropertyNames.Preset);
            presetInfo.ControlProperties[ControlInfoPropertyNames.DisplayName].Value = "Encoding Speed / Quality";
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

            PropertyControlInfo tuningInfo = configUI.FindControlForPropertyName(PropertyNames.Tuning);
            tuningInfo.ControlProperties[ControlInfoPropertyNames.DisplayName].Value = "Tuning";
            tuningInfo.SetValueDisplayName(EncoderTuning.PSNR, "PSNR");
            tuningInfo.SetValueDisplayName(EncoderTuning.SSIM, "SSIM");
            tuningInfo.SetValueDisplayName(EncoderTuning.FilmGrain, "Film Grain");
            tuningInfo.SetValueDisplayName(EncoderTuning.FastDecode, "Fast Decode");

            configUI.SetPropertyControlValue(PropertyNames.TUIntraDepth, ControlInfoPropertyNames.DisplayName, "TU Intra Depth");

            return configUI;
        }

        /// <summary>
        /// Determines if the document was saved without altering the pixel values.
        ///
        /// Any settings that change the pixel values should return 'false'.
        ///
        /// Because Paint.NET prompts the user to flatten the image, flattening should not be
        /// considered.
        /// For example, a 32-bit PNG will return 'true' even if the document has multiple layers.
        /// </summary>
        protected override bool IsReflexive(PropertyBasedSaveConfigToken token)
        {
            // libheif only supports HEVC encoding with YCbCr 4:2:0.
            return false;
        }

        /// <summary>
        /// Saves a document to a stream respecting the properties
        /// </summary>
        protected override void OnSaveT(Document input, Stream output, PropertyBasedSaveConfigToken token, Surface scratchSurface, ProgressEventHandler progressCallback)
        {
            int quality = token.GetProperty<Int32Property>(PropertyNames.Quality).Value;
            EncoderPreset preset = (EncoderPreset)token.GetProperty(PropertyNames.Preset).Value;
            EncoderTuning tuning = (EncoderTuning)token.GetProperty(PropertyNames.Tuning).Value;
            int tuIntraDepth = token.GetProperty<Int32Property>(PropertyNames.TUIntraDepth).Value;

            HeicFile.Save(input, output, scratchSurface, quality, preset, tuning, tuIntraDepth, progressCallback);
        }

        /// <summary>
        /// Creates a document from a stream
        /// </summary>
        protected override Document OnLoad(Stream input)
        {
            return HeicFile.Load(input);
        }
    }
}
