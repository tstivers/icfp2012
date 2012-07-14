using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using LambdaLifter.Model;
using LamddaLifter.Controller;
using Mono.Unix;
using Mono.Unix.Native;

namespace LambdaLifter.Cli
{
    class Program
    {
        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        static void Main(string[] args)
        {
            UnixSignal signal = null;
            if (IsRunningOnMono())
                return;

            string[] mapText;            

            if (args.Length < 1)
            {
                var lines = new List<string>();
                string line;
                do
                {
                    line = Console.In.ReadLine();
                    if (line != null)
                        lines.Add(line);
                    mapText = lines.ToArray();
                } while (line != null);
            }
            else
            {
                mapText = File.ReadAllLines(args[0]);
            }

            TextWriter outfile = null;
            if (args.Length > 1)
            {
                outfile = new StreamWriter(args[1]);
            }

            var map = new Map(mapText);
            var controller = new SimpleAStarController(map);            
            var sw = new Stopwatch();
            sw.Start();
            // controller.GenerateMoves();
            int moves = 0;            
            while (map.State == MapState.Valid && sw.ElapsedMilliseconds < 30 * 1000 && moves < map.Width*map.Height && (signal == null || !signal.IsSet))
            {
                Console.Clear();                
                Console.Write(map.ToString());
                if (outfile != null)
                {
                    outfile.Write((char) map.ExecuteTurn(controller.GetNextMove()));
                    outfile.Flush();
                }
                else
                    map.ExecuteTurn(controller.GetNextMove());
                //Thread.Sleep(200);
                moves++;
            }
            Console.Clear();
            Console.WriteLine(map.ToString());
            Console.WriteLine("MapState: {0}", map.State);
            Console.WriteLine("RocksMoved: {0}", map.RocksMoved);
            Console.WriteLine("Moves: {0}/{1}", moves, map.Width*map.Height);
            if (Regex.IsMatch(args[0], @"tests(\\|/)"))
                Console.WriteLine("score (test): {0}", map.Score);
            else
                Console.WriteLine("Score: {0}", map.Score);
        }

    
    }
}
