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

using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace WebServer
{
    public partial class MainWindow : Window
    {
        const int MaxTextLength = 50;

        public MainWindow()
        {
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            InitializeComponent();

            ThemeSettings.SetThemeData("pack://application:,,,/Local Test Server;component/",
                                "DarkModeColors.xaml",
                                "LightModeColors.xaml",
                                "Styles.xaml");
            var theme = ThemeSettings.GetWindowsTheme() == WindowsTheme.Dark;
            ThemeButton.IsChecked = theme;
            ThemeSettings.SetTheme(this, theme);
        }

        ////////////////////////////////////////////////////////////////////

        private void ThemeButtonChecked(object sender, RoutedEventArgs e)
        {
            ThemeSettings.SetTheme(this, true);
        }

        ////////////////////////////////////////////////////////////////////

        private void ThemeButtonUnchecked(object sender, RoutedEventArgs e)
        {
            ThemeSettings.SetTheme(this, false);
        }

        ////////////////////////////////////////////////////////////////////
        // https://learn.microsoft.com/en-us/dotnet/api/system.windows.weakeventmanager
        private void ShowMenuClicked(object senderObj, RoutedEventArgs e)
        {
            var sender = (Control)senderObj;

            if ((Forms.Control.ModifierKeys & (Forms.Keys.Shift | Forms.Keys.Control)) == (Forms.Keys.Shift | Forms.Keys.Control))
            {
                ICommand command = GetBoundData<ICommand>(sender.DataContext, "SetFolderCommand");
                command?.Execute(@"D:\WebPages\gomi\export");
                return;
            }

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.PlacementTarget = sender;

            var lruItems = GetBoundData<IReadOnlyList<string>>(sender.DataContext, "LruItems");

            if (lruItems != null)
            {
                foreach (var lruItem in lruItems)
                {
                    var menuItem = new MenuItem();
                    menuItem.Header = FolderNameHelper.LimitPath(lruItem, MaxTextLength);
                    menuItem.Tag = lruItem;
                    WeakEventManager<MenuItem, RoutedEventArgs>.AddHandler(menuItem, nameof(MenuItem.Click), OnItemClicked);
                    contextMenu.Items.Add(menuItem);
                }

                if (lruItems.Count > 0)
                {
                    var sep = new Separator();
                    contextMenu.Items.Add(sep);
                }
            }

            var menuItem2 = new MenuItem();
            menuItem2.Header = Properties.Resources.BrowseFolder;
            WeakEventManager<MenuItem, RoutedEventArgs>.AddHandler(menuItem2, nameof(MenuItem.Click), OnBrowseClicked);
            contextMenu.Items.Add(menuItem2);

            // this statement is not possible in a view model, that's why all the logic appears here
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        ////////////////////////////////////////////////////////////////////

        private void OnItemClicked(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            ICommand command = GetBoundData<ICommand>(item.DataContext, "SetFolderCommand");
            command?.Execute((string)item.Tag);
        }

        ////////////////////////////////////////////////////////////////////

        private void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            ICommand command = GetBoundData<ICommand>(((MenuItem)sender).DataContext, "SelectFolderCommand");
            command?.Execute(null);
        }

        ////////////////////////////////////////////////////////////////////

        private T GetBoundData<T>(object dataContext, string name) where T : class
        {
            PropertyInfo prop = dataContext.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

            if (null == prop)
            {
                return null;
            }

            return prop.GetValue(dataContext) as T;
        }
    }
}