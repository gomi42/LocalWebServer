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
using WebServer.Properties;

namespace WebServer
{
#if DEBUG
    public class DesignServerViewModel
    {
        public DesignServerViewModel(int serverNumber, int port)
        {
            ServerNumber = serverNumber + 1;
            Port = port;

            Folder = Resources.DragAndDropHere;
            Hyperlink = SimpleWebServer.FormatHostUri(port);
        }

        public SimpleWebServer WebServer { get; private set; }

        public int ServerNumber { get; private set; }

        public int Port { get; private set; }

        public string Folder { get; private set; }

        public string Hyperlink { get; private set; }

        public bool IsHyperlinkEnabled { get; private set; }
    }

    public class DesignViewModel
    {
        public DesignViewModel()
        {
            int[] ports = new int[] { 8080, 8081, 8082 };

            var servers = new List<DesignServerViewModel>();

            for (int i = 0; i < ports.Length; i++)
            {
                servers.Add(new DesignServerViewModel(i, ports[i]));
            }

            Servers = servers;
        }

        public List<DesignServerViewModel> Servers { get; set; }
    }
#else
    public class DesignViewModel
    {
    }
#endif    
}