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
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WebServer.Properties;
using Forms = System.Windows.Forms;

namespace WebServer
{
    class ServerViewModel : ViewModelBase
    {
        private SimpleWebServer webServer;
        private string folder;
        private string hyperlink;
        private bool isHyperlinkEnabled;

        ////////////////////////////////////////////////////////////////////

        public ServerViewModel(SimpleWebServer webServer, int serverNumber, int port)
        {
            this.webServer = webServer;
            ServerNumber = serverNumber + 1;
            Port = port;

            DropCommand = new DelegateCommand<DataObject>(OnDropCommand);
            SelectFolderCommand = new DelegateCommand(OnSelectFolderCommand);
            SetFolderCommand = new DelegateCommand<string>(OnSetFolderCommand);
            HyperlinkCommand = new DelegateCommand(OnHyperlinkCommand);

            Folder = Resources.DragAndDropHere;
            Hyperlink = SimpleWebServer.FormatHostUri(port);

            webServer.AddPort(port);
        }

        ////////////////////////////////////////////////////////////////////

        public int ServerNumber { get; private set; }

        ////////////////////////////////////////////////////////////////////

        public int Port { get; private set; }

        ////////////////////////////////////////////////////////////////////

        public string Folder
        {
            get => folder;
            private set => SetProperty(ref folder, value);
        }

        ////////////////////////////////////////////////////////////////////

        public string Hyperlink
        {
            get => hyperlink;
            private set => SetProperty(ref hyperlink, value);
        }

        ////////////////////////////////////////////////////////////////////

        public bool IsHyperlinkEnabled
        {
            get => isHyperlinkEnabled;
            private set => SetProperty(ref isHyperlinkEnabled, value);
        }

        ////////////////////////////////////////////////////////////////////

        public IReadOnlyList<string> LruItems => LruManager.Instance.LruItems;

        ////////////////////////////////////////////////////////////////////

        public ICommand SelectFolderCommand { get; private set; }

        ////////////////////////////////////////////////////////////////////

        public ICommand SetFolderCommand { get; private set; }

        ////////////////////////////////////////////////////////////////////

        public ICommand DropCommand { get; private set; }

        ////////////////////////////////////////////////////////////////////

        public ICommand HyperlinkCommand { get; private set; }

        ////////////////////////////////////////////////////////////////////

        private void OnDropCommand(DataObject e)
        {
            var fileOrFolder = ((string[])e.GetData(DataFormats.FileDrop))[0];
            SetFolderMapping(fileOrFolder);
        }

        ////////////////////////////////////////////////////////////////////

        private void OnSelectFolderCommand()
        {
            Forms.FolderBrowserDialog fld = new Forms.FolderBrowserDialog();

            fld.Description = Resources.SelectFolder;
            fld.ShowNewFolderButton = false;
            fld.SelectedPath = Folder;
            Forms.DialogResult result = fld.ShowDialog();

            if (result == Forms.DialogResult.OK)
            {
                SetFolderMapping(fld.SelectedPath);
            }
        }

        ////////////////////////////////////////////////////////////////////

        private void OnSetFolderCommand(string folder)
        {
            SetFolderMapping(folder);
        }

        ////////////////////////////////////////////////////////////////////

        private void SetFolderMapping(string fileOrFolder)
        {
            var lru = LruManager.Instance;
            lru.Add(fileOrFolder);

            if (Directory.Exists(fileOrFolder))
            {
                Folder = fileOrFolder;
                IsHyperlinkEnabled = true;
                webServer.SetPortRootMapping(Port, fileOrFolder);
                var url = SimpleWebServer.FormatHostUri(Port);
                Hyperlink = url;
                Process.Start(new ProcessStartInfo
                                {
                                    FileName = url,
                                    UseShellExecute = true
                                });
            }
            else
            {
                var folder = Path.GetDirectoryName(fileOrFolder);
                var filename = Path.GetFileName(fileOrFolder);
                Folder = folder;
                IsHyperlinkEnabled = true;
                webServer.SetPortRootMapping(Port, folder);
                var url = SimpleWebServer.FormatHostUri(Port) + filename;
                Hyperlink = url;
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }

        ////////////////////////////////////////////////////////////////////

        private void OnHyperlinkCommand()
        {
            Process.Start(Hyperlink);
        }
    }
}