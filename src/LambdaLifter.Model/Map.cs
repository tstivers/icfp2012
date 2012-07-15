using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Text.RegularExpressions;
using LambdaLifter.Utility;

namespace LambdaLifter.Model
{
    public enum CellType
    {
        Robot = 'R',
        Wall = '#',
        Rock = '*',
        Lambda = '\\',
        ClosedLift = 'L',
        OpenLift = 'O',
        Earth = '.',
        Empty = ' ',
        Invalid = 'X',
        Beard = 'W',
        Razor = '!'
    }

    public enum RobotCommand
    {
        Left = 'L',
        Right = 'R',
        Up = 'U',
        Down = 'D',
        Wait = 'W',
        Abort = 'A',
        Shave = 'S'
    }

    public enum MapState
    {
        Valid,
        Aborted,
        Killed,
        Won
    }

    public class InvalidMoveException : Exception
    {
        public Point From { get; private set; }
        public Point To { get; private set; }

        public InvalidMoveException(Point from, Point to)
            : base(String.Format("Robot tried to move from {0} to {1}", from, to))
        {
            From = from;
            To = to;
        }
    }

    public class Map
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Point RobotPosition { get; private set; }
        public List<Point> Lifts { get; private set; }
        public List<Point> Lambdas { get; private set; }
        public CellType[,] Cell { get; private set; }
        public MapState State { get; private set; }
        public bool IsChanged { get; private set; }
        public HashSet<Point> Rocks { get; private set; }
        public int Score { get; private set; }
        public int LambdasCollected { get; private set; }
        public Dictionary<Point, Point> Trampolines { get; private set; }
        public Queue<RobotCommand> Moves { get; private set; }
        
        // flooding stuff
        public int Water { get; private set; }
        public int Flooding { get; private set; }
        public int Waterproof { get; private set; }
        public int Underwater { get; private set; }

        // beard stuff
        public int Growth { get; private set; }
        public int Razors { get; private set; }
        public int GrowthCount { get; private set; }

        public int WaterLevel
        {
            get
            {
                if (Flooding == 0 || Moves.Count <= 1)
                    return Water;
                return Water + ((Moves.Count - 1) / Flooding);
            }
        }

        public bool IsGrowthTurn
        {
            get
            {
                if (Moves.Count <= 1)
                    return false;
                return (Moves.Count % Growth) == 0;
            }
        }

