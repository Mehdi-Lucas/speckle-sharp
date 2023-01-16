using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace PerformanceTests
{
  

  public class Program
  {
    public static async Task Main(string[] args)
    {
      BenchmarkRunner.Run<GraphTraversalBenchmark>();

      // var g = new GraphTraversalBenchmark();
      // g.Iteration = 18;
      // await g.SetupData();
      //
      // var t = g.Traverse();
      // Console.WriteLine(t.Count);
    }
  }
}
