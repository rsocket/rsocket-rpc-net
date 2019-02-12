#region Copyright notice and license

// Copyright 2018-2019 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using NUnit.Framework;

namespace RSocket.Rpc.Tools.Tests
{
    public class CSharpGeneratorTest : GeneratorTest
    {
        GeneratorServices _generator;

        [SetUp]
        public new void SetUp()
        {
            _generator = GeneratorServices.GetForLanguage("CSharp", _log);
        }

        [TestCase("foo.proto", "Foo.cs", "FooRSocketRpc.cs")]
        [TestCase("sub/foo.proto", "Foo.cs", "FooRSocketRpc.cs")]
        [TestCase("one_two.proto", "OneTwo.cs", "OneTwoRSocketRpc.cs")]
        [TestCase("__one_two!.proto", "OneTwo!.cs", "OneTwo!RSocketRpc.cs")]
        [TestCase("one(two).proto", "One(two).cs", "One(two)RSocketRpc.cs")]
        [TestCase("one_(two).proto", "One(two).cs", "One(two)RSocketRpc.cs")]
        [TestCase("one two.proto", "One two.cs", "One twoRSocketRpc.cs")]
        [TestCase("one_ two.proto", "One two.cs", "One twoRSocketRpc.cs")]
        [TestCase("one .proto", "One .cs", "One RSocketRpc.cs")]
        public void NameMangling(string proto, string expectCs, string expectRSocketRpcCs)
        {
            var poss = _generator.GetPossibleOutputs(Utils.MakeItem(proto, "RSocketRpcServices", "Both"));
            Assert.AreEqual(2, poss.Length);
            Assert.Contains(expectCs, poss);
            Assert.Contains(expectRSocketRpcCs, poss);
        }

        [Test]
        public void NoRSocketRpcOneOutput()
        {
            var poss = _generator.GetPossibleOutputs(Utils.MakeItem("foo.proto"));
            Assert.AreEqual(1, poss.Length);
        }

        [TestCase("none")]
        [TestCase("")]
        public void RSocketRpcNoneOneOutput(string services)
        {
            var item = Utils.MakeItem("foo.proto", "RSocketRpcServices", services);
            var poss = _generator.GetPossibleOutputs(item);
            Assert.AreEqual(1, poss.Length);
        }

        [TestCase("client")]
        [TestCase("server")]
        [TestCase("both")]
        public void RSocketRpcEnabledTwoOutputs(string services)
        {
            var item = Utils.MakeItem("foo.proto", "RSocketRpcServices", services);
            var poss = _generator.GetPossibleOutputs(item);
            Assert.AreEqual(2, poss.Length);
        }

        [Test]
        public void OutputDirMetadataRecognized()
        {
            var item = Utils.MakeItem("foo.proto", "OutputDir", "out");
            var poss = _generator.GetPossibleOutputs(item);
            Assert.AreEqual(1, poss.Length);
            Assert.That(poss[0], Is.EqualTo("out/Foo.cs") | Is.EqualTo("out\\Foo.cs"));
        }
    };
}
