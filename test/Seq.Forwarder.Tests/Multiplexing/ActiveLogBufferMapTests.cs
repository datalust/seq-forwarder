﻿using System.IO;
using System.Linq;
using Seq.Forwarder.Config;
using Seq.Forwarder.Cryptography;
using Seq.Forwarder.Multiplexing;
using Seq.Forwarder.Tests.Support;
using Xunit;

namespace Seq.Forwarder.Tests.Multiplexing
{    
    public class ActiveLogBufferMapTests
    {
        [Fact]
        public void AnEmptyMapCreatesNoFiles()
        {
            using var tmp = new TempFolder("Buffer");
            using var map = CreateActiveLogBufferMap(tmp);
            Assert.Empty(Directory.GetFileSystemEntries(tmp.Path));
        }

        [Fact]
        public void TheDefaultBufferWritesDataInTheBufferRoot()
        {
            using var tmp = new TempFolder("Buffer");
            using var map = CreateActiveLogBufferMap(tmp);
            var entry = map.GetLogBuffer(null);
            Assert.NotNull(entry);
            Assert.True(File.Exists(Path.Combine(tmp.Path, "data.mdb")));
            Assert.Empty(Directory.GetDirectories(tmp.Path));
            Assert.Same(entry, map.GetLogBuffer(null));
        }

        [Fact]
        public void ApiKeySpecificBuffersWriteDataToSubfolders()
        {
            using var tmp = new TempFolder("Buffer");
            using var map = CreateActiveLogBufferMap(tmp);
            string key1 = Some.ApiKey(), key2 = Some.ApiKey();
            var entry1 = map.GetLogBuffer(key1);
            var entry2 = map.GetLogBuffer(key2);

            Assert.NotNull(entry1);
            Assert.NotNull(entry2);
            Assert.Same(entry1, map.GetLogBuffer(key1));
            Assert.NotSame(entry1, entry2);
            var subdirs = Directory.GetDirectories(tmp.Path);
            Assert.Equal(2, subdirs.Length);
            Assert.True(File.Exists(Path.Combine(subdirs[0], "data.mdb")));
            Assert.True(File.Exists(Path.Combine(subdirs[0], ".apikey")));
        }

        [Fact]
        public void EntriesSurviveReloads()
        {
            var apiKey = Some.ApiKey();
            var value = Some.Bytes(100);

            using var tmp = new TempFolder("Buffer");
            using (var map = CreateActiveLogBufferMap(tmp))
            {
                map.GetLogBuffer(null).Enqueue(new[] {value});
                map.GetLogBuffer(apiKey).Enqueue(new[] {value});
            }

            using (var map = CreateActiveLogBufferMap(tmp))
            {
                var first = map.GetLogBuffer(null).Peek(0).Single();
                var second = map.GetLogBuffer(apiKey).Peek(0).Single();
                Assert.Equal(value, first.Value);
                Assert.Equal(value, second.Value);
            }
        }

        static ActiveLogBufferMap CreateActiveLogBufferMap(TempFolder tmp)
        {
            var config = new SeqForwarderConfig();
            var map = new ActiveLogBufferMap(tmp.Path, config.Storage, config.Output, new InertLogShipperFactory(), StringDataProtector.CreatePlatformDefault());
            map.Load();
            return map;
        }
    }
}
