﻿#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.Screens;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPowermode
  {
    public static Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      switch (((string)message["PowerMode"]).ToLower())
      {
        case "logoff":
          ServiceRegistration.Get<ISystemStateService>().Logoff(true);
          break;

        case "suspend":
          ServiceRegistration.Get<ISystemStateService>().Suspend();
          break;

        case "hibernate":
          ServiceRegistration.Get<ISystemStateService>().Hibernate();
          break;

        case "reboot":
          ServiceRegistration.Get<ISystemStateService>().Restart(true);
          break;

        case "shutdown":
          ServiceRegistration.Get<ISystemStateService>().Shutdown(true);
          break;

        case "exit":
          ServiceRegistration.Get<IScreenControl>().Shutdown();
          break;
      }
      return Task.FromResult(true);
    }
  }
}
