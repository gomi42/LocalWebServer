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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WebServer
{
    public class LruManager
    {
        private int maxLruItems = 10;
        private static LruManager singleton;
        private List<string> lruItems;
        private int maxReadLruItems;
        private bool isModified;

        ////////////////////////////////////////////////////////////////////

        public static LruManager Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new LruManager();
                }

                return singleton;
            }
        }

        ////////////////////////////////////////////////////////////////////

        private LruManager()
        {
            lruItems = new List<string>();

            int index = 1;
            string item = AppEnv.GetLruItem(index);

            while (!string.IsNullOrEmpty(item))
            {
                if (index <= maxLruItems)
                {
                    lruItems.Add(item);
                }
                
                index++;
                item = AppEnv.GetLruItem(index);
            }

            maxReadLruItems = index - 1;
        }

        ////////////////////////////////////////////////////////////////////

        public IReadOnlyList<string> LruItems => new ReadOnlyCollection<string>(lruItems);

        ////////////////////////////////////////////////////////////////////

        public int MaxLruItems
        {
            get => maxLruItems;

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Value must be greater than 0");
                }

                maxLruItems = value;

                while (lruItems.Count > maxLruItems)
                {
                    lruItems.RemoveAt(lruItems.Count - 1);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////

        public void Add(string item)
        {
            int index = lruItems.IndexOf(item);

            if (index == 0)
            {
                return;
            }

            if (index > 0)
            {

                lruItems.RemoveAt(index);
            }
            else
            {
                var cnt = lruItems.Count;

                if (cnt >= maxLruItems)
                {
                    lruItems.RemoveAt(cnt - 1);
                }
            }

            isModified = true;
            var count = lruItems.Count;

            if (count > 0)
            {
                lruItems.Insert(0, item);
            }
            else
            {
                lruItems.Add(item);
            }
        }

        ////////////////////////////////////////////////////////////////////

        public void CleanUp(Func<string, bool> predicate)
        {
            var removeItems = new List<string>();

            foreach (var item in lruItems)
            {
                if (predicate(item))
                {
                    isModified = true;
                    removeItems.Add(item);
                }
            }

            foreach (var item in removeItems)
            {
                lruItems.Remove(item);
            }
        }

        ////////////////////////////////////////////////////////////////////

        public void Save()
        {
            if (!isModified)
            {
                return;
            }

            int index;

            for (index = 0; index < lruItems.Count; index++)
            {
                AppEnv.SetLruItem(index + 1, lruItems[index]);
            }

            for (; index < maxReadLruItems; index++)
            {
                AppEnv.RemoveLruItem(index + 1);
            }
        }
    }
}