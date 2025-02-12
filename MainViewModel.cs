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
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace WebServer
{
    internal class MainViewModel : ViewModelBase, IDisposable
    {
        const int initialPort = 8080;
        private SimpleWebServer webServer;
        private ObservableCollection<ServerViewModel> servers;
        private bool showError;

        ////////////////////////////////////////////////////////////////////

        public MainViewModel()
        {
            AddServerCommand = new DelegateCommand(OnAddServerCommand);
            Servers = new ObservableCollection<ServerViewModel>();
            LruManager.Instance.CleanUp(item => !Directory.Exists(item) && !File.Exists(item));
            AddServer();
        }

        ////////////////////////////////////////////////////////////////////

        public ObservableCollection<ServerViewModel> Servers
        { 
            get => servers;
            set => SetProperty(ref servers, value);
        }

        ////////////////////////////////////////////////////////////////////

        public ICommand AddServerCommand {  get; set; }

        ////////////////////////////////////////////////////////////////////

        public bool ShowError
        { 
            get => showError;
            set => SetProperty(ref showError, value);
        }

        ////////////////////////////////////////////////////////////////////

        public void Dispose()
        {
            webServer?.Stop();
            webServer = null;
        }

        ////////////////////////////////////////////////////////////////////

        private void OnAddServerCommand()
        {
            AddServer();
        }

        ////////////////////////////////////////////////////////////////////

        private void AddServer()
        {
            if (webServer == null)
            {
                webServer = new SimpleWebServer();
            }
            else
            {
                webServer.Stop();
            }

            var last = servers.Count - 1;

            if (last >= 9)
            {
                return;
            }

            int port;

            if (last >= 0)
            {
                port = servers[last].Port + 1;
            }
            else
            {
                port = initialPort;
            }

            servers.Add(new ServerViewModel(webServer, last + 1, port));

            if (!webServer.Start())
            {
                Servers.Clear();
                webServer = null;
                ShowError = true;
            }
            else
            {
                ShowError = false;
            }
        }
    }
}
