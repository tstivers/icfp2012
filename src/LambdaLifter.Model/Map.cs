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
        Invalid = 'X'
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
        public CellType[,] Cells { get { return _cells;  } }
        public MapState State { get; private set; }
        public bool RocksMoved { get; private set; }

        public new string ToString()
        {
            var sb = new StringBuilder();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    sb.Append((char)_cells[x, Height - (y + 1)]);
                }
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
            _cells = new CellType[map.Cells.GetLength(0), map.Cells.GetLength(1)];
            Array.Copy(map.Cells, _cells, _cells.Length);
            Width = map.Width;
            Height = map.Height;
            RobotPosition = map.RobotPosition;
            State = map.State;
        }

        public Map(string[] lines)
        {
            State = MapState.Valid;
            Lambdas = new List<Point>();
            Lifts = new List<Point>();
                      
            Width = lines.Max(x => x.Length);
            Height = (lines[lines.Length - 1].Length == 0) ? lines.Length - 1 : lines.Length;
            _cells = new CellType[Width,Height];
            for (var y = 0; y < Height; y++)
            {                
                for (var x = 0; x < Width; x++)
                {
                    if (x < lines[Height - (y + 1)].Length)
                    {                        
                        _cells[x, y] = (CellType)lines[Height - (y + 1)][x];
                    }                        
                    else
                    {
                        _cells[x, y] = CellType.Empty;
                    }

                    if (_cells[x, y] == CellType.Robot)
                        RobotPosition = new Point(x, y);

                    if (_cells[x, y] == CellType.Lambda)
                        Lambdas.Add(new Point(x, y));

                    if (_cells[x, y] == CellType.ClosedLift || _cells[x, y] == CellType.OpenLift)
                        Lifts.Add(new Point(x, y));
                }
            }
        }

        public void ExecuteTurn(RobotCommand command)
        {
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
                    break;
            }

            Simulate();
        }

        private void MoveRobotTo(RobotCommand direction, Point destPos)
        {            
            var destType = Cells.At(destPos);

            if (destType == CellType.Invalid || destType == CellType.Wall || destType == CellType.ClosedLift)
                throw new InvalidMoveException(destPos, ToString());

            if (destType == CellType.Rock)
            {               
                if (direction == RobotCommand.Left && Cells.At(destPos.Left()) == CellType.Empty)
                {
                    Cells.Set(destPos.Left(), CellType.Rock);
                }
                else if (direction == RobotCommand.Right && Cells.At(destPos.Right()) == CellType.Empty)
                {
                    Cells.Set(destPos.Right(), CellType.Rock);
                }
                else
                {
                    throw new InvalidMoveException(destPos, ToString());
                }
            }

            if (destType == CellType.Lambda)
            {
                Lambdas.Remove(destPos);
            }

            if (destType == CellType.OpenLift)
            {
                State = MapState.Won;
            }

            Cells.Set(RobotPosition, CellType.Empty);
            Cells.Set(destPos, CellType.Robot);
            RobotPosition = destPos;
        }

        private void Simulate()
        {
            if (State != MapState.Valid)
                return;

            RocksMoved = false;

            var newState = new CellType[Width,Height];
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
                        var dwon = Cells.At(current.Down());
                        if (Cells.At(current.Down()).IsEmpty())
                        {
                            // – (x; y) is updated to Empty, (x; y - 1) is updated to Rock.
                            newState.Set(current, CellType.Empty);
                            newState.Set(current.Down(), CellType.Rock);
                            RocksMoved = true;
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
                            }
                            // If (x; y) contains a Rock, (x; y - 1) contains a Rock, either (x + 1; y) is not Empty or (x + 1; y - 1) is not Empty, (x - 1; y) is Empty and (x - 1; y - 1) is Empty:
                            else if ((!Cells.At(current.Right()).IsEmpty() || !Cells.At(current.Right().Down()).IsEmpty()) &&
                                     Cells.At(current.Left()).IsEmpty() && Cells.At(current.Left().Down()).IsEmpty())
                            {
                                // (x; y) is updated to Empty, (x - 1; y - 1) is updated to Rock
                                newState.Set(current, CellType.Empty);
                                newState.Set(current.Left().Down(), CellType.Rock);
                                RocksMoved = true;
                            }
                        }
                        //  If (x; y) contains a Rock, (x; y - 1) contains a Lambda, (x + 1; y) is Empty and (x + 1; y - 1) is Empty:
                        else if (Cells.At(current.Down()).IsLambda() && Cells.At(current.Right()).IsEmpty() && Cells.At(current.Right().Down()).IsEmpty())
                        {
                            // (x; y) is updated to Empty, (x + 1; y  1) is updated to Rock.
                            newState.Set(current, CellType.Empty);
                            newState.Set(current.Right().Down(), CellType.Rock);
                            RocksMoved = true;
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
            
            if(Cells.IsValidMove(point, point.Up()))
                neighbors.Add(point.Up());

            if (Cells.IsValidMove(point, point.Down()))
                neighbors.Add(point.Down());

            if (Cells.IsValidMove(point, point.Left()))
                neighbors.Add(point.Left());

            if (Cells.IsValidMove(point, point.Right()))
                neighbors.Add(point.Right());

            return neighbors.ToArray();
        }
    }
}
