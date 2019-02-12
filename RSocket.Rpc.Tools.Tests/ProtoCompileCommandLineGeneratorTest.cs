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

using System.IO;
using Microsoft.Build.Framework;
using Moq;
using NUnit.Framework;

namespace RSocket.Rpc.Tools.Tests
{
    public class ProtoCompileCommandLineGeneratorTest : ProtoCompileBasicTest
    {
        [SetUp]
        public new void SetUp()
        {
            _task.Generator = "csharp";
            _task.OutputDir = "outdir";
            _task.ProtoBuf = Utils.MakeSimpleItems("a.proto");
        }

        void ExecuteExpectSuccess()
        {
            _mockEngine
              .Setup(me => me.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
              .Callback((BuildErrorEventArgs e) =>
                  Assert.Fail($"Error logged by build engine:\n{e.Message}"));
            bool result = _task.Execute();
            Assert.IsTrue(result);
        }

        [Test]
        public void MinimalCompile()
        {
            ExecuteExpectSuccess();
            Assert.That(_task.LastPathToTool, Does.Match(@"protoc(.exe)?$"));
            Assert.That(_task.LastResponseFile, Is.EqualTo(new[] {
                "--csharp_out=outdir", "a.proto" }));
        }

        [Test]
        public void CompileTwoFiles()
        {
            _task.ProtoBuf = Utils.MakeSimpleItems("a.proto", "foo/b.proto");
            ExecuteExpectSuccess();
            Assert.That(_task.LastResponseFile, Is.EqualTo(new[] {
                "--csharp_out=outdir", "a.proto", "foo/b.proto" }));
        }

        [Test]
        public void CompileWithProtoPaths()
        {
            _task.ProtoPath = new[] { "/path1", "/path2" };
            ExecuteExpectSuccess();
            Assert.That(_task.LastResponseFile, Is.EqualTo(new[] {
                "--csharp_out=outdir", "--proto_path=/path1",
                "--proto_path=/path2", "a.proto" }));
        }

        [TestCase("Cpp")]
        [TestCase("CSharp")]
        [TestCase("Java")]
        [TestCase("Javanano")]
        [TestCase("Js")]
        [TestCase("Objc")]
        [TestCase("Php")]
        [TestCase("Python")]
        [TestCase("Ruby")]
        public void CompileWithOptions(string gen)
        {
            _task.Generator = gen;
            _task.OutputOptions = new[] { "foo", "bar" };
            ExecuteExpectSuccess();
            gen = gen.ToLowerInvariant();
            Assert.That(_task.LastResponseFile, Is.EqualTo(new[] {
                $"--{gen}_out=outdir", $"--{gen}_opt=foo,bar", "a.proto" }));
        }

        [Test]
        public void OutputDependencyFile()
        {
            _task.DependencyOut = "foo/my.protodep";
            // Task fails trying to read the non-generated file; we ignore that.
            _task.Execute();
            Assert.That(_task.LastResponseFile,
                Does.Contain("--dependency_out=foo/my.protodep"));
        }

        [Test]
        public void OutputDependencyWithProtoDepDir()
        {
            _task.ProtoDepDir = "foo";
            // Task fails trying to read the non-generated file; we ignore that.
            _task.Execute();
            Assert.That(_task.LastResponseFile,
                Has.One.Match(@"^--dependency_out=foo[/\\].+_a.protodep$"));
        }

        [Test]
        public void GenerateRSocketRpc()
        {
            _task.RSocketRpcPluginExe = "/foo/rsocket_rpc_gen";
            ExecuteExpectSuccess();
            Assert.That(_task.LastResponseFile, Is.SupersetOf(new[] {
                "--csharp_out=outdir", "--rsocket_rpc_out=outdir",
                "--plugin=protoc-gen-rsocket_rpc=/foo/rsocket_rpc_gen" }));
        }

        [Test]
        public void GenerateRSocketRpcWithOutDir()
        {
            _task.RSocketRpcPluginExe = "/foo/rsocket_rpc_gen";
            _task.RSocketRpcOutputDir = "gen-out";
            ExecuteExpectSuccess();
            Assert.That(_task.LastResponseFile, Is.SupersetOf(new[] {
                "--csharp_out=outdir", "--rsocket_rpc_out=gen-out" }));
        }

        [Test]
        public void GenerateRSocketRpcWithOptions()
        {
            _task.RSocketRpcPluginExe = "/foo/rsocket_rpc_gen";
            _task.RSocketRpcOutputOptions = new[] { "baz", "quux" };
            ExecuteExpectSuccess();
            Assert.That(_task.LastResponseFile,
                        Does.Contain("--rsocket_rpc_opt=baz,quux"));
        }

        [Test]
        public void DirectoryArgumentsSlashTrimmed()
        {
            _task.RSocketRpcPluginExe = "/foo/rsocket_rpc_gen";
            _task.RSocketRpcOutputDir = "gen-out/";
            _task.OutputDir = "outdir/";
            _task.ProtoPath = new[] { "/path1/", "/path2/" };
            ExecuteExpectSuccess();
            Assert.That(_task.LastResponseFile, Is.SupersetOf(new[] {
        "--proto_path=/path1", "--proto_path=/path2",
        "--csharp_out=outdir", "--rsocket_rpc_out=gen-out" }));
        }

        [TestCase(".", ".")]
        [TestCase("/", "/")]
        [TestCase("//", "/")]
        [TestCase("/foo/", "/foo")]
        [TestCase("/foo", "/foo")]
        [TestCase("foo/", "foo")]
        [TestCase("foo//", "foo")]
        [TestCase("foo/\\", "foo")]
        [TestCase("foo\\/", "foo")]
        [TestCase("C:\\foo", "C:\\foo")]
        [TestCase("C:", "C:")]
        [TestCase("C:\\", "C:\\")]
        [TestCase("C:\\\\", "C:\\")]
        public void DirectorySlashTrimmingCases(string given, string expect)
        {
            if (Path.DirectorySeparatorChar == '/')
                expect = expect.Replace('\\', '/');
            _task.OutputDir = given;
            ExecuteExpectSuccess();
            Assert.That(_task.LastResponseFile,
                        Does.Contain("--csharp_out=" + expect));
        }
    };
}
