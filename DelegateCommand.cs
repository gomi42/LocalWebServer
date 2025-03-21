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
using System.Windows.Input;

namespace WebServer
{
    public class DelegateCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        //////////////////////////////////////////////

        public event EventHandler CanExecuteChanged;

        //////////////////////////////////////////////

        public DelegateCommand(Action execute)
            : this(execute, null)
        {
        }

        //////////////////////////////////////////////

        public DelegateCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        //////////////////////////////////////////////

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, null);
            }
        }

        //////////////////////////////////////////////

        public bool CanExecute(object parameter)
        {
            return canExecute == null ? true : canExecute();
        }

        //////////////////////////////////////////////

        public void Execute(object parameter)
        {
            execute();
        }
    }

    public class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> execute;
        private readonly Func<bool> canExecute;

        //////////////////////////////////////////////

        public event EventHandler CanExecuteChanged;

        //////////////////////////////////////////////

        public DelegateCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        //////////////////////////////////////////////

        public DelegateCommand(Action<T> execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        //////////////////////////////////////////////

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, null);
            }
        }

        //////////////////////////////////////////////

        public bool CanExecute(object parameter)
        {
            return canExecute == null ? true : canExecute();
        }

        //////////////////////////////////////////////

        public void Execute(object parameter)
        {
            execute((T)parameter);
        }
    }
}
