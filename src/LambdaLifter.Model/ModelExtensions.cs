using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace LambdaLifter.Model
{
    public static class PointExtensions
    {
        public static Point Left(this Point point)
        {
            return new Point(point.X - 1, point.Y);
        }

        public static Point Right(this Point point)
        {
            return new Point(point.X + 1, point.Y);
        }

        public static Point Up(this Point point)
        {
            return new Point(point.X, point.Y + 1);
        }

        public static Point Down(this Point point)
        {
            return new Point(point.X, point.Y - 1);
        }

        public static Point Move(this Point point, RobotCommand direction)
        {
            switch (direction)
            {
                case RobotCommand.Up:
                    return point.Up();
                case RobotCommand.Down:
                    return point.Down();
                case RobotCommand.Left:
                    return point.Left();
                case RobotCommand.Right:
                    return point.Right();
                default:
                    return point;
            }
        }

        public static RobotCommand GetMove(this Point start, Point destination)
        {
            if (start.Up() == destination)
                return RobotCommand.Up;
            if (start.Down() == destination)
                return RobotCommand.Down;
            if (start.Left() == destination)
                return RobotCommand.Left;
            if (start.Right() == destination)
                return RobotCommand.Right;

            throw new InvalidMoveException(start, destination);
        }
    }

    public static class CellExtensions
    {
        public static CellType At(this CellType[,] cells, Point point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= cells.GetLength(0) || point.Y >= cells.GetLength(1))
                return CellType.Invalid;

            return cells[point.X, point.Y];
        }

        public static void Set(this CellType[,] cells, Point point, CellType type)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= cells.GetLength(0) || point.Y >= cells.GetLength(1))
                throw new ArgumentException();

            cells[point.X, point.Y] = type;
        }

        public static bool IsValidMove(this CellType[,] cells, Point start, Point end)
        {
            if (cells.At(end).IsTraversible())
            {
                // don't let rocks fall on us
                if (cells.At(end).IsEmpty() && cells.At(end.Up()).IsRock())
                    return false;

                // even if we're moving down
                if (cells.At(start.Up()).IsRock() && start.Down() == end)
                    return false;

                // diagonals too!
                if (cells.At(end.Up().Right()).IsRock() && cells.At(end.Up()).IsEmptyOrRobot() && cells.At(end.Right()).IsRock())
                    return false;

                // diagonals too!
                if (cells.At(end.Up().Left()).IsRock() && cells.At(end.Up()).IsEmptyOrRobot() && (cells.At(end.Left()).IsRock() || cells.At(end.Left()).IsLambda()))
                {
                    if (!cells.At(end.Left()).IsTraversible() && !cells.At(end.Right()).IsTraversible())
                        return false;

                    return false;
                }

                // don't unwedge when moving down
                if (start.Down() == end)
                {
                    if (cells.At(start.Up().Right()).IsRock() && cells.At(start.Up()).IsEmptyOrRobot() && cells.At(start.Right()).IsRock())
                        return false;

                    if (cells.At(start.Up().Left()).IsRock() && cells.At(start.Up()).IsEmptyOrRobot() && (cells.At(start.Left()).IsRock() || cells.At(start.Left()).IsLambda()))
                        return false;
                }

                // don't let crap fall on you if you can't get away
                if (cells.At(end.Up()).IsEmptyOrRobot() && cells.At(end.Up().Up()).IsRock())
                    return false;

                if (cells.At(end.Up()).IsRock() && !cells.At(end.Left()).IsTraversible() && !cells.At(end.Right()).IsTraversible())
                    return false;

                if (cells.At(end).IsRock())
                {
                    //return false;
                    if (start.Right() == end && !cells.At(end.Right()).IsEmpty())
                        return false;
                    if (start.Left() == end && !cells.At(end.Left()).IsEmpty())
                        return false;
                    if (start.Right() != end && start.Left() != end)
                        return false;
                }

                return true;
            }

            return false;
        }

        public static bool MoveDisturbsRock(this CellType[,] cells, Point start, Point end)
        {
            var startType = cells.At(start);    
            var endType = cells.At(end);
            if (endType.IsRock())
                return true;

            if (endType.HoldsRock())
            {
                if (cells.At(end.Up()).IsRock())
                    return true;

                if (cells.At(end.Up().Right()).IsRock() && cells.At(end.Right()).IsRock() && cells.At(end.Up()).IsEmptyOrRobot())
                    return true;

                if (cells.At(end.Up().Left()).IsRock() && cells.At(end.Left()).IsRockOrLambda() && cells.At(end.Up()).IsEmptyOrRobot())
                    return true;
            }

            return false;
        }

        public static bool IsRock(this CellType cell)
        {
            return cell == CellType.Rock;
        }

        public static bool IsEarth(this CellType cell)
        {
            return cell == CellType.Earth;
        }

        public static bool IsEmpty(this CellType cell)
        {
            return cell == CellType.Empty;
        }

        public static bool IsTrampoline(this CellType cell)
        {
            return cell >= (CellType)'A' && cell < (CellType)'I';
        }

        public static bool IsTarget(this CellType cell)
        {
            return cell >= (CellType)'1' && cell <= (CellType)'9';
        }

        public static bool IsEmptyOrRobot(this CellType cell)
        {
            return cell.IsEmpty() || cell == CellType.Robot;
        }

        public static bool IsRockOrLambda(this CellType cell)
        {
            return cell.IsRock() || cell.IsLambda();
        }

        public static bool HoldsRock(this CellType cell)
        {
            return cell.IsRock() || cell.IsTrampoline() || cell.IsTarget() || cell.IsLambda() || cell.IsRobot() || cell.IsEarth() || cell.IsBeard();
        }

        public static bool IsRobot(this CellType cell)
        {
            return cell == CellType.Robot;
        }

        public static bool IsLambda(this CellType cell)
        {
            return cell == CellType.Lambda;
        }

        public static bool IsBeard(this CellType cell)
        {
            return cell == CellType.Beard;
        }

        public static bool IsTraversible(this CellType cell)
        {
            return cell == CellType.Lambda || cell == CellType.Empty || cell == CellType.Earth || cell == CellType.OpenLift || cell == CellType.Robot || cell == CellType.Rock || cell.IsTrampoline();
        }

        public static bool IsClearable(this CellType cell)
        {
            return cell == CellType.Lambda || cell == CellType.Empty || cell == CellType.Earth || cell.IsTrampoline() || cell.IsRobot();
        }

        public static bool IsLeftMoveable(this CellType[,] cells, Point rock)
        {
            if (!cells.At(rock).IsRock())
                return false;

            if (!cells.At(rock.Left()).IsClearable() || !cells.At(rock.Right()).IsClearable())
                return false;

            return true;
        }

        public static bool IsRightMoveable(this CellType[,] cells, Point rock)
        {
            if (!cells.At(rock).IsRock())
                return false;

            if (!cells.At(rock.Left()).IsClearable() || !cells.At(rock.Right()).IsClearable())
                return false;

            return true;
        }


    }
}
