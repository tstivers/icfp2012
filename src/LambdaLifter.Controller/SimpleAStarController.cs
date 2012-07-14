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
        public string State { get; private set; }

        public SimpleAStarController(Map map)
            : base(map)
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
            if (_commands != null && _commands.Count > 0 && !Map.RocksMoved && !Map.StuffChanged)
                return _commands.Dequeue();          

            _commands = null;

            var routeFinder = new SimpleAStar(Map);

            foreach (var lambda in Map.PriorityLambdas)
            {
                if (Map.Cells.At(lambda.Up()).IsRock())
                    continue;
                
                var route = routeFinder.GetRouteTo(lambda);
                if (_commands == null || (route != null && route.Count < _commands.Count))
                {
                    _commands = route;
                    State = String.Format("Navigating to PriorityLambda at {0}", lambda);
                }
            }

            if (_commands == null)
            {
                foreach (var lambda in Map.PriorityLambdas)
                {                    
                    var route = routeFinder.GetRouteTo(lambda);
                    if (_commands == null || (route != null && route.Count < _commands.Count))
                    {
                        _commands = route;
                        State = String.Format("Navigating to PriorityLambda at {0}", lambda);
                    }
                }
            }

            if (_commands == null)
            {
                foreach (var lambda in Map.Lambdas)
                {
                    if (Map.Cells.At(lambda.Up()).IsRock())
                        continue;
                    
                    var route = routeFinder.GetRouteTo(lambda);
                    if (_commands == null || (route != null && route.Count < _commands.Count))
                    {
                        _commands = route;
                        State = String.Format("Navigating to Lambda at {0}", lambda);
                    }
                }
            }

            if (_commands == null)
            {
                foreach (var lambda in Map.Lambdas)
                {                    
                    var route = routeFinder.GetRouteTo(lambda);
                    if (_commands == null || (route != null && route.Count < _commands.Count))
                    {
                        _commands = route;
                        State = String.Format("Navigating to Lambda at {0}", lambda);
                    }
                }
            }

            if (_commands == null && Map.Lambdas.Count == 0)
            {                
                _commands = routeFinder.GetRouteTo(Map.Lifts[0]);
                State = String.Format("Navigating to lift at {0}", Map.Lifts[0]);
            }

            if (_commands == null)
            {
                var temp = Map.Clone();
                temp.ExecuteTurn(RobotCommand.Wait);
                if (temp.RocksMoved)
                {
                    State = String.Format("Waiting for rocks to move");
                    return RobotCommand.Wait;
                }
            }

            if (_commands == null)
            {                
                foreach (var rock in Map.MoveableRocks)
                {
                    if (Map.Cells.IsValidMove(rock.Left(), rock))
                    {
                        var route = routeFinder.GetRouteTo(rock.Left());
                        if (route != null)
                        {
                            _commands = route;                            
                            _commands.Enqueue(RobotCommand.Right);
                            State = String.Format("Moving rock right at {0}", rock);
                            Map.Target = rock;
                            break;
                        }
                    }
                    else if (Map.Cells.IsValidMove(rock.Right(), rock))
                    {
                        var route = routeFinder.GetRouteTo(rock.Right());
                        if (route != null)
                        {
                            _commands = route;
                            _commands.Enqueue(RobotCommand.Left);
                            State = String.Format("Moving rock left at {0}", rock);
                            break;
                        }
                    }
                    else if (Map.Cells.At(rock.Right()).IsEarth())
                    {
                        var route = routeFinder.GetRouteTo(rock.Right());
                        if (route != null)
                        {
                            _commands = route;
                            State = String.Format("Clearing rock right at {0}", rock);
                            break;
                        }
                    }
                    else if (Map.Cells.At(rock.Left()).IsEarth())
                    {
                        var route = routeFinder.GetRouteTo(rock.Left());
                        if (route != null)
                        {
                            _commands = route;
                            State = String.Format("Clearing rock left at {0}", rock);
                            break;
                        }
                    }
                    else if (Map.Cells.At(rock.Down()).IsTraversible())
                    {
                        var route = routeFinder.GetRouteTo(rock.Down());
                        if (route != null)
                        {
                            _commands = route;
                            State = String.Format("Clearing rock bottom at {0} : {1}", rock, rock.Down());
                            Map.Target = rock.Down();
                            break;
                        }
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
            {
                State = "Nothing I can do...";
                return RobotCommand.Abort;
            }

            if (Map.Cells.At(Map.RobotPosition.Move(_commands.Peek())).IsLambda())
            {
                var cmd = _commands.Dequeue();
                _commands = null;
                return cmd;
            }

            return _commands.Dequeue();
        }
    }
}
