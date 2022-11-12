using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Other;
using Speckle.Core.Logging;

namespace Objects.Geometry
{
  /// <summary>
  /// A collection of points, with color and size support.
  /// </summary>
  public class Pointcloud : Base, IHasBoundingBox, ITransformable<Pointcloud>
  {
    /// <summary>
    /// Gets or sets the list of points of this <see cref="Pointcloud"/>, stored as a flat list of coordinates [x1,y1,z1,x2,y2,...]
    /// </summary>
    [DetachProperty]
    [Chunkable(31250)]
    public List<double> points { get; set; } = new List<double>();

    /// <summary>
    /// Gets or sets the list of colors of this <see cref="Pointcloud"/>'s points., stored as ARGB <see cref="int"/>s.
    /// </summary>
    [DetachProperty]
    [Chunkable(62500)]
    public List<int> colors { get; set; } = new List<int>();
    
    /// <summary>
    /// Gets or sets the list of sizes of this <see cref="Pointcloud"/>'s points.
    /// </summary>
    [DetachProperty]
    [Chunkable(62500)]
    public List<double> sizes { get; set; } = new List<double>();

    /// <inheritdoc/>
    public Box bbox { get; set; }
    
    /// <summary>
    /// The unit's this <see cref="Pointcloud"/> is in.
    /// This should be one of <see cref="Speckle.Core.Kits.Units"/>
    /// </summary>
    public string units { get; set; }

    /// <summary>
    /// Constructs an empty <see cref="Pointcloud"/>
    /// </summary>
    public Pointcloud()
    {
    }
    
    /// <returns><see cref="points"/> as list of <see cref="Point"/>s</returns>
    /// <exception cref="SpeckleException">when list is malformed</exception>
    public List<Point> GetPoints()
    {
      if (points.Count % 3 != 0) throw new SpeckleException($"{nameof(Pointcloud)}.{nameof(points)} list is malformed: expected length to be multiple of 3");
      
      var pts = new List<Point>(points.Count / 3);
      for (int i = 2; i < points.Count; i += 3)
      {
        pts.Add(new Point(points[i - 2], points[i - 1], points[i], units));
      }
      return pts;
    }

    /// <inheritdoc/>
    public bool TransformTo(Transform transform, out Pointcloud pointcloud)
    {
      pointcloud = new Pointcloud
      {
        units = units,
        points = transform.ApplyToPoints(points),
        colors = colors,
        sizes = sizes,
        applicationId = applicationId
      };
      
      return true;
    }

    /// <inheritdoc/>
    public bool TransformTo(Transform transform, out ITransformable transformed)
    {
      var res = TransformTo(transform, out Pointcloud pc);
      transformed = pc;
      return res;
    }
  }
}