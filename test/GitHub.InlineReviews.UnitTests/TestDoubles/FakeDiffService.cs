﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitHub.InlineReviews.Services;
using GitHub.Models;
using GitHub.Services;
using LibGit2Sharp;
using NSubstitute;

namespace GitHub.InlineReviews.UnitTests.TestDoubles
{
    sealed class FakeDiffService : IDiffService, IDisposable
    {
        readonly IRepository repository;
        readonly IDiffService inner;

        public FakeDiffService()
        {
            this.repository = CreateRepository();
            this.inner = new DiffService(Substitute.For<IGitClient>());
        }

        public void AddFile(string path, string contents)
        {
            var signature = new Signature("user", "user@user", DateTimeOffset.Now);
            File.WriteAllText(Path.Combine(repository.Info.WorkingDirectory, path), contents);
            repository.Stage(path);
            repository.Commit("Added " + path, signature, signature);

            var tip = repository.Head.Tip.Sha;
        }

        public void Dispose()
        {
            var path = repository.Info.WorkingDirectory;
            repository.Dispose();
            Directory.Delete(path);
        }

        public Task<IList<DiffChunk>> Diff(IRepository repo, string baseSha, string path, byte[] contents)
        {
            var tip = repository.Head.Tip.Sha;
            var blob1 = repository.Head.Tip[path]?.Target as Blob;
            var blob2 = repository.ObjectDatabase.CreateBlob(new MemoryStream(contents), path);
            var patch = repository.Diff.Compare(blob1, blob2).Patch;
            return Task.FromResult<IList<DiffChunk>>(inner.ParseFragment(patch).ToList());
        }

        public IEnumerable<DiffChunk> ParseFragment(string diff)
        {
            return inner.ParseFragment(diff);
        }

        static IRepository CreateRepository()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Repository.Init(tempPath);

            var result = new Repository(tempPath);
            var signature = new Signature("user", "user@user", DateTimeOffset.Now);

            File.WriteAllText(Path.Combine(tempPath, ".gitattributes"), "* text=auto");
            result.Stage("*");
            result.Commit("Initial commit", signature, signature);

            return result;
        }
    }
}