        public new string ToString()
        {
            var sb = new StringBuilder();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                    sb.Append((char) Cell[x, Height - (y + 1)]);

                sb.Append('\n');
            }
            return sb.ToString();
        }

        public Map Clone()
        {
            return new Map(this);
        }

        private Map(Map map)
        {
            Lambdas = new List<Point>(map.Lambdas);
            Lifts = new List<Point>(map.Lifts);
            Cell = new CellType[map.Cell.GetLength(0),map.Cell.GetLength(1)];
            Array.Copy(map.Cell, Cell, Cell.Length);
            Width = map.Width;
            Height = map.Height;
            RobotPosition = map.RobotPosition;
            State = map.State;
            Rocks = new HashSet<Point>(map.Rocks);
            Score = map.Score;
            LambdasCollected = map.LambdasCollected;
            Trampolines = new Dictionary<Point, Point>(map.Trampolines);
            Moves = new Queue<RobotCommand>(map.Moves);
            Water = map.Water;
            Flooding = map.Flooding;
            Waterproof = map.Waterproof;
            Underwater = map.Underwater;
            Growth = map.Growth;
            Razors = map.Razors;
        }

        public Map(string[] lines)
        {
            State = MapState.Valid;
            Lambdas = new List<Point>();
            Lifts = new List<Point>();
            Rocks = new HashSet<Point>();
            Trampolines = new Dictionary<Point, Point>();
            Moves = new Queue<RobotCommand>();
            var trampolineMapping = new List<Pair<Pair<char, Point?>, Pair<char, Point?>>>();
            Water = 0;
            Flooding = 0;
            Waterproof = 10;
            Growth = 25;

            Height = lines.TakeWhile(x => x.Length > 0).Count();
            Width = lines.Max(x => x.Length);
            Cell = new CellType[Width,Height];

            var parser = new Dictionary<Regex, Action<Match>>
                         {
                             {
                                 new Regex(@"Trampoline (.) targets (.)"), match => trampolineMapping.Add(
                                     new Pair<Pair<char, Point?>, Pair<char, Point?>>(
                                         new Pair<char, Point?>(match.Groups[1].Value[0], null),
                                         new Pair<char, Point?>(match.Groups[2].Value[0], null)))
                                 },
                             {new Regex(@"Water ([\d]+)"), match => Water = int.Parse(match.Groups[1].Value)},
                             {new Regex(@"Flooding ([\d]+)"), match => Flooding = int.Parse(match.Groups[1].Value)},
                             {new Regex(@"Waterproof ([\d]+)"), match => Waterproof = int.Parse(match.Groups[1].Value)},
                             {new Regex(@"Growth ([\d]+)"), match => Growth = int.Parse(match.Groups[1].Value)},
                             {new Regex(@"Razors ([\d]+)"), match => Razors = int.Parse(match.Groups[1].Value)},
                         };

            foreach (var line in lines.SkipWhile(x => x.Length > 0))
            {
                foreach (var setter in parser)
                {
                    var match = setter.Key.Match(line);
                    if (match.Success)
                        setter.Value(match);
                }                    
            }

            lines.TakeWhile(x => x.Length > 0).Reverse().ForEachWithIndex((line, y) =>
            {
                line.ForEachWithIndex((cell, x) =>
                {
                    var cellType = (CellType) cell;
                    Cell[x, y] = cellType;

                    switch (cellType)
                    {
                        case CellType.Robot:
                            RobotPosition = new Point(x, y);
                            break;
                        case CellType.Lambda:
                            Lambdas.Add(new Point(x, y));
                            break;
                        case CellType.Rock:
                            Rocks.Add(new Point(x, y));
                            break;
                        case CellType.ClosedLift:
                            Lifts.Add(new Point(x, y));
                            break;
                        case CellType.OpenLift:
                            Lifts.Add(new Point(x, y));
                            break;
                    }

                    if (cellType.IsTrampoline())
                    {
                        var index = trampolineMapping.FindIndex(t => t.first.first == cell);
                        if (index != -1)
                            trampolineMapping[index].first.second = new Point(x, y);
                    }

                    if (cellType.IsTarget())
                    {
                        trampolineMapping.Where(t => t.second.first == cell).ForEachWithIndex(
                            (t, v) => t.second.second = new Point(x, y));
                    }
                });

                for (var i = line.Length; i < Width; i++)
                    Cell[i, y] = CellType.Empty;
            });

            // set up trampoline map
            foreach (var mapping in trampolineMapping)
            {
                // ignore incomplete mappings
                if (!(mapping.first.second.HasValue && mapping.second.second.HasValue))
                    continue;

                Trampolines.Add(mapping.first.second.Value, mapping.second.second.Value);
            }
        }

        public int AbortScore
        {
            get { return Score + LambdasCollected*25; }
        }

        public RobotCommand ExecuteTurn(RobotCommand command)
        {
            IsChanged = false;
            Moves.Enqueue(command);

            switch (command)
            {
                case RobotCommand.Up:
                    MoveRobotTo(RobotCommand.Up, RobotPosition.Up());
                    break;
                case RobotCommand.Down:
                    MoveRobotTo(RobotCommand.Down, RobotPosition.Down());
                    break;
                case RobotCommand.Left:
                    MoveRobotTo(RobotCommand.Left, RobotPosition.Left());
                    break;
                case RobotCommand.Right:
                    MoveRobotTo(RobotCommand.Right, RobotPosition.Right());
                    break;
                case RobotCommand.Abort:
                    State = MapState.Aborted;
                    Score = AbortScore;
                    break;
            }

            if (State == MapState.Valid)
            {
                Score -= 1;
                Simulate();
            }

            return command;
        }

        private void MoveRobotTo(RobotCommand direction, Point destPos)
        {
            var destType = Cell.At(destPos);

            if (!destType.IsTraversible())
                throw new InvalidMoveException(RobotPosition, destPos);

            if (!destType.IsEmpty())
                IsChanged = true;

            if (destType.IsRock())
            {
                if (direction == RobotCommand.Left && Cell.At(destPos.Left()).IsEmpty())
                {
                    Cell.Set(destPos.Left(), CellType.Rock);
                    Rocks.Remove(destPos);
                    Rocks.Add(destPos.Left());
                }
                else if (direction == RobotCommand.Right && Cell.At(destPos.Right()).IsEmpty())
                {
                    Cell.Set(destPos.Right(), CellType.Rock);
                    Rocks.Remove(destPos);
                    Rocks.Add(destPos.Right());
                }
                else
                {
                    throw new InvalidMoveException(RobotPosition, destPos);
                }
            }

            if (destType.IsLambda())
            {
                Lambdas.Remove(destPos);
                Score += 25;
                LambdasCollected++;
            }

            if (destType.IsOpenLift())
            {
                State = MapState.Won;
                Score += LambdasCollected*50 - 1; // minus one point for the final move
            }

            if (destType.IsTrampoline())
            {
                Cell.Set(destPos, CellType.Empty);
                destPos = Trampolines[destPos];
            }

            if (destType.IsRazor())
            {
                Razors++;
            }

            Cell.Set(RobotPosition, CellType.Empty);
            Cell.Set(destPos, CellType.Robot);
            RobotPosition = destPos;
        }

        private void Simulate()
        {
            if (State != MapState.Valid)
                return;

            var newState = new CellType[Width,Height];
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var current = new Point(x, y);
                    var currentType = Cell.At(current);

                    if (newState.At(current) == 0)
                        newState.Set(current, currentType);

                    if (Cell.At(current).IsClosedLift() && Lambdas.Count == 0)
                        newState.Set(current, CellType.OpenLift);

                    if (Cell.At(current).IsBeard() && IsGrowthTurn)
                    {
                        for (int dy = -1; dy < 2; dy++)
                        {
                            for (int dx = -1; dx < 2; dx++)
                            {
                                if (Cell.At(current.X + dx, current.Y + dy).IsEmpty())
                                {
                                    newState.Set(current.X + dx, current.Y + dy, CellType.Beard);
                                    IsChanged = true;
                                }
                            }
                        }
                    }

                    if (Cell.At(current).IsRock())
                    {
                        // If (x; y) contains a Rock, and (x; y - 1) is Empty:                        
                        if (Cell.At(current.Down()).IsEmpty())
                        {
                            // – (x; y) is updated to Empty, (x; y - 1) is updated to Rock.
                            newState.Set(current, CellType.Empty);
                            newState.Set(current.Down(), CellType.Rock);
                            IsChanged = true;
                            Rocks.Remove(current);
                            Rocks.Add(current.Down());
                        }
                        else if (Cell.At(current.Down()).IsRock())
                        {
                            // If (x; y) contains a Rock, (x; y - 1) contains a Rock, (x + 1; y) is Empty and (x + 1; y - 1) is Empty:
                            if (Cell.At(current.Right()).IsEmpty() && Cell.At(current.Right().Down()).IsEmpty())
                            {
                                // (x; y) is updated to Empty, (x + 1; y - 1) is updated to Rock
                                newState.Set(current, CellType.Empty);
                                newState.Set(current.Right().Down(), CellType.Rock);
                                IsChanged = true;
                                Rocks.Remove(current);
                                Rocks.Add(current.Right().Down());
                            }
                                // If (x; y) contains a Rock, (x; y - 1) contains a Rock, either (x + 1; y) is not Empty or (x + 1; y - 1) is not Empty, (x - 1; y) is Empty and (x - 1; y - 1) is Empty:
                            else if ((!Cell.At(current.Right()).IsEmpty() || !Cell.At(current.Right().Down()).IsEmpty()) &&
                                     Cell.At(current.Left()).IsEmpty() && Cell.At(current.Left().Down()).IsEmpty())
                            {
                                // (x; y) is updated to Empty, (x - 1; y - 1) is updated to Rock
                                newState.Set(current, CellType.Empty);
                                newState.Set(current.Left().Down(), CellType.Rock);
                                IsChanged = true;
                                Rocks.Remove(current);
                                Rocks.Add(current.Left().Down());
                            }
                        }
                            //  If (x; y) contains a Rock, (x; y - 1) contains a Lambda, (x + 1; y) is Empty and (x + 1; y - 1) is Empty:
                        else if (Cell.At(current.Down()).IsLambda() && Cell.At(current.Right()).IsEmpty() &&
                                 Cell.At(current.Right().Down()).IsEmpty())
                        {
                            // (x; y) is updated to Empty, (x + 1; y  1) is updated to Rock.
                            newState.Set(current, CellType.Empty);
                            newState.Set(current.Right().Down(), CellType.Rock);
                            IsChanged = true;
                            Rocks.Remove(current);
                            Rocks.Add(current.Right().Down());
                        }
                    }
                }
            }

            if (newState.At(RobotPosition.Up()).IsRock() && !Cell.At(RobotPosition.Up()).IsRock())
                State = MapState.Killed;

            if (RobotPosition.Y < WaterLevel)
            {
                Underwater++;
                if (Underwater > Waterproof)
                {
                    State = MapState.Killed;
                }
            }
            else
            {
                Underwater = 0;
            }

            Cell = newState;
        }

        public bool IsValidCommand(RobotCommand command)
        {
            switch (command)
            {
                case RobotCommand.Up:
                    return Cell.IsValidMove(RobotPosition, RobotPosition.Up());
                case RobotCommand.Down:
                    return Cell.IsValidMove(RobotPosition, RobotPosition.Down());
                case RobotCommand.Left:
                    return Cell.IsValidMove(RobotPosition, RobotPosition.Left());
                case RobotCommand.Right:
                    return Cell.IsValidMove(RobotPosition, RobotPosition.Right());
                default:
                    return true;
            }
        }

        public Point[] Neighbors(Point point)
        {
            var neighbors = new List<Point>();

            if (Cell.At(point).IsTrampoline())
                point = Trampolines[point];

            if (Cell.IsValidMove(point, point.Up()))
                neighbors.Add(point.Up());

            if (Cell.IsValidMove(point, point.Down()))
                neighbors.Add(point.Down());

            if (Cell.IsValidMove(point, point.Left()))
                neighbors.Add(point.Left());

            if (Cell.IsValidMove(point, point.Right()))
                neighbors.Add(point.Right());

            return neighbors.ToArray();
        }

        public HashSet<Point> MoveableRocks
        {
            get
            {
                var mrocs = new HashSet<Point>();
                Rocks.Where(
                    rock =>
                    Cell.IsLeftMoveable(rock) || Cell.IsRightMoveable(rock) || Cell.At(rock.Down()).IsTraversible()).
                    ForEachWithIndex((x, index) => mrocs.Add(x));
                return mrocs;
            }
        }
    }
}
