// based on paul's resolver from
// https://github.com/MirageNet/Mirage/commit/def64cd1db525398738f057b3d1eb1fe8afc540c?branch=def64cd1db525398738f057b3d1eb1fe8afc540c&diff=split
//
// an assembly resolver's job is to open an assembly in case we want to resolve
// a type from it.
//
// for example, while weaving MyGame.dll: if we want to resolve ArraySegment<T>,
// then we need to open and resolve from another assembly (CoreLib).
//
// using DefaultAssemblyResolver with ILPostProcessor throws Exceptions in
// WeaverTypes.cs when resolving anything, for example:
// ArraySegment<T> in Mirror.Tests.Dll.
//
// we need a custom resolver for ILPostProcessor.
#if UNITY_2020_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.CecilX;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Mirror.Weaver
{
	internal class ILPostProcessorAssemblyResolver : IAssemblyResolver
	{
		private readonly Logger _log;
		private readonly string[] _assemblyReferences;
		private readonly Dictionary<string, AssemblyDefinition> _assemblyCache = new();
		private readonly ICompiledAssembly _compiledAssembly;

		private AssemblyDefinition _selfAssembly;

		public ILPostProcessorAssemblyResolver(ICompiledAssembly compiledAssembly, Logger log)
		{
			_compiledAssembly = compiledAssembly;
			_assemblyReferences = compiledAssembly.References;
			_log = log;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Cleanup
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name)
		{
			return Resolve(name, new ReaderParameters(ReadingMode.Deferred));
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			lock(_assemblyCache)
			{
				if(name.Name == _compiledAssembly.Name)
				{
					return _selfAssembly;
				}

				var fileName = FindFile(name);

				if(fileName == null)
				{
					// returning null will throw exceptions in our weaver where.
					// let's make it obvious why we returned null for easier debugging.
					// NOTE: if this fails for "System.Private.CoreLib":
					//       ILPostProcessorReflectionImporter fixes it!
					_log.Warning($"ILPostProcessorAssemblyResolver.Resolve: Failed to find file for {name}");
					return null;
				}
				
				var lastWriteTime = File.GetLastWriteTime(fileName);
				var cacheKey = fileName + lastWriteTime;
				if(_assemblyCache.TryGetValue(cacheKey, out var result))
				{
					return result;
				}

				parameters.AssemblyResolver = this;

				var ms = MemoryStreamFor(fileName);

				var pdb = fileName + ".pdb";
				if(File.Exists(pdb))
				{
					parameters.SymbolStream = MemoryStreamFor(pdb);
				}

				var assemblyDefinition = AssemblyDefinition.ReadAssembly(ms, parameters);
				_assemblyCache.Add(cacheKey, assemblyDefinition);

				return assemblyDefinition;
			}
		}

		// find assemblyname in assembly's references
		public string FindFile(AssemblyNameReference name)
		{
			var fileName = _assemblyReferences.FirstOrDefault(_ => Path.GetFileName(_) == name.Name + ".dll");
			if(fileName != null)
			{
				return fileName;
			}

			// perhaps the type comes from an exe instead
			fileName = _assemblyReferences.FirstOrDefault(_ => Path.GetFileName(_) == name.Name + ".exe");
			if(fileName != null)
			{
				return fileName;
			}

			// Unfortunately the current ICompiledAssembly API only provides direct references.
			// It is very much possible that a postprocessor ends up investigating a type in a directly
			// referenced assembly, that contains a field that is not in a directly referenced assembly.
			// if we don't do anything special for that situation, it will fail to resolve.  We should fix this
			// in the ILPostProcessing API. As a workaround, we rely on the fact here that the indirect references
			// are always located next to direct references, so we search in all directories of direct references we
			// got passed, and if we find the file in there, we resolve to it.
			foreach(var parentDir in _assemblyReferences.Select(Path.GetDirectoryName).Distinct())
			{
				var candidate = Path.Combine(parentDir, name.Name + ".dll");
				if(File.Exists(candidate))
				{
					return candidate;
				}
			}

			return null;
		}

		// open file as MemoryStream
		// attempts multiple times, not sure why..
		private static MemoryStream MemoryStreamFor(string fileName)
		{
			return Retry(10,
				TimeSpan.FromSeconds(1),
				() =>
				{
					byte[] byteArray;
					using(var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						byteArray = new byte[fs.Length];
						var readLength = fs.Read(byteArray, 0, (int)fs.Length);
						if(readLength != fs.Length)
						{
							throw new InvalidOperationException("File read length is not full length of file.");
						}
					}

					return new MemoryStream(byteArray);
				});
		}

		private static MemoryStream Retry(int retryCount, TimeSpan waitTime, Func<MemoryStream> func)
		{
			try
			{
				return func();
			}
			catch(IOException)
			{
				if(retryCount == 0)
				{
					throw;
				}
				Console.WriteLine($"Caught IO Exception, trying {retryCount} more times");
				Thread.Sleep(waitTime);
				return Retry(retryCount - 1, waitTime, func);
			}
		}

		// if the CompiledAssembly's AssemblyDefinition is known, we can add it
		public void SetAssemblyDefinitionForCompiledAssembly(AssemblyDefinition assemblyDefinition)
		{
			_selfAssembly = assemblyDefinition;
		}
	}
}
#endif
