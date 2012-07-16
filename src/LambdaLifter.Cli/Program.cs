using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using LambdaLifter.Model;
using LambdaLifter.Controller;

namespace LambdaLifter.Cli
{
    internal class Program
    {
        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        private static void Main(string[] args)
        {
            string[] mapText;
            bool contest = false;
            bool debug = false;
            int sleep = 0;
            const int timelimit = 140;
            int bestTurn = 0;
            int bestScore = 0;
            bool abort = true;
            var sw = new Stopwatch();
            sw.Start();

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
                if (!File.Exists(args[0]))
                    return;

                mapText = File.ReadAllLines(args[0]);
            }

            if (args.Length > 1)
            {
                debug = true;
                sleep = int.Parse(args[1]);
            }

            var map = new Map(mapText);
            int maxMoves = map.Width * map.Height;

            if (args.Length > 2)
            {
                maxMoves = int.Parse(args[2]);
            }

            var tempMap = map.Clone();
            var tempController = new SimpleAStarController(tempMap);

            while (tempMap.State == MapState.Valid && (debug || sw.ElapsedMilliseconds < timelimit * 1000) &&
                   tempMap.Moves.Count < maxMoves)
            {
                tempMap.ExecuteTurn(tempController.GetNextMove());
                if (debug)
                {
                    SafeClear();
                    tempMap.DumpState();
                    Thread.Sleep(sleep);
                }

                if (tempMap.AbortScore > bestScore && tempMap.State == MapState.Valid)
                {
                    bestTurn = tempMap.Moves.Count;
                    bestScore = tempMap.AbortScore;
                }
                else if (tempMap.State == MapState.Won && tempMap.Score > bestScore)
                {
                    bestTurn = tempMap.Moves.Count;
                    bestScore = tempMap.Score;
                    abort = false;
                }
            }            

            if (contest)
            {
                for (int i = 0; i < bestTurn; i++)
                {
                    Console.Write((char)tempMap.Moves.Dequeue());
                }

                if (abort)
                    Console.Write((char)RobotCommand.Abort);

                return;
            }

            while (map.State == MapState.Valid && map.Moves.Count <= bestTurn)
            {
                if (map.Moves.Count == bestTurn && abort)
                    map.ExecuteTurn(RobotCommand.Abort);
                else
                    map.ExecuteTurn(tempMap.Moves.Dequeue());

                //if (debug)
                //{
                //    SafeClear();
                //    map.DumpState();
                //    Thread.Sleep(sleep);
                //}
            }

            if (!debug) // runtests
            {
                Console.WriteLine("{0,-20}  Score: {1,5}   Moves: {2,3}   State: {3}",
                                  Path.GetFileName(args[0]),
                                  map.Score,
                                  map.Moves.Count,
                                  tempMap.State);
            }
            else
            {
                SafeClear();
                map.DumpState();
                foreach (var move in map.Moves)
                    Console.Write((char)move);
                Console.WriteLine();
            }
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
