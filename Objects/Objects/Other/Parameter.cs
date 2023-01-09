using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Objects.BuiltElements.Revit;
using Objects.Utils;
using System.Runtime.CompilerServices;

namespace Objects.Other
{
  /// <summary>
  /// Generic class for a set of <see cref="Parameter"/>
  /// </summary>
  public class ParameterSet : Base
  {
    /// <summary>
    /// Gets or sets the name of the <see cref="ParameterSet"/>
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// Gets or sets the list of <see cref="Parameter"/>
    /// </summary>
    public List<Parameter> parameters { get; set; }

    public ParameterSet() { }

    /// <summary>
    /// Flattens the list of parameters into a Base so it can be used with the Speckle parameters prop
    /// </summary>
    /// <returns></returns>
    public virtual Base ToBase()
    {
      if (parameters == null)
        return null;

      var @base = new Base();
      foreach (Parameter p in parameters)
      {
        var key = p.name;
        if (string.IsNullOrEmpty(key) || @base[key] != null)
          continue;
        @base[key] = p;
      }

      return @base;
    }
  }

  /// <summary>
  /// Generic class for an object parameter
  /// </summary>
  public class Parameter : Base
  {
    /// <summary>
    /// Gets or sets the name of the <see cref="Parameter"/>
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// Gets or sets the value of the <see cref="Parameter"/>
    /// </summary>
    public object value { get; set; }

    /// <summary>
    /// Gets or sets the Speckle units of the <see cref="Parameter"/>
    /// </summary>
    /// <remarks>
    /// For non length-based units, set to `None` and use <see cref="applicationUnit"/> instead.
    /// </remarks>
    public string units { get; set; }

    /// <summary>
    /// Gets or sets the application unit of the <see cref="Parameter"/>.
    /// </summary>
    /// <remarks>
    /// Used for non length-based units.
    /// </remarks>
    public string applicationUnit { get; set; }

    public Parameter() { }
  }
}

namespace Objects.Other.Revit
{
  /// <summary>
  /// Revit class for a set of Revit parameters
  /// </summary>
  public class RevitParameterSet : ParameterSet
  {
    public override Base ToBase()
    {
      if (parameters == null)
        return null;

      var @base = new Base();
      foreach (RevitParameter p in parameters)
      {
        //if an applicationId is defined (BuiltInName) use that as key, otherwise use the display name
        var key = string.IsNullOrEmpty(p.applicationInternalName) ? p.name : p.applicationInternalName;
        if (string.IsNullOrEmpty(key) || @base[key] != null)
          continue;

        @base[key] = p;
      }

      return @base;
    }
  }

  /// <summary>
  /// Revit class for an element parameter
  /// </summary>
  public class RevitParameter : Parameter
  {
    public string applicationUnitType { get; set; } //eg UnitType UT_Length
    public string applicationInternalName { get; set; } // BuiltInParameterName or GUID for shared parameter

    /// <summary>
    /// If True it's a Shared Parameter, in which case the ApplicationId field will contain this parameter GUID, 
    /// otherwise it will store its BuiltInParameter name
    /// </summary>
    public bool isShared { get; set; } = false;
    public bool isReadOnly { get; set; } = false;

    /// <summary>
    /// True = Type Parameter, False = Instance Parameter 
    /// </summary>
    public bool isTypeParameter { get; set; } = false;

    public RevitParameter() { }

    [SchemaInfo("RevitParameter", "A Revit instance parameter to set on an element", "Revit", "Families")]
    public RevitParameter([SchemaParamInfo("The Revit display name, BuiltInParameter name or GUID (for shared parameters)")] string name, object value,
      [SchemaParamInfo("(Optional) Speckle units. If not set, it's retrieved from the current document. For non length based parameters (eg. Air Flow) it should be set to 'none' so that the Revit display unit will be used instead.")] string units = "")
    {
      this.name = name;
      this.value = value;
      this.units = units;
      this.applicationInternalName = name;
    }
  }
}


