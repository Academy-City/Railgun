using System;
using System.IO;
using System.Reflection;
using System.Text;
using Cocona;
using Railgun.Grammar;
using Railgun.Grammar.Sweet;
using Railgun.Runtime;

namespace Railgun
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Console.InputEncoding = Encoding.Unicode;
            CoconaApp.Run<Program>(args);
        }

        [Command("repl")]
        public void Repl()
        {
            var version = Assembly.GetExecutingAssembly().
                GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var runtime = new RailgunRuntime();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Welcome to the Railgun Interactive Environment version {version}!");
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("⚡ ");
                Console.ForegroundColor = ConsoleColor.White;
                var text = Console.ReadLine();
                if (text == ".exit")
                {
                    throw new CommandExitedException(0);
                }

                try
                {
                    var exs = new SweetParser(text).ParseSweetProgram();
                    if (exs.Length == 1)
                    {
                        var x = runtime.Eval(exs[0], topLevel: true);
                        if (x != null)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(RailgunLibrary.Repr(x));
                        }
                    }
                    else
                    {
                        runtime.RunProgram(exs);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                }
            }
        }

        private static object[] ReadProgram(string path)
        {
            var program = File.ReadAllText(path);
            return new Parser(program).ParseProgram();
        }
        
        [Command("run")]
        public void Run(string entry = "./main")
        {
            var workingDir = Directory.GetCurrentDirectory();
            var runtime = new RailgunRuntime(workingDir);

            entry = Path.Join(workingDir, entry);
            var program = ProgramLoader.LoadProgram(entry);
            
            runtime.RunProgram(program);
        }
    }
}