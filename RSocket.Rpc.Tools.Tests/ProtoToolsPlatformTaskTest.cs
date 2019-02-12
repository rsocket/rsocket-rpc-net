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

using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Moq;
using NUnit.Framework;

namespace RSocket.Rpc.Tools.Tests
{
    public class ProtoToolsPlatformTaskTest
    {
        ProtoToolsPlatform _task;
        int _cpuMatched, _osMatched;

        [OneTimeSetUp]
        public void SetUp()
        {
            var mockEng = new Mock<IBuildEngine>();
            _task = new ProtoToolsPlatform() { BuildEngine = mockEng.Object };
            _task.Execute();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Assert.AreEqual(1, _cpuMatched, "CPU type detection failed");
            Assert.AreEqual(1, _osMatched, "OS detection failed");
        }

        // PlatformAttribute not yet available in NUnit, coming soon:
        // https://github.com/nunit/nunit/pull/3003.
        // Use same test case names as under the full framework.
        [Test]
        public void CpuIsX86()
        {
            if (RuntimeInformation.OSArchitecture == Architecture.X86)
            {
                _cpuMatched++;
                Assert.AreEqual("x86", _task.Cpu);
            }
        }

        [Test]
        public void CpuIsX64()
        {
            if (RuntimeInformation.OSArchitecture == Architecture.X64)
            {
                _cpuMatched++;
                Assert.AreEqual("x64", _task.Cpu);
            }
        }

        [Test]
        public void OsIsWindows()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _osMatched++;
                Assert.AreEqual("windows", _task.Os);
            }
        }

        [Test]
        public void OsIsLinux()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _osMatched++;
                Assert.AreEqual("linux", _task.Os);
            }
        }

        [Test]
        public void OsIsMacOsX()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _osMatched++;
                Assert.AreEqual("macosx", _task.Os);
            }
        }
    };
}
