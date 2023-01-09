using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateObject : ICommand<IEnumerable<string>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("objects")]
      private IEnumerable<ArchicadObject> Datas { get; }

      public Parameters(IEnumerable<ArchicadObject> datas)
      {
        Datas = datas;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationIds")]
      public IEnumerable<string> ApplicationIds { get; private set; }
    }

    private IEnumerable<ArchicadObject> Datas { get; }

    public CreateObject(IEnumerable<ArchicadObject> datas)
    {
      foreach (var data in datas)
      {
        // todo
        //data.displayValue = null;
        data.basePoint = null;
      }

      Datas = datas;
    }

    public async Task<IEnumerable<string>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateObject", new Parameters(Datas));
      return result == null ? null : result.ApplicationIds;
    }

  }
}
