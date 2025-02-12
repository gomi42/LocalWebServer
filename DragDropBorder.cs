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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WebServer
{
    // Alternatively its better to implement an attached property that handles the drop event.
    // I started that way and it worked fine. But for some unknown reason the visual designer
    // didn't show the preview anymore, complaining about the attached property. That's why
    // I took this way.
    public class DragDropBorder : Border
    {
        public DragDropBorder()
        {
            AllowDrop = true;
            DragOver += OnDragOver;
            Drop += OnDrop;
        }

        private void OnDragOver(object sender, DragEventArgs args)
        {
            string[] files = (string[])args.Data.GetData(DataFormats.FileDrop);

            args.Effects = files.Length == 1 ? DragDropEffects.Move : DragDropEffects.None;
            args.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs args)
        {
            DropCommand?.Execute(args.Data);
            args.Handled = true;
        }

        public ICommand DropCommand
        {
            get => (ICommand)GetValue(DropCommandProperty);
            set => SetValue(DropCommandProperty, value);
        }

        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.Register("DropCommand", typeof(ICommand), typeof(DragDropBorder), new PropertyMetadata(null));
    }
}
