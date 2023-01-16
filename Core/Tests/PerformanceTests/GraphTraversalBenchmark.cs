using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace PerformanceTests
{


  [MemoryDiagnoser]
  // [SimpleJob(RunStrategy.Monitoring, RuntimeMoniker.Net60,launchCount: 2,
  //   warmupCount: 1, iterationCount: 5)]
  // [SimpleJob(RunStrategy.Monitoring, RuntimeMoniker.Net472, baseline: true, launchCount: 2,
  //   warmupCount: 0, iterationCount: 5)]
  [SimpleJob(RunStrategy.Monitoring, baseline: true, launchCount: 2,
    warmupCount: 0, iterationCount: 3)]
  public class GraphTraversalBenchmark
  {

    [Params(0, 2, 8, 19)] public int Iteration { get; set; }

    private ISpeckleConverter converter;
    private Base data;
    private GraphTraversal traversal;

    [GlobalSetup]
    public async Task SetupData()
    {
      // converter = KitManager.GetDefaultKit().LoadConverter(HostApplications.Rhino.GetVersion(HostAppVersion.v7));
      // data = await Helpers.Receive($"https://latest.speckle.dev/streams/efd2c6a31d/branches/{Iteration}",
      //   null,
      //   null,
      //   (_, e) => throw e
      // );
      //traversal = DefaultTraversal.CreateTraverseFunc(converter);
    }

    [Benchmark]
    public async Task<List<ApplicationObject>> FlattenCommitObject()
    {
      var converter = KitManager.GetDefaultKit().LoadConverter(HostApplications.Rhino.GetVersion(HostAppVersion.v7));
      var data = await Helpers.Receive($"https://latest.speckle.dev/streams/efd2c6a31d/branches/{Iteration}",
        null,
        null,
        (_, e) => throw e
      );
      
      Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();

      void StoreObject(Base @base, ApplicationObject appObj)
      {
        if (StoredObjects.ContainsKey(@base.id))
          appObj.Update(
            logItem:
            "Found another object in this commit with the same id. Skipped other object"); //TODO check if we are actually ignoring duplicates, since we are returning the app object anyway...
        else
          StoredObjects.Add(@base.id, @base);
      }

      
      ApplicationObject CreateApplicationObject(Base current, string containerId)
      {
        ApplicationObject NewAppObj()
        {
          var speckleType = current.speckle_type.Split(new [] { ':' }, StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();
          return new ApplicationObject(current.id, speckleType) { applicationId = current.applicationId, Container = containerId };
        }
        
        //Handle convertable objects
        if (converter.CanConvertToNative(current))
        {
          var appObj = NewAppObj();
          appObj.Convertible = true;
          StoreObject(current, appObj);
          return appObj;
        }

        //Handle objects convertable using displayValues
        var fallbackMember = current["displayValue"] ?? current["@displayValue"];
        if (fallbackMember != null)
        {
          var appObj = NewAppObj();

          var fallbackObjects = GraphTraversal.TraverseMember(fallbackMember)
            .Select(o => CreateApplicationObject(o, containerId));
          appObj.Fallback.AddRange(fallbackObjects);

          StoreObject(current, appObj);
          return appObj;
        }
        
        return null;
      }

      string LayerId(TraversalContext context) => LayerIdRecurse(context, new StringBuilder()).ToString();
      StringBuilder LayerIdRecurse(TraversalContext context, StringBuilder stringBuilder)
      {
        if (context.propName == null) return stringBuilder;

        var objectLayerName = context.propName[0] == '@'
          ? context.propName.Substring(1)
          : context.propName;

        LayerIdRecurse(context.parent, stringBuilder);
        stringBuilder.Append("::");
        stringBuilder.Append(objectLayerName);
        
        return stringBuilder;
      }

      var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

      var objectsToConvert = traverseFunction.Traverse(data)
        .Select(tc => CreateApplicationObject(tc.current, LayerId(tc)))
        .Where(appObject => appObject != null)
        .Reverse() //just for the sake of matching the previous behaviour as close as possible
        .ToList();

      Console.WriteLine(StoredObjects.Count);

      return objectsToConvert;
    }


    // [Benchmark]
    // public List<TraversalContext> Traverse()
    // {
    //   return traversal.Traverse(data).ToList();
    // }

  }
}
