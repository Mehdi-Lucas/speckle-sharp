using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using NUnit.Framework;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Tests.BaseTests
{


  [TestFixture, TestOf(typeof(Base))]
  public partial class BaseTests
  {

    [Test]
    [TestCase("something", "some other value")]
    [TestCase(null, "something else")]
    [TestCase("something", null)]
    public void Indexer_GetSet(string value1, string value2)
    {
      const string KEY = "dynamicProp";
      var @base = new SampleObject();

      // Test value is initially null
      Assert.IsNull(@base[KEY]);

      // Can create a new dynamic member
      @base[KEY] = value1;
      Assert.AreEqual(@base[KEY], value1);

      // Can overwrite existing
      @base[KEY] = value2;
      Assert.AreEqual(@base[KEY], value2);
    }

    [Test]
    public void Indexer_InstanceProp()
    {
      var @base = new ObjectWithItemProp();
      @base.Item = "baz";

      Assert.AreEqual(@base[nameof(@base.Item)], "baz");
      Assert.AreEqual(@base.Item, "baz");
    }

    [Test(Description = "Checks if validation is performed in property names")]
    public void Indexer_InvalidPropNames()
    {
      var @base = new Base();

      // Word chars are OK
      @base["something"] = "B";

      // Only single leading @ allowed
      @base["@something"] = "A";
      Assert.Throws<SpeckleException>(() => { @base["@@@something"] = "Testing"; });

      // Invalid chars:  ./
      Assert.Throws<SpeckleException>(() => { @base["some.thing"] = "Testing"; });
      Assert.Throws<SpeckleException>(() => { @base["some/thing"] = "Testing"; });

      // null name:  ./
      Assert.Throws<ArgumentNullException>(() => { @base[null] = "Testing"; });

      // Trying to change a class member value will throw exceptions.
      //Assert.Throws<Exception>(() => { @base["speckle_type"] = "Testing"; });
      //Assert.Throws<Exception>(() => { @base["id"] = "Testing"; });
    }

    private static string[] GetSourceGetterAccessModifiers()
    {
      var ignore = DynamicBase.GetInstanceMembers(typeof(Base)).Select(x => x.Name).ToImmutableHashSet();
      return DynamicBase.GetInstanceMembers(typeof(MockAccessModifierBase))
        .Select(x => x.Name)
        .Where(x => !ignore.Contains(x))
        .ToArray();
    }

    [Test(Description = "Checks that access modifiers are ignored when dynamically setting/getting properties")]
    [TestCaseSource(nameof(GetSourceGetterAccessModifiers))]
    public void Indexer_AccessModifiers(string propertyName)
    {
      const int PayLoad = 420;
      Base mock = new MockAccessModifierBase();
      Assert.NotNull(mock[propertyName]);
      Assert.That(mock[propertyName], Is.TypeOf<int>());

      mock[propertyName] = PayLoad;

      Assert.NotNull(mock[propertyName]);
      Assert.AreEqual(mock[propertyName], PayLoad);
    }

    [Test(Description = "Tests that get only properties throw an exception when get")]
    public void Indexer_GetOnly()
    {
      var mock = new MockSingleAccessPropsBase<string>();

      string testPropName = nameof(MockSingleAccessPropsBase<string>.GetOnly);
      string playload = "my payload value";

      //Test can get payload
      mock.Backing = playload;
      Assert.AreEqual(mock[testPropName], mock.Backing);

      //Test can't set payload
      var newValue = "my new value";
      Assert.Throws<SpeckleException>(() => mock[testPropName] = newValue);
    }

    [Test(Description = "Tests that set only properties throw an exception when set")]
    public void Indexer_SetOnly()
    {
      var mock = new MockSingleAccessPropsBase<string>();
      string testPropName = nameof(MockSingleAccessPropsBase<string>.SetOnly);
      string playload = "my payload value";

      //Test can set payload
      mock[testPropName] = playload;
      Assert.AreEqual(mock.Backing, playload);

      //Test can't get payload
      Assert.Throws<ArgumentException>(() => _ = mock[testPropName]);
    }

    [Test]
    [TestCase(3000, 1000)]
    [TestCase(1000, 3000)]
    [TestCase(1, 1000)]
    [TestCase(500, 500)]
    public void CountChildren_DynamicChunkables(int listSize, int chunkSize)
    {
      var @base = new Base();
      var customChunk = new List<double>();
      var customChunkArr = new double[listSize];

      for (int i = 0; i < listSize; i++)
      {
        customChunk.Add(i / 2);
        customChunkArr[i] = i;
      }

      @base[$"@({chunkSize})cc1"] = customChunk;
      @base[$"@({chunkSize})cc2"] = customChunkArr;

      var num = @base.GetTotalChildrenCount();
      Assert.AreEqual(listSize / chunkSize * 2 + 1, num);
    }

    [Test]
    [TestCase(Chunkable.DEFAULT_CHUNK_SIZE * 3)]
    [TestCase(1)]
    public void CountChildren_InstanceChunkables(int listSize)
    {
      //Values hard coded elsewhere \/
      const int ChunkSizeList = Chunkable.DEFAULT_CHUNK_SIZE;
      const int ChunkSizeArr = SampleObject.ArrChunkSize;

      var @base = new SampleObject();
      var customChunk = new List<double>(listSize);
      var customChunkArr = new double[listSize];

      for (int i = 0; i < listSize; i++)
      {
        //Ensure the two lists aren't equivalent
        customChunk.Add(i / 2);
        customChunkArr[i] = i;
      }

      @base.list = customChunk;
      @base.arr = customChunkArr;

      var num = @base.GetTotalChildrenCount();
      var actualNum = 1 + (listSize / ChunkSizeArr) + (listSize / ChunkSizeList);
      Assert.AreEqual(actualNum, num);
    }
  }
}
