using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using LambdaLifter.Model;
using LambdaLifter.Controller;

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
            string[] mapText;
            bool contest = false;
            int timelimit = 140;
            int bestTurn = 0;
            int bestScore = 0;
            bool abort = true;

            if (args.Length < 1)
            {
                contest = true;
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

            if (!contest)
                timelimit = 30;

            var map = new Map(mapText);
            int maxMoves = map.Width * map.Height;

            if (args.Length > 2)
            {
                maxMoves = int.Parse(args[2]);
            }

            var controller = new SimpleAStarController(map);
            var sw = new Stopwatch();
            sw.Start();            

            var queue = new Queue<RobotCommand>();
            int moves = 0;
            var tempMap = map.Clone();
            var tempController = new SimpleAStarController(tempMap);

            while (tempMap.State == MapState.Valid && sw.ElapsedMilliseconds < timelimit * 1000 && moves + 1 < maxMoves)
            {
                queue.Enqueue(tempMap.ExecuteTurn(tempController.GetNextMove()));
                moves++;
                if (tempMap.AbortScore > bestScore && tempMap.State == MapState.Valid)
                {
                    bestTurn = moves;
                    bestScore = tempMap.AbortScore;
                    //Console.WriteLine(tempMap.AbortScore);
                }
                else if (tempMap.State == MapState.Won && tempMap.Score > bestScore)
                {
                    bestTurn = moves;
                    bestScore = tempMap.Score;
                    abort = false;
                }
            }

            if (contest)
            {
                for (int i = 0; i < bestTurn; i++)
                {
                    Console.Write((char)queue.Dequeue());
                }

                if(abort)
                    Console.Write((char)RobotCommand.Abort);
                
                return;
            }

            moves = 0;
            while (map.State == MapState.Valid && sw.ElapsedMilliseconds < timelimit * 1000 && moves <= bestTurn)
            {
                if (moves == bestTurn && abort)
                    map.ExecuteTurn(RobotCommand.Abort);
                else
                    map.ExecuteTurn(queue.Dequeue());

                SafeClear();
                Console.Write(map.ToString());
                Console.WriteLine("ControllerState: {0}", controller.State);
                Console.WriteLine("Moves: {0}/{1}", moves, map.Width * map.Height);

                moves++;
            }

            Console.WriteLine("MapState: {0}", map.State);
            if (Regex.IsMatch(args[0], @"tests(\\|/)"))
                Console.WriteLine("score (test): {0}", map.Score);
            else
                Console.WriteLine("Score: {0}", map.Score);

            Console.WriteLine("Best Score: {0}", bestScore);
        }

        private static bool _isRedirected;

        public static void SafeClear()
        {
            if (_isRedirected)
                return;

            try
            {
                Console.Clear();
            }
            catch (IOException)
            {
                _isRedirected = true;
            }
        }
    }
}
