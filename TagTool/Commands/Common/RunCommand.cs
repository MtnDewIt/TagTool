using System.Collections.Generic;
using System.IO;
using TagTool.Scripting.CSharp;

namespace TagTool.Commands.Common
{
    class RunCommand : Command
    {
        private readonly CommandContextStack ContextStack;

        public RunCommand(CommandContextStack contextStack)
            : base(true,

                  "Run",
                  "Runs a command (.cmds) or csharp script (.cs) file ",

                  "Run <file> [args...]",

                  "")
        {
            ContextStack = contextStack;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count < 1)
                return new TagToolError(CommandError.ArgCount);

            string fileName = args[0];

            if (!File.Exists(fileName))
                return new TagToolError(CommandError.FileNotFound, fileName);

            var commandRunner = new CommandRunner(ContextStack)
            {
                // inherit error suppression
                SuppressErrors = CommandRunner.Current?.SuppressErrors ?? false
            };

            switch (Path.GetExtension(fileName))
            {
                case ".cs":
                    {
                        var evalContext = new ScriptEvaluationContext(ContextStack);
                        return ContextStack.ScriptEvaluator.ExecuteScriptFile(evalContext, args[0], args[1..]);
                    }

                case ".cmds":
                    return commandRunner.RunCommandScript(fileName, shouldPrint: false);

                default:
                    return new TagToolError(CommandError.ArgInvalid, $"Unknown file extension '{fileName}'");
            }
        }
    }
}
