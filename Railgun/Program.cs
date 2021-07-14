using System;
using System.IO;
using Cocona;
using Railgun.Grammar;
using Railgun.Runtime;

namespace Railgun
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CoconaApp.Run<Program>(args);
        }

        [Command("repl")]
        public void Repl()
        {
            var runtime = new RailgunRuntime();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Welcome to the Railgun REPL!");
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("> ");
                Console.ForegroundColor = ConsoleColor.White;
                var text = Console.ReadLine();
                if (text == ".exit")
                {
                    break;
                }

                try
                {
                    var exs = new Parser(text).ParseProgram();
                    if (exs.Length == 1)
                    {
                        var x = runtime.Eval(exs[0], topLevel: true);
                        if (x != null)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine(x);   
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
        public void Run()
        {
            // var l = new Cell(1, new Cell(3, new Cell(5, null)));
            // Console.WriteLine(JsonConvert.SerializeObject(l.ToList()));
            var runtime = new RailgunRuntime();
            var workingDir = Directory.GetCurrentDirectory();
            
            var entry = Path.Join(workingDir, "./main.rg");

            var program = ReadProgram(entry);
            runtime.RunProgram(program);
        }
    }
}