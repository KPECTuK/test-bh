// hook via ILPostProcessor from Unity 2020.3+
// (2020.1 has errors https://github.com/vis2k/Mirror/issues/2912)
#if UNITY_2020_3_OR_NEWER
// Unity.CompilationPipeline reference is only resolved if assembly name is
// Unity.*.CodeGen:
// https://forum.unity.com/threads/how-does-unity-do-codegen-and-why-cant-i-do-it-myself.853867/#post-5646937
using System;
using System.IO;
using System.Linq;
// to use Mono.CecilX here, we need to 'override references' in the
// Unity.Mirror.CodeGen assembly definition file in the Editor, and add CecilX.
// otherwise we get a reflection exception with 'file not found: CecilX'.using System.Linq;
using Mono.CecilX;
using Mono.CecilX.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;
// IMPORTANT: 'using UnityEngine' does not work in here.
// Unity gives "(0,0): error System.Security.SecurityException: ECall methods must be packaged into a system module."
//using UnityEngine;

namespace Mirror.Weaver
{
	public class ILPostProcessorHook : ILPostProcessor
	{
		// from CompilationFinishedHook
		private const string MIRROR_RUNTIME_ASSEMBLY_NAME = "Mirror";

		// ILPostProcessor is invoked by Unity.
		// we can not tell it to ignore certain assemblies before processing.
		// add a 'ignore' define for convenience.
		// => WeaverTests/WeaverAssembler need it to avoid Unity running it
		public const string IGNORE_DEFINE = "ILPP_IGNORE";

		// we can't use Debug.Log in ILPP, so we need a custom logger
		private readonly ILPostProcessorLogger _log = new();

		// ???
		public override ILPostProcessor GetInstance()
		{
			return this;
		}

		// check if assembly has the 'ignore' define
		private static bool HasDefine(ICompiledAssembly assembly, string define)
		{
			return
				assembly.Defines != null &&
				assembly.Defines.Contains(define);
		}

		// process Mirror, or anything that references Mirror
		public override bool WillProcess(ICompiledAssembly compiledAssembly)
		{
			// compiledAssembly.References are file paths:
			//   Library/Bee/artifacts/200b0aE.dag/Mirror.CompilerSymbols.dll
			//   Assets/Mirror/Plugins/Mono.Cecil/Mono.CecilX.dll
			//   /Applications/Unity/Hub/Editor/2021.2.0b6_apple_silicon/Unity.app/Contents/NetStandard/ref/2.1.0/netstandard.dll
			//
			// log them to see:
			//foreach(var reference in compiledAssembly.References)
			//{
			//	_log.Warning($"{compiledAssembly.Name} references {reference}");
			//}

			var relevant =
				compiledAssembly.Name == MIRROR_RUNTIME_ASSEMBLY_NAME ||
				compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == MIRROR_RUNTIME_ASSEMBLY_NAME);
			var ignore = HasDefine(compiledAssembly, IGNORE_DEFINE);
			var result = relevant && !ignore;
			if(result)
			{
				_log.Warning($"assembly include: {compiledAssembly.Name} defines: {string.Join(", ", compiledAssembly.Defines ?? Array.Empty<string>())}");
			}
			else
			{
				if(compiledAssembly.Name.Contains("BH"))
				{
					_log.Warning($"assembly skipped: {compiledAssembly.Name} defines: {string.Join(", ", compiledAssembly.Defines ?? Array.Empty<string>())}");
				}
			}

			return result;
		}

		public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
		{
			// load the InMemoryAssembly peData into a MemoryStream
			var peData = compiledAssembly.InMemoryAssembly.PeData;
			//LogDiagnostics($"  peData.Length={peData.Length} bytes");
			using(var stream = new MemoryStream(peData))
			{
				using(var asmResolver = new ILPostProcessorAssemblyResolver(compiledAssembly, _log))
				{
					// we need to load symbols. otherwise we get:
					// "(0,0): error Mono.CecilX.Cil.SymbolsNotFoundException: No symbol found for file: "
					using(var symbols = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData))
					{
						var readerParameters = new ReaderParameters
						{
							SymbolStream = symbols,
							ReadWrite = true,
							ReadSymbols = true,
							AssemblyResolver = asmResolver,
							// custom reflection importer to fix System.Private.CoreLib
							// not being found in custom assembly resolver above.
							ReflectionImporterProvider = new ILPostProcessorReflectionImporterProvider()
						};

						using(var asmDef = AssemblyDefinition.ReadAssembly(stream, readerParameters))
						{
							// resolving a Mirror.dll type like NetworkServer while
							// weaving Mirror.dll does not work. it throws a
							// NullReferenceException in WeaverTypes.ctor
							// when Resolve() is called on the first Mirror type.
							// need to add the AssemblyDefinition itself to use.
							asmResolver.SetAssemblyDefinitionForCompiledAssembly(asmDef);

							// weave this assembly.
							var weaver = new Weaver(_log);
							if(weaver.Weave(asmDef, asmResolver, out var modified))
							{
								// _log.Warning($"Weaving succeeded for: {compiledAssembly.Name}");

								// write if modified
								if(modified)
								{
									// when weaving Mirror.dll with ILPostProcessor,
									// Weave() -> WeaverTypes -> resolving the first
									// type in Mirror.dll adds a reference to
									// Mirror.dll even though we are in Mirror.dll.
									// -> this would throw an exception:
									//    "Mirror references itself" and not compile
									// -> need to detect and fix manually here
									if(asmDef.MainModule.AssemblyReferences.Any(r => r.Name == asmDef.Name.Name))
									{
										asmDef.MainModule.AssemblyReferences.Remove(asmDef.MainModule.AssemblyReferences.First(r => r.Name == asmDef.Name.Name));
										//_log.Warning($"fixed self referencing Assembly: {asmDef.Name.Name}");
									}

									var peOut = new MemoryStream();
									var pdbOut = new MemoryStream();
									var writerParameters = new WriterParameters
									{
										SymbolWriterProvider = new PortablePdbWriterProvider(),
										SymbolStream = pdbOut,
										WriteSymbols = true
									};

									asmDef.Write(peOut, writerParameters);

									var inMemory = new InMemoryAssembly(peOut.ToArray(), pdbOut.ToArray());
									return new ILPostProcessResult(inMemory, _log.Logs);
								}
							}
							// if anything during Weave() fails, we log an error.
							// don't need to indicate 'weaving failed' again.
							// in fact, this would break tests only expecting certain errors.
							//else _log.Error($"Weaving failed for: {compiledAssembly.Name}");
						}
					}
				}
			}

			// always return an ILPostProcessResult with Logs.
			// otherwise we won't see Logs if weaving failed.
			return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, _log.Logs);
		}
	}
}
#endif
