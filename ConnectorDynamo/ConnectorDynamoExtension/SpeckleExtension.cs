﻿using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Speckle.ConnectorDynamo.Functions;
using System;
using Speckle.Core.Logging;
using Speckle.Core.Kits;
using Dynamo.Applications.Models;
using RevitServices.Persistence;

namespace Speckle.ConnectorDynamo.Extension
{
  public class SpeckleExtension : IViewExtension
  {
    public string UniqueId => "B8160241-CAC4-4189-BF79-87C92914B8EC";

    public string Name => "Speckle Extension";

    public void Loaded(ViewLoadedParams viewLoadedParams)
    {


      try
      {
        var dynamoViewModel = viewLoadedParams.DynamoWindow.DataContext as DynamoViewModel;
        //var speckleWatchHandler = new SpeckleWatchHandler(dynamoViewModel.PreferenceSettings);

        if (dynamoViewModel.Model is RevitDynamoModel rdm)
        {
          rdm.RevitDocumentChanged += Rdm_RevitDocumentChanged;
          Globals.RevitDocument = DocumentManager.Instance.GetType().GetProperty("CurrentDBDocument").GetValue(DocumentManager.Instance);
        }
        //sets a read-only property using reflection WatchHandler
        //typeof(DynamoViewModel).GetProperty("WatchHandler").SetValue(dynamoViewModel, speckleWatchHandler);

      }
      catch (Exception e)
      {

      }

      //initialize telemetry and logging
      if (Globals.RevitDocument != null)
        Setup.Init(Applications.DynamoRevit);
      else
        Setup.Init(Applications.DynamoSandbox);
    }

    private void Rdm_RevitDocumentChanged(object sender, EventArgs e)
    {

      Globals.RevitDocument = DocumentManager.Instance.GetType().GetProperty("CurrentDBDocument").GetValue(DocumentManager.Instance);
    }

    public void Dispose() { }

    public void Shutdown()
    {
      Tracker.TrackPageview(Tracker.SESSION_END);
    }

    public void Startup(ViewStartupParams p) { }
  }
}