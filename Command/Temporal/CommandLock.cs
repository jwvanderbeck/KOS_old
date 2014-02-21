using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.Temporal
{
    [Command("LOCK % TO *")]
    public class CommandLock : Command
    {
        public CommandLock(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            var varname = RegexMatch.Groups[1].Value;
            var expression = new Expression.Expression(RegexMatch.Groups[2].Value, ParentContext);

            ParentContext.Unlock(varname);
            ParentContext.Lock(varname, expression);

            State = ExecutionState.DONE;
        }
    }
}