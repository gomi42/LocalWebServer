﻿//
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

namespace WebServer
{
    public partial class App : Application
    {
        Window window;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            window = new MainWindow();
            window.DataContext = new MainViewModel();
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            LruManager.Instance.Save();

            if (window.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
