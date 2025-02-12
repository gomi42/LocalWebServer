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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace WebServer
{
    public enum WindowsTheme
    {
        Default = 0,
        Light = 1,
        Dark = 2,
        HighContrast = 3
    }

    internal class ThemeSettings
    {
        // The enum flag for DwmSetWindowAttribute's second parameter, which tells the function what attribute to set.
        // Copied from dwmapi.h
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        // The DWM_WINDOW_CORNER_PREFERENCE enum for DwmSetWindowAttribute's third parameter, which tells the function
        // what value of the enum to set.
        // Copied from dwmapi.h
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        // Import dwmapi.dll and define DwmSetWindowAttribute in C# corresponding to the native function.
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref bool pvAttribute,
                                                         uint cbAttribute);

        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        static string baseUri = "";
        static string darkModeColors = "";
        static string lightModeColors = "";
        static string allStyles = "";

        public static void SetThemeData(string baseUriP, string darkModeColorsP, string lightModeColorsP, string stylesP)
        {
            baseUri = baseUriP;
            darkModeColors = darkModeColorsP;
            lightModeColors = lightModeColorsP;
            allStyles = stylesP;
        }

        public static WindowsTheme GetWindowsTheme()
        {
            var theme = WindowsTheme.Light;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                    {
                        object registryValueObject = key?.GetValue(RegistryValueName);

                        if (registryValueObject == null)
                        {
                            return WindowsTheme.Light;
                        }

                        int registryValue = (int)registryValueObject;

                        if (SystemParameters.HighContrast)
                        {
                            theme = WindowsTheme.HighContrast;
                        }

                        theme = registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
                    }
                }
                catch
                {
                }
            }

            return theme;
        }

        public static void SetTheme(Window window, bool darkTheme)
        {
            Uri CreateUri(string file) => new Uri(baseUri + file);

            var dark = Application.Current.Resources.MergedDictionaries.FirstOrDefault(t => t.Source.OriginalString.Contains(darkModeColors));
            var light = Application.Current.Resources.MergedDictionaries.FirstOrDefault(t => t.Source.OriginalString.Contains(lightModeColors));
            var styles = Application.Current.Resources.MergedDictionaries.FirstOrDefault(t => t.Source.OriginalString.Contains(allStyles));

            if (styles != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(styles);
            }

            if (dark != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(dark);
            }

            if (light != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(light);
            }

            if (darkTheme)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = CreateUri(darkModeColors) });
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = CreateUri(lightModeColors) });
            }

            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = CreateUri(allStyles) });

            ThemeSettings.SetTitleMode(window, darkTheme);
        }

        private static void SetTitleMode(Window window, bool dark)
        {
            IntPtr hWnd = new WindowInteropHelper(Window.GetWindow(window)).EnsureHandle();
            var attribute = DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;
            bool darkTitle = dark;
            DwmSetWindowAttribute(hWnd, attribute, ref darkTitle, sizeof(uint));
        }
    }
}
