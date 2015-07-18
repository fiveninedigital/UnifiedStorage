﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnifiedStorage.Extensions;

// ReSharper disable ConvertToAutoProperty
// ReSharper disable UseNameofExpression
// ReSharper disable UseStringInterpolation
// ReSharper disable ConvertPropertyToExpressionBody

namespace UnifiedStorage.DotNet
{
    internal class DotNetDirectory : IDirectory
    {
        private readonly string _path;
        private readonly string _name;

        public DotNetDirectory(string path)
        {
            _path = System.IO.Path.GetFullPath(path);
            _name = System.IO.Path.GetDirectoryName(path);
        }

        public string Name
        {
            get { return _name; }
        }

        public string Path
        {
            get { return _path; }
        }

        public async Task<IFile> CreateFileAsync(string desiredName, CollisionOption option,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            string nameToUse = desiredName;
            string newPath = System.IO.Path.Combine(Path, nameToUse);

            if (File.Exists(newPath))
            {
                switch (option)
                {
                    case CollisionOption.GenerateUniqueName:
                    {
                        string desiredRoot = System.IO.Path.GetFileNameWithoutExtension(desiredName);
                        string desiredExtension = System.IO.Path.GetExtension(desiredName);
                        for (int num = 1; File.Exists(newPath); num++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            nameToUse = string.Format("{0} ({1}).{2}", desiredRoot, num, desiredExtension);
                            newPath = System.IO.Path.Combine(Path, nameToUse);
                        }

                        break;
                    }
                    case CollisionOption.ReplaceExisting:
                    {
                        File.Delete(newPath);
                        CreateFile(newPath);

                        break;
                    }
                    case CollisionOption.FailIfExists:
                        throw new Exceptions.UnifiedIOException("Cannot create file, it already exists");

                    case CollisionOption.OpenIfExists:
                    {
                        // Do nothing
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException("option", option, null);
                }
            }
            else
            {
                //	Create file
                CreateFile(newPath);
            }

            return new DotNetFile(newPath);
        }

        public async Task<IFile> GetFileAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            string path = System.IO.Path.Combine(Path, name);
            return new DotNetFile(path);
        }

        public async Task<IList<IFile>> GetFilesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            EnsureExists();

            IList<IFile> files = Directory.GetFiles(Path)
                .Select(f => new DotNetFile(f))
                .ToList<IFile>()
                .AsReadOnly();

            return files;
        }

        public async Task<IList<IFile>> GetFilesAsync(string searchPattern, CancellationToken cancellationToken = new CancellationToken())
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            EnsureExists();

            IList<IFile> files = Directory.GetFiles(Path, searchPattern, SearchOption.TopDirectoryOnly)
                .Select(f => new DotNetFile(f))
                .ToList<IFile>()
                .AsReadOnly();

            return files;
        }

        public async Task<IDirectory> CreateFolderAsync(string desiredName, CollisionOption option,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            EnsureExists();

            string nameToUse = desiredName;
            string newPath = System.IO.Path.Combine(Path, nameToUse);
            if (Directory.Exists(newPath))
            {
                switch (option)
                {
                    case CollisionOption.GenerateUniqueName:
                    {
                        for (int num = 2; Directory.Exists(newPath); num++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            nameToUse = string.Format("{0} ({1})", desiredName, num);
                            newPath = System.IO.Path.Combine(Path, nameToUse);
                        }

                        Directory.CreateDirectory(newPath);
                        break;
                    }

                    case CollisionOption.ReplaceExisting:
                    {
                        Directory.Delete(newPath, true);
                        Directory.CreateDirectory(newPath);
                        break;
                    }

                    case CollisionOption.FailIfExists:
                        throw new Exceptions.UnifiedIOException(string.Format("The directory {0} already exists",
                            newPath));

                    case CollisionOption.OpenIfExists:
                    {
                        // Do nothing...
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException("option", option, null);
                }
            }
            else
            {
                Directory.CreateDirectory(newPath);
            }

            return new DotNetDirectory(newPath);
        }

        public async Task<IDirectory> GetFolderAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            string path = System.IO.Path.Combine(Path, name);
            return new DotNetDirectory(path);
        }

        public async Task<IList<IDirectory>> GetFoldersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);
            EnsureExists();

            IList<IDirectory> directories = Directory.GetDirectories(Path)
                .Select(d => new DotNetDirectory(d))
                .ToList<IDirectory>()
                .AsReadOnly();

            return directories;
        }

        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);
            Directory.Delete(_path, true);
        }

        private void CreateFile(string filePath)
        {
            using (var stream = File.Create(filePath))
            {
            }
        }

        private void EnsureExists()
        {
            if (!Directory.Exists(_path))
            {
                throw new Exceptions.DirectoryNotFoundException(this);
            }
        }
    }
}