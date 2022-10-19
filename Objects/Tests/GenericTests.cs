using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Objects;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace Tests
{
  [TestFixture]
  public class GenericTests
  {
    public static IEnumerable<Type> AvailableTypesInKit()
    {
      // Get all types in the Objects assembly that inherit from Base
      return Assembly.GetAssembly(typeof(ObjectsKit))
        .GetTypes()
        .Where(t => typeof(Base).IsAssignableFrom(t));
    }
    
    [Test(Description = "Checks that all objects inside the Default Kit have empty constructors.")]
    [TestCaseSource(nameof(AvailableTypesInKit))]
    public void ObjectHasEmptyConstructor(Type t)
    {
      var constructor = t.GetConstructor(Type.EmptyTypes);
      Assert.That(constructor, Is.Not.Null);
    }
    public static IEnumerable<Type> ConcretePublicTypes() => AvailableTypesInKit().Where(t => !t.IsAbstract && t.IsVisible);
    
    [Test(Description = "Check all objects can serialise and deserialise")]
    [TestCaseSource(nameof(ConcretePublicTypes))]
    public void ObjectCanSerializeDeserialize(Type t)
    {
      Base objIn = (Base)Activator.CreateInstance(t);
      
      string json = Operations.Serialize(objIn);
      Assert.NotNull(json);

      Base objOut = Operations.Deserialize(json);
      Assert.NotNull(objOut);
      Assert.That(objOut, Is.TypeOf(t));
      
      Assert.AreEqual(objOut.GetId(), objIn.GetId());
    }
    
  }
}