using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using LambdaLifter.Model;

namespace LambdaLifter.Controller
{
    class SimpleAStar
    {
        private readonly Map _map;

        public SimpleAStar(Map map)
        {
            _map = map;
        }

        private class PointComparer : IComparer<Point>
        {
            private Dictionary<Point, float> _f_scores;
 
            public PointComparer(Dictionary<Point, float> f_scores)
            {
                _f_scores = f_scores;
            }

            public int Compare(Point x, Point y)
            {
                return _f_scores[x].CompareTo(_f_scores[y]);
            }
        }

        public Queue<RobotCommand> GetRouteTo(Point goal)
        {
            var start = _map.RobotPosition;

            if (start == goal)
                return null;

            var closed_set = new HashSet<Point>();            
            var came_from = new Dictionary<Point, Point>();
            var g_score = new Dictionary<Point, float>() {{start, 0}};
            var f_score = new Dictionary<Point, float>() {{start, GetDistance(start, goal)}};
            var current = start;                        
            C5.IPriorityQueue<Point> open_set = new C5.IntervalHeap<Point>(new PointComparer(f_score)) {start};            
            
            while (!open_set.IsEmpty)
            {
                current = open_set.DeleteMin();
                if (current == goal)
                    return ReconstructPath(came_from, start, goal);
                closed_set.Add(current);

                foreach (var neighbor in _map.Neighbors(current))
                {
                    if (closed_set.Contains(neighbor))
                        continue;
                    var tentative_g_score = g_score[current] + MoveCost(current, neighbor);

                    var temp = neighbor;
                    var temp1 = temp;
                    if (!open_set.Find(x => x.Equals(temp1), out temp) || tentative_g_score < g_score[neighbor])
                    {
                        g_score[neighbor] = tentative_g_score;
                        f_score[neighbor] = g_score[neighbor] + GetDistance(neighbor, goal);
                        open_set.Add(neighbor);
                        came_from[neighbor] = current;
                    }
                }
            }

            return null;
        }

        private float MoveCost(Point current, Point neighbor)
        {
            switch(_map.Cells.At(neighbor))
            {
                case CellType.Empty:
                    return 0;
                case CellType.Lambda:
                    if (_map.Cells.At(neighbor.Up()) == CellType.Rock)
                        return float.MaxValue;
                    return 0;
                case CellType.Earth:
                    if (_map.Cells.At(neighbor.Up()) == CellType.Rock)
                        return float.MaxValue;
                    return 1;
                case CellType.Rock:
                    return float.MaxValue;
                case CellType.OpenLift:
                    return 0;
                case CellType.ClosedLift:
                    return 100;
                default:
                    return 100;
            }
        }

        private Queue<RobotCommand> ReconstructPath(Dictionary<Point, Point> came_from, Point start, Point current)
        {
            var path = new Queue<RobotCommand>();

            while (current != start)
            {
                var prev = came_from[current];

                if (prev.Up() == current)
                    path.Enqueue(RobotCommand.Up);
                else if (prev.Down() == current)
                    path.Enqueue(RobotCommand.Down);
                else if (prev.Left() == current)
                    path.Enqueue(RobotCommand.Left);
                else if (prev.Right() == current)
                    path.Enqueue(RobotCommand.Right);
                else
                {
                    throw new InvalidMoveException(prev, null);
                }
                current = prev;
            }

            return new Queue<RobotCommand>(path.Reverse());
        }

        private float GetDistance(Point start, Point goal)
        {
            return (float)(Math.Sqrt(Math.Pow(Math.Abs((float)start.X - goal.X), 2) + Math.Pow(Math.Abs((float)start.Y - goal.Y), 2)));
        }
    }
}
