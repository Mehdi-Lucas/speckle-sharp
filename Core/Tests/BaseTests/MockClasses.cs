using System;
using System.Collections.Generic;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Tests.BaseTests
{

  public class SampleObject : Base
  {
    [Chunkable] [DetachProperty] public List<double> list { get; set; } = new List<double>();

    public const int ArrChunkSize = 300; //A value != Chunkable.DEFAULT_CHUNK_SIZE

    [Chunkable(ArrChunkSize)]
    [DetachProperty]
    public double[] arr { get; set; }

    [DetachProperty] public SampleProp detachedProp { get; set; }

    public SampleProp attachedProp { get; set; }

    public string @crazyProp { get; set; }

    [SchemaIgnore] public string IgnoredSchemaProp { get; set; }

    [Obsolete("Use attached prop")] public string ObsoleteSchemaProp { get; set; }

    public SampleObject()
    {
    }
  }

  public class SampleProp
  {
    public string name { get; set; }
  }

  public class ObjectWithItemProp : Base
  {
    public string Item { get; set; } = "Item";
  }


  public class MockAccessModifierBase : Base
  {
    public int PublicSetter { get; set; }
    public int PrivateSetter { get; private set; }
    public int ProtectedSetter { get; protected set; }
    public int InternalSetter { get; internal set; }
    public int ProtectedInternalSetter { get; protected internal set; }
    //public int InitSetter { get; init; } //TODO consider testing for c#9 and above features

    public int PublicGetter { get; set; }
    public int PrivateGetter { private get; set; }
    public int ProtectedGetter { protected get; set; }
    public int InternalGetter { internal get; set; }
    public int ProtectedInternalGetter { protected internal get; set; }
  }

  public class MockSingleAccessPropsBase<T> : Base
  {
    public T Backing { get; set; }
    public T GetOnly => Backing;

    public T SetOnly
    {
      set => Backing = value;
    }
  }
}
