using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LambdaLifter.Model;

namespace LamddaLifter.Controller
{
    public class SimpleAStarController : ControllerBase
    {
        private Queue<RobotCommand> _commands = new Queue<RobotCommand>();
 
        public SimpleAStarController(Map map) : base(map)
        {
        }

        public override RobotCommand GetNextMove()
        {
            //if (_commands != null && _commands.Count > 0 && Map.IsValidCommand(_commands.Peek()))
            //    return _commands.Dequeue();

            _commands = null;            

            foreach (var lambda in Map.Lambdas)
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
                foreach (var lambda in Map.Lambdas)
                {
                    var routeFinder = new SimpleAStar(Map.Clone());
                    var route = routeFinder.GetRouteTo(lambda);
                    if (_commands == null || (route != null && route.Count < _commands.Count))
                        _commands = route;
                }
            }
            
            if (_commands == null)
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
                return RobotCommand.Abort;

            return _commands.Dequeue();
        }
    }
}
