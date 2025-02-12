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

using Microsoft.Win32;

namespace WebServer
{
    static class AppEnv
    {
        private const string userRoot = "HKEY_CURRENT_USER";
        private const string subkey = "Software\\gomi\\LocalTestServer";
        private const string keyName = userRoot + "\\" + subkey;
        private const string itemPath = "LruItem{0}";

        ////////////////////////////////////////////////////////////////////

        public static string GetLruItem(int index)
        {
            return (string)Registry.GetValue(keyName, string.Format(itemPath, index), null);
        }

        ////////////////////////////////////////////////////////////////////

        public static void SetLruItem(int index, string value)
        {
            Registry.SetValue(keyName, string.Format(itemPath, index), value);
        }

        ////////////////////////////////////////////////////////////////////

        public static void RemoveLruItem(int index)
        {
            var key = Registry.CurrentUser.OpenSubKey(subkey, true);
            key.DeleteValue(string.Format(itemPath, index), true);
            key.Close();
        }
    }
}