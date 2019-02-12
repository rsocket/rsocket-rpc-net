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
using Microsoft.Build.Utilities;
using NUnit.Framework;

namespace RSocket.Rpc.Tools.Tests
{
    public class DepFileUtilTest
    {

        [Test]
        public void HashString64Hex_IsSane()
        {
            string hashFoo1 = DepFileUtil.HashString64Hex("foo");
            string hashEmpty = DepFileUtil.HashString64Hex("");
            string hashFoo2 = DepFileUtil.HashString64Hex("foo");

            StringAssert.IsMatch("^[a-f0-9]{16}$", hashFoo1);
            Assert.AreEqual(hashFoo1, hashFoo2);
            Assert.AreNotEqual(hashFoo1, hashEmpty);
        }

        [Test]
        public void GetDepFilenameForProto_IsSane()
        {
            StringAssert.IsMatch(@"^out[\\/][a-f0-9]{16}_foo.protodep$",
                DepFileUtil.GetDepFilenameForProto("out", "foo.proto"));
            StringAssert.IsMatch(@"^[a-f0-9]{16}_foo.protodep$",
                DepFileUtil.GetDepFilenameForProto("", "foo.proto"));
        }

        [Test]
        public void GetDepFilenameForProto_HashesDir()
        {
            string PickHash(string fname) =>
                DepFileUtil.GetDepFilenameForProto("", fname).Substring(0, 16);

            string same1 = PickHash("dir1/dir2/foo.proto");
            string same2 = PickHash("dir1/dir2/proto.foo");
            string same3 = PickHash("dir1/dir2/proto");
            string same4 = PickHash("dir1/dir2/.proto");
            string unsame1 = PickHash("dir2/foo.proto");
            string unsame2 = PickHash("/dir2/foo.proto");

            Assert.AreEqual(same1, same2);
            Assert.AreEqual(same1, same3);
            Assert.AreEqual(same1, same4);
            Assert.AreNotEqual(same1, unsame1);
            Assert.AreNotEqual(unsame1, unsame2);
        }

        //////////////////////////////////////////////////////////////////////////
        // Full file reading tests

        // Generated by protoc on Windows. Slashes vary.
        const string depFile1 =
    @"C:\projects\foo\src\./foo.rsocket_rpc.pb.cc \
C:\projects\foo\src\./foo.rsocket_rpc.pb.h \
C:\projects\foo\src\./foo.pb.cc \
 C:\projects\foo\src\./foo.pb.h: C:/usr/include/google/protobuf/wrappers.proto\
   C:/usr/include/google/protobuf/any.proto\
C:/usr/include/google/protobuf/source_context.proto\
   C:/usr/include/google/protobuf/type.proto\
   foo.proto";

        // This has a nasty output directory with a space.
        const string depFile2 =
    @"obj\Release x64\net45\/Foo.cs \
obj\Release x64\net45\/FooRSocketRpc.cs: C:/usr/include/google/protobuf/wrappers.proto\
 C:/projects/foo/src//foo.proto";

        [Test]
        public void ReadDependencyInput_FullFile1()
        {
            string[] deps = ReadDependencyInputFromFileData(depFile1, "foo.proto");

            Assert.NotNull(deps);
            Assert.That(deps, Has.Length.InRange(4, 5));  // foo.proto may or may not be listed.
            Assert.That(deps, Has.One.EndsWith("wrappers.proto"));
            Assert.That(deps, Has.One.EndsWith("type.proto"));
            Assert.That(deps, Has.None.StartWith(" "));
        }

        [Test]
        public void ReadDependencyInput_FullFile2()
        {
            string[] deps = ReadDependencyInputFromFileData(depFile2, "C:/projects/foo/src/foo.proto");

            Assert.NotNull(deps);
            Assert.That(deps, Has.Length.InRange(1, 2));
            Assert.That(deps, Has.One.EndsWith("wrappers.proto"));
            Assert.That(deps, Has.None.StartWith(" "));
        }

        [Test]
        public void ReadDependencyInput_FullFileUnparsable()
        {
            string[] deps = ReadDependencyInputFromFileData("a:/foo.proto", "/foo.proto");
            Assert.NotNull(deps);
            Assert.Zero(deps.Length);
        }

        // NB in our tests files are put into the temp directory but all have
        // different names. Avoid adding files with the same directory path and
        // name, or add reasonable handling for it if required. Tests are run in
        // parallel and will collide otherwise.
        private string[] ReadDependencyInputFromFileData(string fileData, string protoName)
        {
            string tempPath = Path.GetTempPath();
            string tempfile = DepFileUtil.GetDepFilenameForProto(tempPath, protoName);
            try
            {
                File.WriteAllText(tempfile, fileData);
                var mockEng = new Moq.Mock<IBuildEngine>();
                var log = new TaskLoggingHelper(mockEng.Object, "x");
                return DepFileUtil.ReadDependencyInputs(tempPath, protoName, log);
            }
            finally
            {
                try
                {
                    File.Delete(tempfile);
                }
                catch { }
            }
        }
    };
}
