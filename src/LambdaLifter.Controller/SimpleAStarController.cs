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

        public override RobotCommand GetNextMove()
        {
            if (_commands != null && _commands.Count > 0 && !Map.RocksMoved && !Map.StuffChanged)
                return _commands.Dequeue();          

            _commands = null;

            var routeFinder = new AStarRouteFinder(Map);           

            // first we try to get to a lambda
            if (Map.Lambdas.Count > 0)
            {
                var routes = new Dictionary<Queue<RobotCommand>, int>();
                foreach (var lambda in Map.Lambdas)
                {                    
                    var route = routeFinder.GetRouteTo(lambda);

                    if (route == null)
                        continue;

                    var score = route.Count;
                    if (routeFinder.UsesPortals)
                        score *= 1000;
                    else if (routeFinder.PushesRocks)
                        score *= 100;
                    else if (routeFinder.DisturbsRocks)
                        score *= 10;

                    routes.Add(route, score);
                }

                if (routes.Count > 0)
                    _commands = routes.Aggregate((best, route) => best = route.Value < best.Value ? route : best).Key;
            }

            // if all lambdas are gone, try to get to the lift
            if (Map.Lambdas.Count == 0)
            {                
                _commands = routeFinder.GetRouteTo(Map.Lifts[0]);
                State = String.Format("Navigating to lift at {0}", Map.Lifts[0]);
            }

            // can't get lambas or a lift, wait for falling rocks to stop
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

            // try to move a rock as a last ditch effort
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

            // can't get anywhere or move any rocks, give up
            if (_commands == null)
            {
                State = "Nothing I can do...";
                return RobotCommand.Abort;
            }            

            return _commands.Dequeue();
        }
    }
}
