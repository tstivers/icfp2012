using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using LambdaLifter.Model;

namespace LambdaLifter.Controller
{
    public class SimpleAStarController : ControllerBase
    {
        private Queue<RobotCommand> _commands = new Queue<RobotCommand>();
        private RobotCommand _finalCommand = RobotCommand.Abort;
 
        public SimpleAStarController(Map map) : base(map)
        {
        }

        public Queue<RobotCommand> GenerateMoves()
        {
            Map start = Map.Clone();

            var commands = new Queue<RobotCommand>();

            while (Map.State != MapState.Won)
            {
                while (Map.State == MapState.Valid)
                {
                    var move = GetNextMove();
                    commands.Enqueue(move);
                    Map.ExecuteTurn(move);
                }

                if (Map.State == MapState.Won)
                {
                    Map = start.Clone();
                    break;
                }

                var newMap = start.Clone();
                Map.Lambdas.ForEach(x => newMap.PriorityLambdas.Add(x));
                Map = newMap;
                return commands;
            }

            return commands;
        }

        public override RobotCommand GetNextMove()
        {
            //if (_commands != null && _commands.Count > 0 && !Map.RocksMoved)
            //    return _commands.Dequeue();

            if (_commands != null && _commands.Count == 0 && _finalCommand != RobotCommand.Abort)
            {
                var temp = _finalCommand;
                _finalCommand = RobotCommand.Abort;
                return temp;
            }

            _commands = null;

            foreach (var lambda in Map.PriorityLambdas)
            {
                if (Map.Cells.At(lambda.Up()).IsRock())
                    continue;

                var routeFinder = new SimpleAStar(Map.Clone());
                var route = routeFinder.GetRouteTo(lambda);
                if (_commands == null || (route != null && route.Count < _commands.Count))
                    _commands = route;
            }

            if (_commands == null)
            {
                foreach (var lambda in Map.PriorityLambdas)
                {
                    var routeFinder = new SimpleAStar(Map.Clone());
                    var route = routeFinder.GetRouteTo(lambda);
                    if (_commands == null || (route != null && route.Count < _commands.Count))
                        _commands = route;
                }
            }

            if (_commands == null)
            {
                foreach (var lambda in Map.Lambdas)
                {
                    if (Map.Cells.At(lambda.Up()).IsRock())
                        continue;

                    var routeFinder = new SimpleAStar(Map.Clone());
                    var route = routeFinder.GetRouteTo(lambda);
                    if (_commands == null || (route != null && route.Count < _commands.Count))
                        _commands = route;
                }
            }

            if (_commands == null)
            {
                foreach (var lambda in Map.Lambdas)
                {
                    var routeFinder = new SimpleAStar(Map.Clone());
                    var route = routeFinder.GetRouteTo(lambda);
                    if (_commands == null || (route != null && route.Count < _commands.Count))
                        _commands = route;
                }
            }
            
            if (_commands == null && Map.Lambdas.Count == 0)
            {
                var routeFinder = new SimpleAStar(Map.Clone());
                _commands = routeFinder.GetRouteTo(Map.Lifts[0]);
            }

            if (_commands == null)
            {
                var temp = Map.Clone();
                temp.ExecuteTurn(RobotCommand.Wait);
                if (temp.RocksMoved)
                    return RobotCommand.Wait;
            }

            if (_commands == null)
            {
                var routeFinder = new SimpleAStar(Map.Clone());
                foreach (var rock in Map.MoveableRocks)
                {
                    if (Map.Cells.IsValidMove(rock.Left(), rock))
                    {
                        var route = routeFinder.GetRouteTo(rock.Left());
                        if (route != null)
                        {
                            _commands = route;
                            _finalCommand = RobotCommand.Right;
                            break;
                        }
                    }
                    else if (Map.Cells.IsValidMove(rock.Right(), rock))
                    {
                        var route = routeFinder.GetRouteTo(rock.Right());
                        if (route != null)
                        {
                            _commands = route;
                            _finalCommand = RobotCommand.Left;
                            break;
                        }
                    }
                    else if (Map.Cells.At(rock.Right()).IsEarth())
                    {
                        var route = routeFinder.GetRouteTo(rock.Right());
                        if (route != null)
                            _commands = route;
                        break;
                    }
                    else if (Map.Cells.At(rock.Left()).IsEarth())
                    {
                        var route = routeFinder.GetRouteTo(rock.Left());
                        if (route != null)
                            _commands = route;
                        break;
                    }
                    else if (Map.Cells.At(rock.Down()).IsTraversible())
                    {
                        var route = routeFinder.GetRouteTo(rock.Down());
                        if (route != null)
                            _commands = route;                        
                        break;
                    }
                }
            }

            //if (_commands == null)
            //{
            //    foreach (var neighbor in Map.Neighbors(Map.RobotPosition))
            //    {
            //        var routeFinder = new SimpleAStar(Map.Clone());
            //        var route = routeFinder.GetRouteTo(neighbor);
            //        if (_commands == null || (route != null && route.Count < _commands.Count))
            //            _commands = route;                    
            //    }
            //}

            if (_commands == null)
                return RobotCommand.Abort;

            return _commands.Dequeue();
        }       
    }
}
