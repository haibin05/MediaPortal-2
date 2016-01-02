﻿using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  internal class FinishStream
  {
    public WebBoolResult Process(string identifier)
    {
      bool result = true;

      if (identifier == null)
      {
        Logger.Debug("FinishStream: identifier is null");
        result = false;
      }

      if (!StreamControl.ValidateIdentifie(identifier))
      {
        Logger.Debug("FinishStream: unknown identifier: {0}", identifier);
        result = false;
      }

      // Remove the stream from the stream controler
      StreamControl.DeleteStreamItem(identifier);

     return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}