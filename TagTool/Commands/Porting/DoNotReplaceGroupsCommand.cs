using System;
using System.Collections.Generic;
using System.Linq;
using TagTool.Commands.Common;
using TagTool.Porting;

namespace TagTool.Commands.Porting
{
    class DoNotReplaceGroupsCommand : Command
    {
        private readonly PortingContext PortContext;

        public DoNotReplaceGroupsCommand(PortingContext portContext)
               : base(true,

                     "DoNotReplaceGroups",
                     "Prevents the specified tag groups from being replaced when porting tags.",

                     "DoNotReplaceGroups [remove] <tag groups>",

                     "Prevents the specified tag groups from being replaced when porting tags.\n" +
                     "Tag group format: \"grp1,grp2,grp3\". Use \"all\" to clear the list.")
        {
            PortContext = portContext;        
        }

        public override object Execute(List<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine("Current groups: " + string.Join(", ", PortContext.DoNotReplaceGroups));
                return true;
            }
            else if (args.Count < 1 || args.Count > 2)
                return new TagToolError(CommandError.ArgCount);

            bool adding = true;
            if (args.Count == 2)
            {
                if (args[0].ToLower() == "remove")
                {
                    adding = false;
                    if (args[1] == "all")
                    {
                        PortContext.DoNotReplaceGroups.Clear();
                        return true;
                    }
                    args.RemoveAt(0);
                }
                else
                {
                    return new TagToolError(CommandError.ArgInvalid);
                }
            }

            List<string> groups = args[0].Split(',').ToList();

            foreach (var group in groups)
            {
                string tempGroup = group;
                while (tempGroup.Length < 4)
                    tempGroup += " ";

                if (adding && !PortContext.DoNotReplaceGroups.Contains(tempGroup))
                    PortContext.DoNotReplaceGroups.Add(tempGroup);
                else if (!adding && PortContext.DoNotReplaceGroups.Contains(tempGroup))
                    PortContext.DoNotReplaceGroups.Remove(tempGroup);
            }

            Console.WriteLine("Current groups: " + string.Join(", ", PortContext.DoNotReplaceGroups));
            return true;
        }
    }
}

