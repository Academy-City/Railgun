﻿using System;
using System.IO;
using Cocona;
using Railgun.Grammar.Sweet;

namespace Railgun.Grammar
{
    public static class BetterConsole
    {
        public static void PrintColored(ConsoleColor color, string s)
        {
            Console.ForegroundColor = color;
            Console.Write(s);
            Console.ResetColor();
        }
        
        public static void PrintlnColored(ConsoleColor color, string s)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(s);
            Console.ResetColor();
        }
    }
    
    public class ProgramLoader
    {
        private static void PaddedPrint(string[] splitSource, int lineNo)
        {
            var magnitude = splitSource.Length.ToString().Length;
            var p = lineNo.ToString().PadLeft(magnitude, ' ') + " |";
            BetterConsole.PrintColored(ConsoleColor.DarkGray, p);
            Console.WriteLine(splitSource[lineNo - 1]);
        }
        
        private static void PaddedPrint(string[] splitSource, string content)
        {
            var magnitude = splitSource.Length.ToString().Length;
            var p = new string(' ', magnitude) + " |";
            BetterConsole.PrintColored(ConsoleColor.DarkGray, p);
            BetterConsole.PrintColored(ConsoleColor.Red, content);
        }
        
        public static object[] LoadProgram(string path)
        {
            var sourceText = "";
            try
            {
                if (File.Exists(path + ".rgx"))
                {
                    sourceText = File.ReadAllText(path + ".rgx");
                    return new SweetParser(sourceText).ParseSweetProgram();
                }

                sourceText = File.ReadAllText(path + ".rg");
                return new Parser(sourceText).ParseProgram();
            }
            catch (ParseException ex)
            {
                // Console.WriteLine(ex.Index);
                var pos = BaseLexer.CalculatePosition(sourceText, ex.Index);

                var sourceSplit = sourceText.Split("\n");
                
                BetterConsole.PrintColored(ConsoleColor.Red, "Parse Error");
                Console.WriteLine($" on line {pos.Line}, column {pos.Column}: " + ex.Message);
                
                PaddedPrint(sourceSplit, pos.Line);
                PaddedPrint(sourceSplit, new string(' ', pos.Column - 2) + "^\n");
                
                throw new CommandExitedException(1);
            }
        }
    }
}