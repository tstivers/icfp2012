using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LambdaLifter.Model;

namespace LambdaLifter.Controller
{
    public abstract class ControllerBase
    {
        public Map Map { get; set; }

        protected ControllerBase(Map map)
        {
            Map = map;
        }

        public abstract RobotCommand GetNextMove();
    }
}
