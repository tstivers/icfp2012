using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
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
        TrampolineIn = '@',
        TrampolineOut = '?'
    }

    public enum RobotCommand
    {
        Left = 'L',
        Right = 'R',
        Up = 'U',
        Down = 'D',
        Wait = 'W',
        Abort = 'A'
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
        public string MapState { get; private set; }
        public InvalidMoveException(Point point, string mapState)
            : base(String.Format("Robot tried to move to invalid point: {0}", point))
        {
            MapState = mapState;
        }
    }

    public class Map
    {
        private CellType[,] _cells;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Point RobotPosition { get; private set; }
        public List<Point> Lifts { get; private set; }
        public List<Point> Lambdas { get; private set; }
        public CellType[,] Cells { get { return _cells; } }
        public MapState State { get; private set; }
        public bool RocksMoved { get; private set; }
        public HashSet<Point> PriorityLambdas { get; set; }
        public HashSet<Point> Rocks { get; private set; }
        public int Score { get; private set; }
        public int LambdasCollected { get; private set; }
        public Point Target { get; set; }
        public bool StuffChanged { get; private set; }
        public Dictionary<Point, Point> Trampolines { get; private set; }

        public new string ToString()
        {
            var sb = new StringBuilder();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (new Point(x, Height - (y + 1)) != Target)
                        sb.Append((char)_cells[x, Height - (y + 1)]);
                    else
                        sb.Append('+');
                }
                sb.Append('\n');
            }
            sb.Append(String.Format("\nTarget: {0}\n", Target));
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
            PriorityLambdas = new HashSet<Point>(map.PriorityLambdas);
            _cells = new CellType[map.Cells.GetLength(0), map.Cells.GetLength(1)];
            Array.Copy(map.Cells, _cells, _cells.Length);
            Width = map.Width;
            Height = map.Height;
            RobotPosition = map.RobotPosition;
            State = map.State;
            Rocks = map.Rocks;
            Score = map.Score;
            LambdasCollected = map.LambdasCollected;
        }

        public Map(string[] lines)
        {
            State = MapState.Valid;
            Lambdas = new List<Point>();
            Lifts = new List<Point>();
            PriorityLambdas = new HashSet<Point>();
            Rocks = new HashSet<Point>();

            Height = lines.TakeWhile(x => x.Length > 0).Count();
            Width = lines.Max(x => x.Length);
            _cells = new CellType[Width, Height];

            lines.TakeWhile(x => x.Length > 0).
                Reverse().ForEachWithIndex((line, y) =>
                                               {
                                                   line.ForEachWithIndex((cell, x) =>
                                                                             {
                                                                                 var cellType = (CellType)cell;
                                                                                 _cells[
                                                                                     x, y
                                                                                     ] =
                                                                                     (CellType)cell;
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
                                                                                         goto case CellType.OpenLift;
                                                                                     case CellType.OpenLift:
                                                                                         Lifts.Add(new Point(x, y));
                                                                                         break;
                                                                                 }
                                                                             });
                                                   for (int i = line.Length; i < Width; i++)
                                                       _cells[i, y] = CellType.Empty;
                                               });
        }

        public int AbortScore
        {
            get
            {
                return Score + LambdasCollected * 25;
            }
        }

        public RobotCommand ExecuteTurn(RobotCommand command)
        {
            RocksMoved = false;
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

            Score -= 1;
            Simulate();
            return command;
        }

        private void MoveRobotTo(RobotCommand direction, Point destPos)
        {
            StuffChanged = false;
            var destType = Cells.At(destPos);

            if (destType == CellType.Invalid || destType == CellType.Wall || destType == CellType.ClosedLift)
                throw new InvalidMoveException(destPos, ToString());

            if (destType == CellType.Rock)
            {
                if (direction == RobotCommand.Left && Cells.At(destPos.Left()) == CellType.Empty)
                {
                    Cells.Set(destPos.Left(), CellType.Rock);
                    Rocks.Remove(destPos);
                    Rocks.Add(destPos.Left());
                    RocksMoved = true;
                }
                else if (direction == RobotCommand.Right && Cells.At(destPos.Right()) == CellType.Empty)
                {
                    Cells.Set(destPos.Right(), CellType.Rock);
                    Rocks.Remove(destPos);
                    Rocks.Add(destPos.Right());
                    RocksMoved = true;
                }
                else
                {
                    throw new InvalidMoveException(destPos, ToString());
                }
            }

            if (destType == CellType.Lambda)
            {
                Lambdas.Remove(destPos);
                PriorityLambdas.Remove(destPos);
                Score += 25;
                LambdasCollected++;
                StuffChanged = true;
            }

            if (destType == CellType.Earth)
                StuffChanged = true;

            if (destType == CellType.OpenLift)
            {
                State = MapState.Won;
                Score += LambdasCollected * 50;
            }

            Cells.Set(RobotPosition, CellType.Empty);
            Cells.Set(destPos, CellType.Robot);
            RobotPosition = destPos;
        }

        private void Simulate()
        {
            if (State != MapState.Valid)
                return;

            var newState = new CellType[Width, Height];
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var current = new Point(x, y);
                    var currentType = Cells.At(current);
                    newState.Set(current, currentType);

                    if (Cells.At(current) == CellType.ClosedLift && Lambdas.Count == 0)
                        newState.Set(current, CellType.OpenLift);

                    if (Cells.At(current).IsRock())
                    {
                        // If (x; y) contains a Rock, and (x; y - 1) is Empty:                        
                        if (Cells.At(current.Down()).IsEmpty())
                        {
                            // – (x; y) is updated to Empty, (x; y - 1) is updated to Rock.
                            newState.Set(current, CellType.Empty);
                            newState.Set(current.Down(), CellType.Rock);
                            RocksMoved = true;
                            Rocks.Remove(current);
                            Rocks.Add(current.Down());
                        }
                        else if (Cells.At(current.Down()).IsRock())
                        {
                            // If (x; y) contains a Rock, (x; y - 1) contains a Rock, (x + 1; y) is Empty and (x + 1; y - 1) is Empty:
                            if (Cells.At(current.Right()).IsEmpty() && Cells.At(current.Right().Down()).IsEmpty())
                            {
                                // (x; y) is updated to Empty, (x + 1; y - 1) is updated to Rock
                                newState.Set(current, CellType.Empty);
                                newState.Set(current.Right().Down(), CellType.Rock);
                                RocksMoved = true;
                                Rocks.Remove(current);
                                Rocks.Add(current.Right().Down());
                            }
                            // If (x; y) contains a Rock, (x; y - 1) contains a Rock, either (x + 1; y) is not Empty or (x + 1; y - 1) is not Empty, (x - 1; y) is Empty and (x - 1; y - 1) is Empty:
                            else if ((!Cells.At(current.Right()).IsEmpty() || !Cells.At(current.Right().Down()).IsEmpty()) &&
                                     Cells.At(current.Left()).IsEmpty() && Cells.At(current.Left().Down()).IsEmpty())
                            {
                                // (x; y) is updated to Empty, (x - 1; y - 1) is updated to Rock
                                newState.Set(current, CellType.Empty);
                                newState.Set(current.Left().Down(), CellType.Rock);
                                RocksMoved = true;
                                Rocks.Remove(current);
                                Rocks.Add(current.Left().Down());
                            }
                        }
                        //  If (x; y) contains a Rock, (x; y - 1) contains a Lambda, (x + 1; y) is Empty and (x + 1; y - 1) is Empty:
                        else if (Cells.At(current.Down()).IsLambda() && Cells.At(current.Right()).IsEmpty() && Cells.At(current.Right().Down()).IsEmpty())
                        {
                            // (x; y) is updated to Empty, (x + 1; y  1) is updated to Rock.
                            newState.Set(current, CellType.Empty);
                            newState.Set(current.Right().Down(), CellType.Rock);
                            RocksMoved = true;
                            Rocks.Remove(current);
                            Rocks.Add(current.Right().Down());
                        }
                    }
                }
            }

            if (newState.At(RobotPosition.Up()).IsRock() && !Cells.At(RobotPosition.Up()).IsRock())
                State = MapState.Killed;

            _cells = newState;
        }

        public bool IsValidCommand(RobotCommand command)
        {
            switch (command)
            {
                case RobotCommand.Up:
                    return Cells.IsValidMove(RobotPosition, RobotPosition.Up());
                case RobotCommand.Down:
                    return Cells.IsValidMove(RobotPosition, RobotPosition.Down());
                case RobotCommand.Left:
                    return Cells.IsValidMove(RobotPosition, RobotPosition.Left());
                case RobotCommand.Right:
                    return Cells.IsValidMove(RobotPosition, RobotPosition.Right());
                default:
                    return true;
            }
        }

        public Point[] Neighbors(Point point)
        {
            var neighbors = new List<Point>();

            if (Cells.IsValidMove(point, point.Up()))
                neighbors.Add(point.Up());

            if (Cells.IsValidMove(point, point.Down()))
                neighbors.Add(point.Down());

            if (Cells.IsValidMove(point, point.Left()))
                neighbors.Add(point.Left());

            if (Cells.IsValidMove(point, point.Right()))
                neighbors.Add(point.Right());

            return neighbors.ToArray();
        }

        public HashSet<Point> MoveableRocks
        {
            get
            {
                var mrocs = new HashSet<Point>();
                Rocks.Where(rock => Cells.IsLeftMoveable(rock) || Cells.IsRightMoveable(rock) || Cells.At(rock.Down()).IsTraversible()).ForEachWithIndex((x, index) => mrocs.Add(x));
                return mrocs;
            }
        }
    }
}
