#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using LambdaLifter.Controller;
using LambdaLifter.Model;
using Mono.Unix;
using Mono.Unix.Native;

#endregion

namespace LambdaLifter.Cli
{
    internal class Program
    {
        private static bool _isRedirected;

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        private static bool _done;
        private static volatile bool _signaled;

        private static ManualResetEvent _stop = new ManualResetEvent(false);

        static void TerminateHandler()
        {
            Console.WriteLine("Initializing Handler for SIGINT");
            UnixSignal signal = new UnixSignal(Signum.SIGINT);
           
                while (!signal.WaitOne(100, false) && !_done)
                {
                    Console.WriteLine("Control-C Pressed!");
                    _stop.Set();
                }           

            Console.WriteLine("handler Terminated");
        }

        private static void Main(string[] args)
        {
            var handler = new Thread(TerminateHandler);
            handler.Start();

            string[] mapText;
            var contest = false;
            var debug = false;
            var sleep = 0;
            const int timelimit = 140;
            var bestTurn = 0;
            var bestScore = 0;
            var abort = true;
            var sw = Stopwatch.StartNew();

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
            var maxMoves = map.Width * map.Height;

            if (args.Length > 2)
                maxMoves = int.Parse(args[2]);

            var tempMap = map.Clone();
            var tempController = new SimpleAStarController(tempMap);

            while (tempMap.State == MapState.Valid && (debug || sw.ElapsedMilliseconds < timelimit * 1000) &&
                   tempMap.Moves.Count < maxMoves && !_stop.WaitOne(0))
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

            sw.Stop();
            _done = true;

            if (contest)
            {
                for (var i = 0; i < bestTurn; i++)
                    Console.Write((char) tempMap.Moves.Dequeue());

                if (abort)
                    Console.Write((char) RobotCommand.Abort);

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
                Console.WriteLine("{0,-20}  Score: {1,5}   Moves: {2,3}   State: {3,-7}  Time: {4,3}",
                                  Path.GetFileName(args[0]), map.Score, map.Moves.Count, tempMap.State,
                                  String.Format("{2:d2}:{0}.{1:d4}", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds,
                                                sw.Elapsed.Minutes));
            }
            else
            {
                SafeClear();
                map.DumpState();
                foreach (var move in map.Moves)
                    Console.Write((char) move);
                Console.WriteLine();
            }
        }

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
