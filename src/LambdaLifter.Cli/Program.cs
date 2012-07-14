using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LambdaLifter.Model;
using LamddaLifter.Controller;

namespace LambdaLifter.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
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

            var map = new Map(mapText);
            var controller = new SimpleAStarController(map);
            string lastState = null;
            var sw = new Stopwatch();
            sw.Start();

            while (map.State == MapState.Valid && sw.ElapsedMilliseconds < 120 * 1000)
            {
                Console.Clear();
                lastState = map.ToString();
                Console.Write(map.ToString());                
                map.ExecuteTurn(controller.GetNextMove());
                //Thread.Sleep(100);
            }
            Console.Clear();
            Console.WriteLine(map.ToString());
            Console.WriteLine("MapState: {0}", map.State);
        }
    }
}
