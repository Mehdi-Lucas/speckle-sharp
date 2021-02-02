﻿using Dynamo.Engine;
using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Speckle.Core.Logging;
using System.Collections.Concurrent;
using Dynamo.Utilities;
using Speckle.ConnectorDynamo.Functions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Speckle.ConnectorDynamo.ReceiveNode
{
  /// <summary>
  /// Receive data from Speckle
  /// </summary>
  [NodeName("Receive")]
  [NodeCategory("Speckle 2")]
  [NodeDescription("Receive data from a Speckle server")]
  [NodeSearchTags("receive", "speckle")]
  [IsDesignScriptCompatible]
  public class Receive : NodeModel
  {
    #region fields & props

    #region private fields & props

    private bool _transmitting = false;
    private string _message = "";
    private double _progress = 0;
    private string _expiredCount = "";
    private bool _receiveEnabled = false;
    private int _objectCount = 1;
    private bool _hasOutput = false;
    private bool _autoUpdate = false;
    private bool _autoUpdateEnabled = true;
    private CancellationTokenSource _cancellationToken;
    private Client _client;

    private Client Client
    {
      get
      {
        if (_client == null && Stream != null)
        {
          var account = Stream.GetAccount();
          _client = new Client(account);
        }

        return _client;
      }
      set { _client = value; }
    }

    #endregion

    #region public properties

    /// <summary>
    /// Current Stream
    /// </summary>
    public StreamWrapper Stream { get; set; }


    /// <summary>
    /// Latest Commit received
    /// </summary>
    public string LastCommitId { get; set; }

    #endregion

    #region ui bindings

    /// <summary>
    /// UI Binding
    /// </summary>
    [JsonIgnore]
    public bool Transmitting
    {
      get => _transmitting;
      set
      {
        _transmitting = value;
        RaisePropertyChanged("Transmitting");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    [JsonIgnore]
    public string Message
    {
      get => _message;
      set
      {
        _message = value;
        RaisePropertyChanged("Message");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    [JsonIgnore]
    public double Progress
    {
      get => _progress;
      set
      {
        _progress = value;
        RaisePropertyChanged("Progress");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    public bool ReceiveEnabled
    {
      get => _receiveEnabled;
      set
      {
        _receiveEnabled = value;
        RaisePropertyChanged("ReceiveEnabled");
      }
    }

    //BINDINGS TO SAVE

    /// <summary>
    /// UI Binding
    /// </summary>
    public string ExpiredCount
    {
      get => _expiredCount;
      set
      {
        _expiredCount = value;
        RaisePropertyChanged("ExpiredCount");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    public bool AutoUpdate
    {
      get => _autoUpdate;
      set
      {
        _autoUpdate = value;
        RaisePropertyChanged("AutoUpdate");
      }
    }

    /// <summary>
    /// UI Binding
    /// </summary>
    public bool AutoUpdateEnabled
    {
      get => _autoUpdateEnabled;
      set
      {
        _autoUpdateEnabled = value;
        RaisePropertyChanged("AutoUpdateEnabled");
      }
    }

    #endregion

    #endregion

    /// <summary>
    /// JSON constructor, called on file open
    /// </summary>
    /// <param name="inPorts"></param>
    /// <param name="outPorts"></param>
    [JsonConstructor]
    private Receive(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
    {
      if (inPorts.Count() == 1)
      {
        //blocker: https://github.com/DynamoDS/Dynamo/issues/11118
        //inPorts.ElementAt(1).DefaultValue = endPortDefaultValue;
      }
      else
      {
        // If information from json does not look correct, clear the default ports and add ones with default value
        InPorts.Clear();
        AddInputs();
      }

      if (!outPorts.Any())
        AddOutputs();

      ArgumentLacing = LacingStrategy.Disabled;

      PropertyChanged += HandlePropertyChanged;
    }

    /// <summary>
    /// Normal constructor, called when adding node to canvas
    /// </summary>
    public Receive()
    {
      Tracker.TrackPageview(Tracker.RECEIVE_ADDED);

      AddInputs();
      AddOutputs();

      RegisterAllPorts();
      ArgumentLacing = LacingStrategy.Disabled;

      PropertyChanged += HandlePropertyChanged;
    }


    private void AddInputs()
    {
      InPorts.Add(new PortModel(PortType.Input, this, new PortData("stream", "The stream to receive from")));
    }

    private void AddOutputs()
    {
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("data", "Data received")));
      OutPorts.Add(new PortModel(PortType.Output, this, new PortData("info", "Commit information")));
    }

    internal void CancelReceive()
    {
      _cancellationToken.Cancel();
      ResetNode();
    }

    /// <summary>
    /// Takes care of actually receiving data
    /// it then stores the data in memory and expires the node
    /// the node AST function retrieves the data from the memory and outputs it
    /// </summary>
    internal void DoReceive()
    {
      //double check, but can probably remove it
      if (!InPorts[0].IsConnected)
      {
        ResetNode(true);
        return;
      }

      //if already receiving, stop and start again
      if (Transmitting)
        CancelReceive();

      if (AutoUpdate)
        Tracker.TrackPageview(Tracker.RECEIVE_AUTO);
      else
        Tracker.TrackPageview(Tracker.RECEIVE_MANUAL);

      Transmitting = true;
      Message = "Receiving...";
      _cancellationToken = new CancellationTokenSource();

      try
      {
        if (Stream == null)
          Core.Logging.Log.CaptureAndThrow(new Exception("The stream provided is invalid"));

        void ProgressAction(ConcurrentDictionary<string, int> dict)
        {
          var val = dict.Values.Average() / _objectCount;
          Message = val.ToString("0%");
          Progress = val * 100;
        }

        var hasErrors = false;

        void ErrorAction(string transportName, Exception e)
        {
          hasErrors = true;
          Message = e.InnerException != null ? e.InnerException.Message : e.Message;
          Message = Message.Contains("401") ? "Not authorized" : Message;
          _cancellationToken.Cancel();
        }

        var data = Functions.Functions.Receive(Stream, _cancellationToken.Token, ProgressAction,
          ErrorAction);

        if (!hasErrors && data != null)
        {
          LastCommitId = ((Commit)data["commit"]).id;

          InMemoryCache.Set(LastCommitId, data);
          Message = "";
        }
      }
      catch (Exception e)
      {
        if (!_cancellationToken.IsCancellationRequested)
        {
          _cancellationToken.Cancel();
          Message = e.Message;
          Core.Logging.Log.CaptureAndThrow(e);
        }
      }
      finally
      {
        ResetNode();
        if (!_cancellationToken.IsCancellationRequested)
        {
          _hasOutput = true;
          ExpireNode();
        }
      }
    }


    /// <summary>
    /// Triggered when the node inputs change
    /// Caches a copy of the inputs (Stream and BranchName)
    /// </summary>
    /// <param name="engine"></param>
    internal void LoadInputs(EngineController engine)
    {
      var oldStream = Stream;
      StreamWrapper newStream = null;

      //update inputs stored locally
      //try parse as streamWrapper
      try
      {
        var inputStream = GetInputAs<StreamWrapper>(engine, 0);
        //avoid editing upstream stream!
        newStream = new StreamWrapper(inputStream.StreamId, inputStream.AccountId, inputStream.ServerUrl)
        {
          BranchName = inputStream.BranchName,
          CommitId = inputStream.CommitId
        };
      }
      catch
      {
        // ignored
      }

      //try parse as Url
      if (newStream == null)
      {
        try
        {
          var url = GetInputAs<string>(engine, 0);
          newStream = new StreamWrapper(url);
        }
        catch
        {
          // ignored
        }
      }

      //invalid input
      if (newStream == null)
      {
        ResetNode(true);
        Message = "Stream is invalid";
        return;
      }

      if (newStream.Type != StreamWrapperType.Branch)
        newStream.BranchName = "main";

      //no need to re-subscribe. it's the same stream
      if (oldStream != null && newStream.ToString() == oldStream.ToString())
        return;

      ResetNode(true);
      Stream = newStream;
      ReceiveEnabled = true;

      //StreamWrapper points to a Stream
      if (newStream.Type == StreamWrapperType.Commit)
      {
        Name = "Receive Commit";
        AutoUpdate = false;
        AutoUpdateEnabled = false;
      }
      else
      {
        Name = "Receive";
        AutoUpdateEnabled = true;
        InitializeReceiver();
      }
    }

    internal void InitializeReceiver()
    {
      if (Stream == null)
        return;

      var account = Stream.GetAccount();
      Client = new Client(account);
      Client.SubscribeCommitCreated(Stream.StreamId);
      Client.OnCommitCreated += OnCommitChange;

      CheckIfBehind();
    }

    private T GetInputAs<T>(EngineController engine, int port)
    {
      var valuesNode = InPorts[port].Connectors[0].Start.Owner;
      var valuesIndex = InPorts[port].Connectors[0].Start.Index;
      var astId = valuesNode.GetAstIdentifierForOutputIndex(valuesIndex).Name;
      var inputMirror = engine.GetMirror(astId);

      if (inputMirror?.GetData() == null) return default(T);

      var data = inputMirror.GetData();

      return (T)data.Data;
    }


    private void CheckIfBehind()
    {
      if (Stream == null)
        return;
      try
      {
        var branches = Client.StreamGetBranches(Stream.StreamId).Result;
        var branchName = string.IsNullOrEmpty(Stream.BranchName) ? "main" : Stream.BranchName;
        var mainBranch = branches.FirstOrDefault(b => b.name == branchName);
        if (mainBranch == null || !mainBranch.commits.items.Any())
        {
          Message = "Empty Stream";
          return;
        }


        var lastCommit = mainBranch.commits.items[0];

        //it's behind! Get objects count from last commit
        if (lastCommit.id != LastCommitId)
        {
          Message = "Updates available";
          GetExpiredObjectCount(lastCommit.referencedObject);
        }
        else
        {
          Message = "Up to date";
        }
      }
      catch (Exception e)
      {
        Core.Logging.Log.CaptureException(e);
      }
    }

    private void GetExpiredObjectCount(string objectId)
    {
      if (Stream == null)
        return;
      try
      {
        var @object = Client.ObjectGet(Stream.StreamId, objectId).Result;
        //quick fix for the scenario in which a single base is sent, we don't want to show 0!
        var count = @object.totalChildrenCount == 0 ? @object.totalChildrenCount + 1 : @object.totalChildrenCount;
        ExpiredCount = count.ToString();
        Message = "Updates available";
        _objectCount = count;
      }
      catch (Exception e)
      {
        Core.Logging.Log.CaptureException(e);
      }
    }

    private void ExpireNode()
    {
      OnNodeModified(true);
    }

    /// <summary>
    /// Reset the node UI
    /// </summary>
    /// <param name="hardReset">If true, resets enabled status and subscriptions too</param>
    private void ResetNode(bool hardReset = false)
    {
      Transmitting = false;
      ExpiredCount = "";
      Progress = 0;
      _hasOutput = false;
      if (hardReset)
      {
        Name = "Receive";
        Stream = null;
        ReceiveEnabled = false;
        Message = "";
        Client?.Dispose();
      }
    }

    #region events

    internal event Action OnNewDataAvail;
    internal event Action OnInputsChanged;

    /// <summary>
    /// Node inputs have changed, trigger load of new inputs
    /// </summary>
    protected virtual void RequestNewInputs()
    {
      OnInputsChanged?.Invoke();
    }

    private void OnCommitChange(object sender, CommitInfo e)
    {
      Task.Run(async () => GetExpiredObjectCount(e.objectId));
      if (AutoUpdate)
        OnNewDataAvail?.Invoke();
    }

    void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != "CachedValue")
        return;

      if (!InPorts[0].IsConnected)
      {
        ResetNode(true);
        return;
      }

      RequestNewInputs();
    }

    #endregion

    #region overrides

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputAstNodes"></param>
    /// <returns></returns>
    public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
    {
      if (!InPorts[0].IsConnected || !_hasOutput || string.IsNullOrEmpty(LastCommitId))
      {
        return OutPorts.Enumerate().Select(output =>
          AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(output.Index), new NullNode()));
      }

      _hasOutput = false;
      var associativeNodes = new List<AssociativeNode>();
      var primitiveNode = AstFactory.BuildStringNode(LastCommitId);
      var dataFunctionCall = AstFactory.BuildFunctionCall(
        new Func<string, object>(Functions.Functions.ReceiveData),
        new List<AssociativeNode> { primitiveNode });

      var infoFunctionCall = AstFactory.BuildFunctionCall(
        new Func<string, string>(Functions.Functions.ReceiveInfo),
        new List<AssociativeNode> { primitiveNode });

      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), dataFunctionCall));
      associativeNodes.Add(AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(1), infoFunctionCall));
      return associativeNodes;
    }

    /// <summary>
    /// Dispose node and Client
    /// </summary>
    public override void Dispose()
    {
      Client?.Dispose();
      base.Dispose();
    }

    #endregion
  }
}