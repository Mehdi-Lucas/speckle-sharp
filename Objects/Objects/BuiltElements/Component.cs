using Objects.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties.Profiles;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Component : Base, IDisplayValue<List<Mesh>>
  {
    public Point basePoint { get; set; }

    [DetachProperty]
    public List<Mesh> displayValue { get; set; }

    public string units { get; set; }

    public Component() { }

    [SchemaInfo("Component", "Creates a Speckle component", "BIM", "Structure")]
    public Component([SchemaMainParam] Point basePoint)
    {
      this.basePoint = basePoint;
    }
  }
}

namespace Objects.BuiltElements.Archicad
{
  public class ArchicadObject : Objects.BuiltElements.Component
  {
    public ArchicadObject() { }

    [SchemaInfo("ArchicadObject", "Creates an Archicad object.", "Archicad", "Structure")]

    public ArchicadObject(Point basePoint)
    {
      this.basePoint = basePoint;
    }
  }
}