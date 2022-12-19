using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using Archicad.Operations;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Object : IConverter
  {
    #region --- Properties ---

    public Type Type => typeof(Objects.BuiltElements.Component);

    #endregion

    #region --- Functions ---

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var objects = new List<Objects.BuiltElements.Archicad.ArchicadObject>();
      foreach (var el in elements)
      {
        switch (el)
        {
          case Objects.BuiltElements.Archicad.ArchicadObject archicadObject:
            objects.Add(archicadObject);
            break;
          case Objects.BuiltElements.Component component:

            // upgrade (if not Archicad object): Objects.BuiltElements.Component --> Objects.BuiltElements.Archicad.ArchicadObject
            var basePoint = (Point)component.basePoint;
            var newObject = new Objects.BuiltElements.Archicad.ArchicadObject(Utils.ScaleToNative(basePoint));
            objects.Add(newObject);
            break;
        }
      }

      var result = await AsyncCommandProcessor.Execute(new Communication.Commands.CreateObject(objects), token);

      return result is null ? new List<string>() : result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements, CancellationToken token)
    {
      var elementModels = elements as ElementModelData[] ?? elements.ToArray();
      IEnumerable<Objects.BuiltElements.Archicad.ArchicadObject> data =
        await AsyncCommandProcessor.Execute(
          new Communication.Commands.GetObjectData(elementModels.Select(e => e.applicationId)),
          token);
      if (data is null)
      {
        return new List<Base>();
      }

      List<Base> objects = new List<Base>();
      foreach (Objects.BuiltElements.Archicad.ArchicadObject @object in data)
      {
        @object.displayValue =
          Operations.ModelConverter.MeshesToSpeckle(elementModels.First(e => e.applicationId == @object.applicationId)
            .model);

        // downgrade (always): Objects.BuiltElements.Archicad.ArchicadObject --> Objects.BuiltElements.Component
        {
          @object.basePoint = @object.pos;
        }

        objects.Add(@object);
      }

      return objects;
    }

    #endregion
  }
}
