#region Using

using System;
using System.Drawing;

#endregion

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

        public static Point UpLeft(this Point point)
        {
            return new Point(point.X - 1, point.Y + 1);
        }

        public static Point UpRight(this Point point)
        {
            return new Point(point.X + 1, point.Y + 1);
        }

        public static Point DownLeft(this Point point)
        {
            return new Point(point.X - 1, point.Y - 1);
        }

        public static Point DownRight(this Point point)
        {
            return new Point(point.X + 1, point.Y - 1);
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
            return cells.At(point.X, point.Y);
        }

        public static CellType At(this CellType[,] cells, int x, int y)
        {
            if (x < 0 || y < 0 || x >= cells.GetLength(0) || y >= cells.GetLength(1))
                return CellType.Invalid;

            return cells[x, y];
        }

        public static void Set(this CellType[,] cells, Point point, CellType type)
        {
            cells.Set(point.X, point.Y, type);
        }

        public static void Set(this CellType[,] cells, int x, int y, CellType type)
        {
            if (x < 0 || y < 0 || x >= cells.GetLength(0) || y >= cells.GetLength(1))
                throw new ArgumentException();

            cells[x, y] = type;
        }

        public static bool IsValidMove(this CellType[,] cells, Point start, Point end)
        {
            var startType = cells.At(start);
            var endType = cells.At(end);

            if (!startType.IsTraversible())
                return false;

            if (endType.IsTraversible())
            {
                var endu = cells.At(end.Up());

                // don't let rocks fall on us
                if (endType.IsEmpty() && endu.IsRock())
                    return false;

                var startu = cells.At(start.Up());

                // even if we're moving down
                if (startu.IsRock() && start.Down() == end)
                    return false;

                var endur = cells.At(end.UpRight());
                var endr = cells.At(end.Right());

                // diagonals too!
                if (endur.IsRock() && endu.IsEmptyOrRobot() && endr.IsRock())
                    return false;

                var endul = cells.At(end.UpLeft());
                var endl = cells.At(end.Left());

                // diagonals too!
                if (endul.IsRock() && endu.IsEmptyOrRobot() && (endl.IsRock() || endl.IsLambda()))
                    return false;

                // don't unwedge when moving down
                if (start.Down() == end)
                {
                    var startur = cells.At(start.UpRight());
                    var startr = cells.At(start.Right());
                    if (startur.IsRock() && startu.IsEmptyOrRobot() && startr.IsRock())
                        return false;

                    var startul = cells.At(start.UpLeft());
                    var startl = cells.At(start.Left());
                    if (startul.IsRock() && startu.IsEmptyOrRobot() && (startl.IsRock() || startl.IsLambda()))
                        return false;
                }

                // don't let crap fall on you if you can't get away
                if (endu.IsEmptyOrRobot() && cells.At(end.Up().Up()).IsRock())
                    return false;

                // check for horocks
                if (endu.IsHoRock())
                {
                    // check diagonals again
                    if (endul.IsRockOrLambda() && cells.At(end.UpLeft().Up()).IsRock() &&
                        cells.At(end.Up().Up()).IsEmpty())
                        return false;
                }

                if (endu.IsRock() && !endl.IsTraversible() && !endr.IsTraversible())
                    return false;

                if (endType.IsRock())
                {
                    //return false;
                    if (start.Right() == end && !endr.IsEmpty())
                        return false;
                    if (start.Left() == end && !endl.IsEmpty())
                        return false;
                    if (start.Right() != end && start.Left() != end)
                        return false;
                }

                return true;
            }

            return false;
        }

        public static bool MoveDisturbsRock(this CellType[,] cells, Point end)
        {
            var endType = cells.At(end);
            if (endType.IsRock())
                return true;

            if (endType.HoldsRock())
            {
                if (cells.At(end.Up()).IsRock())
                    return true;

                if (cells.At(end.Up().Right()).IsRock() && cells.At(end.Right()).IsRock() &&
                    cells.At(end.Up()).IsEmptyOrRobot())
                    return true;

                if (cells.At(end.Up().Left()).IsRock() && cells.At(end.Left()).IsRockOrLambda() &&
                    cells.At(end.Up()).IsEmptyOrRobot())
                    return true;
            }

            return false;
        }

        public static bool MoveDisturbsBeard(this CellType[,] cells, Point end)
        {
            return cells.At(end.Up()).IsBeard() || cells.At(end.Down()).IsBeard() || cells.At(end.Left()).IsBeard() ||
                   cells.At(end.Right()).IsBeard() || cells.At(end.UpLeft()).IsBeard() ||
                   cells.At(end.UpRight()).IsBeard() || cells.At(end.DownLeft()).IsBeard() ||
                   cells.At(end.DownRight()).IsBeard();
        }

        public static bool IsRock(this CellType cell)
        {
            return cell == CellType.Rock || cell.IsHoRock();
        }

        public static bool IsHoRock(this CellType cell)
        {
            return cell == CellType.HoRock;
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
            return cell >= (CellType) 'A' && cell < (CellType) 'I';
        }

        public static bool IsTarget(this CellType cell)
        {
            return cell >= (CellType) '1' && cell <= (CellType) '9';
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
            return cell.IsRock() || cell.IsTrampoline() || cell.IsTarget() || cell.IsLambda() || cell.IsRobot() ||
                   cell.IsEarth() || cell.IsBeard() || cell.IsRazor();
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

        public static bool IsRazor(this CellType cell)
        {
            return cell == CellType.Razor;
        }

        public static bool IsTraversible(this CellType cell)
        {
            return cell.IsLambda() || cell.IsEmpty() || cell.IsEarth() || cell == CellType.OpenLift || cell.IsRobot() ||
                   cell.IsRock() || cell.IsTrampoline() || cell.IsRazor() || cell.IsTarget();
        }

        public static bool IsClearable(this CellType cell)
        {
            return cell.IsLambda() || cell.IsEmpty() || cell.IsEarth() || cell.IsTrampoline() || cell.IsRobot() ||
                   cell.IsRazor();
        }

        public static bool IsOpenLift(this CellType cell)
        {
            return cell == CellType.OpenLift;
        }

        public static bool IsClosedLift(this CellType cell)
        {
            return cell == CellType.ClosedLift;
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
