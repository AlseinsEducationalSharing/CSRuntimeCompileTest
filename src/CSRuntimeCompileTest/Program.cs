using System;
using System.Linq;
using System.CodeDom.Compiler;
using System.Text;
using Microsoft.CSharp;
using Alsein.Utilities;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Collections.Generic;

namespace CSRuntimeCompileTest
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Enter your code (ending with 2 empty lines):");
                var sb = new StringBuilder();
                var blank = false;
                while (true)
                {
                    var input = Console.ReadLine();
                    if (input == string.Empty)
                    {
                        if (!blank)
                        {
                            blank = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    sb.Append(input);
                }
                try
                {
                    var code = sb.ToString();
                    if (code.Trim() == string.Empty)
                    {
                        break;
                    }
                    Console.WriteLine("Compiling...");
                    Compiler.Compile(code)(new string[] { });
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine();
                }
            }
        }


    }
}

