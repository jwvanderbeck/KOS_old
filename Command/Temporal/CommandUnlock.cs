using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.Temporal
{
    [Command("UNLOCK %")]
    public class CommandUnlock : Command
    {
        public CommandUnlock(Match regexMatch, IExecutionContext context) : base(regexMatch, context)
        {
        }

        public override void Evaluate()
        {
            var varname = RegexMatch.Groups[1].Value;

            if (varname.ToUpper() == "ALL")
            {
                ParentContext.UnlockAll();
            }
            else
            {
                ParentContext.Unlock(varname);
            }

            State = ExecutionState.DONE;
        }
    }
}