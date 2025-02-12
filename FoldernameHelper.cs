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

namespace WebServer
{
    internal static class FolderNameHelper
    {
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// The LimitPath shortens a given full specified folder or file
        /// path so that it fits into the given number of characters.
        /// The shortening algorithm removes subfolders from the
        /// middle of the path and replaces them with a single "...". E.g
        /// "d:\aaaa\bbbbb\ccccc\ddddd\eeeee\fffff\gggg"
        /// may be shortened to
        /// "d:\aaaa\bbbbb\...\fffff\gggg" 
        /// </summary>
        public static string LimitPath(string folder, int maxTextLength)
        {
            if (folder.Length < maxTextLength)
            {
                return folder;
            }

            var folderParts = folder.Split('\\');

            if (folderParts.Length <= 3)
            {
                return folder;
            }

            string leftPart = folderParts[0] + "\\" + folderParts[1] + "\\";
            int leftIndex = 2;

            int rightIndex = folderParts.Length - 1;
            string rightPart = "\\" + folderParts[rightIndex];
            rightIndex--;

            bool nextAddAtLeft = false;
            int nextIndex = rightIndex;

            while (leftIndex != rightIndex && leftPart.Length + rightPart.Length + folderParts[nextIndex].Length + 2 < maxTextLength)
            {
                if (nextAddAtLeft)
                {
                    leftPart += folderParts[leftIndex] + "\\";
                    leftIndex++;

                    nextAddAtLeft = false;
                    nextIndex = rightIndex;
                }
                else
                {
                    rightPart = "\\" + folderParts[rightIndex] + rightPart;
                    rightIndex--;

                    nextAddAtLeft = true;
                    nextIndex = leftIndex;
                }
            }

            nextAddAtLeft = !nextAddAtLeft;
            nextIndex = nextAddAtLeft ? leftIndex : rightIndex;

            while (leftIndex != rightIndex && leftPart.Length + rightPart.Length + folderParts[nextIndex].Length + 2 < maxTextLength)
            {
                if (nextAddAtLeft)
                {
                    leftPart += folderParts[nextIndex] + "\\";
                    nextIndex++;
                }
                else
                {
                    rightPart = "\\" + folderParts[nextIndex] + rightPart;
                    nextIndex--;
                }
            }

            return leftPart + "…" + rightPart;
        }
    }
}
