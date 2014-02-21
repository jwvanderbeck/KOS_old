﻿using System.Text.RegularExpressions;
using kOS.Context;
using kOS.Debug;
using kOS.Suffixed;

namespace kOS.Command.Vessel
{
    [Command("ADD *")]
    public class CommandAddObjectToVessel : Command
    {
        public CommandAddObjectToVessel(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            var ex = new Expression.Expression(RegexMatch.Groups[1].Value, this);
            var obj = ex.GetValue();

            var node = obj as Node;
            if (node != null)
            {
                node.AddToVessel(Vessel);
            }
            else
            {
                throw new KOSException("Supplied object ineligible for adding", this);
            }

            State = ExecutionState.DONE;
        }
    }
}