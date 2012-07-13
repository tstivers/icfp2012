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
                throw new InvalidMoveException(point, null);

            cells[point.X, point.Y] = type;
        }      

        public static bool IsRock(this CellType cell)
        {
            return cell == CellType.Rock;
        }

        public static bool IsEmpty(this CellType cell)
        {
            return cell == CellType.Empty;
        }

        public static bool IsLambda(this CellType cell)
        {
            return cell == CellType.Lambda;
        }

        public static bool IsValidMove(this CellType cell)
        {
            return cell == CellType.Lambda || cell == CellType.Empty || cell == CellType.Earth || cell == CellType.OpenLift;
        }

    }
}
