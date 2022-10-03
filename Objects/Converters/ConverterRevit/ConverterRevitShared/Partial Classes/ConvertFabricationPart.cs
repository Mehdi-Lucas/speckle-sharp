using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public RevitElement FabricationPartToSpeckle(FabricationPart revitElement, out List<string> notes)
    {
      notes = new List<string>();

      RevitElement speckleElement = new RevitElement();

      speckleElement.type = revitElement.Name;

      speckleElement.category = revitElement.Category.Name;

      speckleElement.displayValue = GetFabricationMeshes(revitElement);

      //Only send elements that have a mesh, if not we should probably support them properly via direct conversions
      if (speckleElement.displayValue == null || speckleElement.displayValue.Count == 0)
      {
        notes.Add("Not sending elements without display meshes");
        return null;
      }

      GetAllRevitParamsAndIds(speckleElement, revitElement);

      return speckleElement;
    }

    /// <summary>
    /// Get meshes from fabrication parts which have different geometry hierarchy than other revit elements.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="subElements"></param>
    /// <returns></returns>
    public List<Geometry.Mesh> GetFabricationMeshes(Element element, List<Element> subElements = null)
    {
      //Search for solids on geometry element level
      var allSolids = GetElementSolids(element, opt: new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true });

      List<Geometry.Mesh> meshes = new List<Geometry.Mesh>();

      var geom = element.get_Geometry(new Options());
      foreach (GeometryInstance instance in geom)
      {
        //Get instance geometry from fabrication part geometry
        var symbolGeometry = instance.GetInstanceGeometry();

        //Get meshes
        var symbolMeshes = GetMeshes(symbolGeometry, element.Document);
        meshes.AddRange(symbolMeshes);

        //Get solids
        var symbolSolids = GetSolids(symbolGeometry);
        allSolids.AddRange(symbolSolids);
      }

      if (subElements != null)
        foreach (var sb in subElements)
          allSolids.AddRange(GetElementSolids(sb));

      //Convert solids to meshes
      meshes.AddRange(GetMeshesFromSolids(allSolids, element.Document));

      return meshes;
    }
  }
}
