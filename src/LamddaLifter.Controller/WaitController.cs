using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LambdaLifter.Model;

namespace LamddaLifter.Controller
{
    public class WaitController : ControllerBase
    {
        public WaitController(Map map)
            : base(map)
        {
            
        }

        public override RobotCommand GetNextMove()
        {
            return RobotCommand.Right;
        }
    }
}
