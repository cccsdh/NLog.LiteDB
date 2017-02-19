using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog.Layouts;


namespace NLog.LiteDB.Specs.UnitTests
{
    [TestClass]
    public class LiteDBFieldSpecs
    {
        [TestMethod]
        public void TestConstructor()
        {
            const string name = "SomeName!";
            var layout = new SimpleLayout
                             {
                                 Text = "SomeText"
                             };

            var field = new LiteDBField(name, layout);

            field.Name
                .Should().Be(name);
            field.Layout
                .Should().Be(layout);
        }
    }
}