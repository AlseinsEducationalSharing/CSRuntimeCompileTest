using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Alsein.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSRuntimeCompileTest
{
    public class Compiler
    {
        private static IEnumerable<MetadataReference> _references = (from path in Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location), "*.dll")
                                                                     where IsManagedAssembly(path) || path.EndsWith("System.Private.CoreLib.dll")
                                                                     select MetadataReference.CreateFromFile(path)).ToArray();

        private static IEnumerable<MetadataReference> References
        {
            get
            {
                if (!(_references is MetadataReference[]))
                {
                    _references = _references.ToArray();
                }
                return _references;
            }
        }

        public static Action<string[]> Compile(string code, params Assembly[] extraReferences) => Compile(code, (IEnumerable<Assembly>)extraReferences);

        public static Action<string[]> Compile(string code, IEnumerable<Assembly> extraReferences)
        {
            using (var dll = new MemoryStream())
            using (var pdb = new MemoryStream())
            {
                var result = CSharpCompilation.Create("Test")
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                .AddReferences(References.Union(extraReferences.Select(x => MetadataReference.CreateFromFile(x.Location))))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code))
                .Emit(dll, pdb);
                if (!result.Success)
                {
                    throw new Exception(result.Diagnostics.Select(x => x.ToString()).Join("\n"));
                }
                var assembly = Assembly.Load(dll.ToArray(), pdb.ToArray());
                var main = assembly.EntryPoint;
                if (main.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(string[]) }))
                {
                    return (Action<string[]>)main.CreateDelegate(typeof(Action<string[]>));
                }
                else
                {
                    var de = (Action)main.CreateDelegate(typeof(Action));
                    return x => de();
                }
            }
        }

        private static bool IsManagedAssembly(string fileName)
        {
            using (Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                if (fileStream.Length < 64)
                {
                    return false;
                }

                fileStream.Position = 0x3C;
                uint peHeaderPointer = binaryReader.ReadUInt32();
                if (peHeaderPointer == 0)
                {
                    peHeaderPointer = 0x80;
                }

                if (peHeaderPointer > fileStream.Length - 256)
                {
                    return false;
                }

                fileStream.Position = peHeaderPointer;
                uint peHeaderSignature = binaryReader.ReadUInt32();
                if (peHeaderSignature != 0x00004550)
                {
                    return false;
                }

                fileStream.Position += 20;

                const ushort PE32 = 0x10b;
                const ushort PE32Plus = 0x20b;

                var peFormat = binaryReader.ReadUInt16();
                if (peFormat != PE32 && peFormat != PE32Plus)
                {
                    return false;
                }

                ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
                fileStream.Position = dataDictionaryStart;

                uint cliHeaderRva = binaryReader.ReadUInt32();
                if (cliHeaderRva == 0)
                {
                    return false;
                }

                return true;
            }
        }
    }
}