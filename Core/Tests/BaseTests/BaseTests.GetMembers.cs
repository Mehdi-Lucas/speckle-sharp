using NUnit.Framework;
using Speckle.Core.Models;

namespace Tests.BaseTests
{

  public partial class BaseTests
  {
    [Test(Description = "Checks that no ignored or obsolete properties are returned")]
    public void GetMembers_InstanceAndDynamic()
    {
      var @base = new SampleObject();
      var dynamicProp = "dynamicProp";
      @base[dynamicProp] = 123;

      var names = @base.GetMembers().Keys;
      Assert.That(names, Has.No.Member(nameof(@base.IgnoredSchemaProp)));
      Assert.That(names, Has.No.Member(nameof(@base.ObsoleteSchemaProp)));
      Assert.That(names, Has.Member(dynamicProp));
      Assert.That(names, Has.Member(nameof(@base.attachedProp)));
    }

    [Test(Description = "Checks that only instance properties are returned, excluding obsolete and ignored.")]
    public void GetMembers_OnlyInstance()
    {
      var @base = new SampleObject();
      @base["dynamicProp"] = 123;

      var names = @base.GetMembers(DynamicBaseMemberType.Instance).Keys;
      Assert.That(names, Has.Member(nameof(@base.attachedProp)));
    }

    [Test(Description = "Checks that only dynamic properties are returned")]
    public void GetMembers_OnlyDynamic()
    {
      var @base = new SampleObject();
      var dynamicProp = "dynamicProp";
      @base[dynamicProp] = 123;

      var names = @base.GetMembers(DynamicBaseMemberType.Dynamic).Keys;
      Assert.That(names, Has.Member(dynamicProp));
      Assert.That(names.Count, Is.EqualTo(1));
    }

    [Test(Description = "Checks that all typed properties (including ignored ones) are returned")]
    public void GetMembers_OnlyInstance_IncludeIgnored()
    {
      var @base = new SampleObject();
      @base["dynamicProp"] = 123;

      var names = @base.GetMembers(DynamicBaseMemberType.Instance | DynamicBaseMemberType.SchemaIgnored).Keys;
      Assert.That(names, Has.Member(nameof(@base.IgnoredSchemaProp)));
      Assert.That(names, Has.Member(nameof(@base.attachedProp)));
    }

    [Test(Description = "Checks that all typed properties (including obsolete ones) are returned")]
    public void GetMembers_OnlyInstance_IncludeObsolete()
    {
      var @base = new SampleObject();
      @base["dynamicProp"] = 123;

      var names = @base.GetMembers(DynamicBaseMemberType.Instance | DynamicBaseMemberType.Obsolete).Keys;
      Assert.That(names, Has.Member(nameof(@base.ObsoleteSchemaProp)));
      Assert.That(names, Has.Member(nameof(@base.attachedProp)));
    }

    [Test]
    public void ShallowCopy()
    {
      var sample = new SampleObject();
      var copy = sample.ShallowCopy();

      var selectedMembers = DynamicBaseMemberType.Dynamic
                            | DynamicBaseMemberType.Instance
                            | DynamicBaseMemberType.SchemaIgnored;
      var sampleMembers = sample.GetMembers(selectedMembers);
      var copyMembers = copy.GetMembers(selectedMembers);

      foreach (var kvp in copyMembers)
      {
        Assert.Contains(kvp.Key, sampleMembers.Keys);
        Assert.That(kvp.Value, Is.EqualTo(sample[kvp.Key]));
      }
    }
  }
}
