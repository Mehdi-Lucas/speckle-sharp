using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;

namespace TestsUnit.SerializerTests;

[TestFixture, TestOf(typeof(BaseObjectDeserializerV2))]
public class ObjectModelDeprecationTests
{
  [Test, Description("When a breaking type change is made to a member of an object mode, we would expect a ")]
  public async Task Deserializer_HandlesDeprecatedProperty_WhenTypeChangeIsMade()
  {
    const string payloadString = "payload";
    var oldBase = new MyOldBase { myString = payloadString };

    MemoryTransport t = new MemoryTransport();
    string id = await Operations.Send(oldBase, new List<ITransport>() {t});

    var newBase = await Operations.Receive(id, onErrorAction: (m, e) => throw e);

    Assert.That(newBase, Is.TypeOf<MyBase>());
    var myNewBase = (MyBase)newBase;
    Assert.That(myNewBase.myString, Is.EqualTo(default(int)));
    Assert.That(myNewBase[$"DEPRECATED_{nameof(MyBase.myString)}"], Is.EqualTo(payloadString));
  }
}

class MyOldBase : Base
{
  public string myString { get; set; } = "payload";
  public override string speckle_type => "TestsUnit.SerializerTests.MyBase";
}

class MyBase : Base
{
  public int myString { get; set; }
  
  
}
