//
// Author:
//   Michael Göricke
//
// Copyright (c) 2025
//
// This file is part of LocalTestServer.
//
// LocalTestServer is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see<http://www.gnu.org/licenses/>.

using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WebServer
{
    /// <summary>
    /// The FolderTextBlock shortens a given full specified folder or file
    /// path so that it fits into the given space (widths) of the control.
    /// The shortening algorithm removes subfolders from the
    /// middle of the path and replaces them with a single "...". E.g.
    /// "d:\aaaa\bbbbb\ccccc\ddddd\eeeee\fffff\gggg"
    /// may be shortened to
    /// "d:\aaaa\bbbbb\...\fffff\gggg" 
    /// </summary>
    public class FolderTextBlock : FrameworkElement
    {
        public static readonly DependencyProperty ForegroundProperty =
                TextElement.ForegroundProperty.AddOwner(typeof(FolderTextBlock),
                        new FrameworkPropertyMetadata(TextElement.ForegroundProperty.DefaultMetadata.DefaultValue,
                                                FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        ////////////////////////////////////////////////////////////////////

        public static readonly DependencyProperty FontFamilyProperty =
                TextElement.FontFamilyProperty.AddOwner(typeof(FolderTextBlock),
                        new FrameworkPropertyMetadata(TextElement.FontFamilyProperty.DefaultMetadata.DefaultValue,
                                                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        ////////////////////////////////////////////////////////////////////

        public static readonly DependencyProperty FontSizeProperty =
                TextElement.FontSizeProperty.AddOwner(typeof(FolderTextBlock),
                        new FrameworkPropertyMetadata(TextElement.FontSizeProperty.DefaultMetadata.DefaultValue,
                                                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        ////////////////////////////////////////////////////////////////////

        public static readonly DependencyProperty FontStretchProperty
            = TextElement.FontStretchProperty.AddOwner(typeof(FolderTextBlock),
                    new FrameworkPropertyMetadata(TextElement.FontStretchProperty.DefaultMetadata.DefaultValue,
                                                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        ////////////////////////////////////////////////////////////////////

        public static readonly DependencyProperty FontStyleProperty =
                TextElement.FontStyleProperty.AddOwner(typeof(FolderTextBlock),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle,
                                                 FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        ////////////////////////////////////////////////////////////////////

        public static readonly DependencyProperty FontWeightProperty =
                TextElement.FontWeightProperty.AddOwner(typeof(FolderTextBlock),
                        new FrameworkPropertyMetadata(TextElement.FontWeightProperty.DefaultMetadata.DefaultValue,
                                                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        ////////////////////////////////////////////////////////////////////

        public static readonly DependencyProperty FolderProperty =
            DependencyProperty.Register("Folder",
                                        typeof(string),
                                        typeof(FolderTextBlock),
                                        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public string Folder
        {
            get => (string)GetValue(FolderProperty);
            set => SetValue(FolderProperty, value);
        }

        ////////////////////////////////////////////////////////////////////

        protected override Size MeasureOverride(Size constraint)
        {
            var formattedText = new FormattedText(Folder,
                                                CultureInfo.CurrentUICulture,
                                                FlowDirection,
                                                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                                                FontSize,
                                                Foreground,
                                                1);

            var width = constraint.Width == double.PositiveInfinity ? formattedText.Width : constraint.Width;
            var height = constraint.Height == double.PositiveInfinity ? formattedText.Height : constraint.Height;

            return new Size(width, height);
        }

        ////////////////////////////////////////////////////////////////////

        protected override void OnRender(DrawingContext ctx)
        {
            var fullFormattedText = new FormattedText(Folder,
                                                CultureInfo.CurrentUICulture,
                                                FlowDirection,
                                                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                                                FontSize,
                                                Foreground,
                                                1);

            if (fullFormattedText.Width < ActualWidth)
            {
                ctx.DrawText(fullFormattedText, new Point(0, 0));
                return;
            }

            var limitedFolderMin = FolderNameHelper.LimitPath(Folder, 1);
            var formattedTextMin = new FormattedText(limitedFolderMin,
                                            CultureInfo.CurrentUICulture,
                                            FlowDirection,
                                            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                                            FontSize,
                                            Foreground,
                                            1);

            if (formattedTextMin.Width > ActualWidth)
            {
                fullFormattedText.MaxTextWidth = ActualWidth;
                fullFormattedText.MaxTextHeight = ActualHeight;
                fullFormattedText.Trimming = TextTrimming.CharacterEllipsis;
                ctx.DrawText(fullFormattedText, new Point(0, 0));
                return;
            }

            int min = limitedFolderMin.Length;
            int max = Folder.Length - 1;
            FormattedText lastFit = null;

            while (max - min > 1)
            {
                var current = (max - min) / 2 + min;
                var limitedFolder = FolderNameHelper.LimitPath(Folder, current);
                var formattedText = new FormattedText(limitedFolder,
                                                CultureInfo.CurrentUICulture,
                                                FlowDirection,
                                                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                                                FontSize,
                                                Foreground,
                                                1);

                if (formattedText.Width > ActualWidth)
                {
                    var len = limitedFolder.Length;
                    max = len;
                }
                else
                {
                    min = current;
                    lastFit = formattedText;
                }
            }

            if (lastFit == null)
            {
                lastFit = fullFormattedText;
                lastFit.MaxTextWidth = ActualWidth;
                lastFit.MaxTextHeight = ActualHeight;
                lastFit.Trimming = TextTrimming.CharacterEllipsis;
            }

            ctx.DrawText(lastFit, new Point(0, 0));
        }
    }
}
