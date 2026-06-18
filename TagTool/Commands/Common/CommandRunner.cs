using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagTool.Common.Logging;
using TagTool.IO;
using TagTool.Scripting.CSharp;

namespace TagTool.Commands.Common
{
    public class CommandRunner
    {
        public CommandContextStack ContextStack { get; set; }
        public bool EOF { get; private set; } = false;
        public string CommandLine { get; private set; }
        public string CurrentCommandName { get; private set; }
        // If true errors returned from commands will not cause the script to terminate
        public bool SuppressErrors { get; set; }

        public static CommandRunner Current { get; private set; }

        public CommandRunner(CommandContextStack contextStack)
        {
            ContextStack = contextStack;
        }

        public object RunCommandScript(string filePath, bool shouldPrint = false)
        {
            if (!File.Exists(filePath))
                return new TagToolError(CommandError.FileNotFound, filePath);

            string fileName = Path.GetFileName(filePath);

            using var reader = new LineTrackingTextReader(File.OpenText(filePath));

            return RunCommandScript(fileName, reader, shouldPrint);
        }

        public object RunCommandScript(string fileName, TextReader inputReader, bool shouldPrint = false)
        {
            var reader = new LineTrackingTextReader(inputReader);

            TextReader oldStdIn = Console.In;
            Console.SetIn(reader);

            try
            {
                for (string line; (line = reader.ReadLine()) != null && !EOF;)
                {
                    object result = RunCommand(line, shouldPrint);
                    if (result is TagToolError error)
                    {
                        string indentedMessage = string.Join("\n", error.Message.Split('\n').Select((line, i) => i > 0 ? $"  {line}" : line));
                        string errorMessage = $"Error executing \"{line}\"\n  in \"{fileName}\" on line {reader.LineNumber}: {indentedMessage}";

                        if (SuppressErrors && error.Error != CommandError.CmdNotFound)
                        {
                            Log.Error(errorMessage);
                        }
                        else
                        {
                            return error.Error == CommandError.CmdScriptError
                                ? error
                                : new TagToolError(CommandError.CmdScriptError, errorMessage);
                        }
                    }
                }
            }
            finally
            {
                Console.SetIn(oldStdIn);
            }

            return true;
        }

        public object RunCommand(string commandLine, bool printInput = false, bool printOutput = true)
        {
            if (commandLine == null)
            {
                EOF = true;
                return false;
            }

            Current = this;
            CommandLine = commandLine = PreprocessCommandLine(commandLine);
            if (commandLine == null)
                return false;

            if (printInput)
                Console.WriteLine(commandLine);

            var commandArgs = ArgumentParser.ParseCommand(commandLine, out string redirectFile);
            if (commandArgs.Count == 0)
                return false;

            switch (commandArgs[0].ToLower())
            {
                case "quit":
                    EOF = true;
                    return true;
                case "exit":
                    if (ContextStack.IsBase())
                        Log.Warning("Cannot exit, already at base context! Use 'quit' to quit tagtool.");
                    else if (ContextStack.IsModPackage())
                        Log.Warning("Use 'exitmodpackage' to leave a mod package context.");
                    else
                        ContextStack.Pop();
                    return true;
                case "exitmodpackage":
                    {
                        if (ContextStack.IsModPackage())
                            ContextStack.Pop();
                        else
                            Log.Warning("Use 'exit' to leave standard contexts.");
                    }
                    return true;
                case "cs" when !ExecuteCSharpCommand.OutputIsRedirectable(commandArgs.Skip(1).ToList()):
                    redirectFile = null;
                    break;
            }

            if (commandArgs[0].StartsWith("#") || commandArgs[0].StartsWith($"//"))
                return true; // ignore comments

            // Handle redirection
            var oldOut = Console.Out;
            StreamWriter redirectWriter = null;
            if (redirectFile != null || !printOutput)
            {
                redirectWriter = !printOutput ? StreamWriter.Null : new StreamWriter(File.Open(redirectFile, FileMode.Create, FileAccess.Write));
                Console.SetOut(redirectWriter);
            }

            // Try to execute it
            object result = ExecuteCommand(ContextStack.Context, commandArgs, ContextStack.ArgumentVariables);

            // Undo redirection
            if (redirectFile != null || !printOutput)
            {
                Console.SetOut(oldOut);
                redirectWriter.Dispose();
                if (redirectFile != null)
                    Console.WriteLine("Wrote output to {0}.", redirectFile);
            }

            return result;
        }

        private object ExecuteCommand(CommandContext context, List<string> commandAndArgs, Dictionary<string, string> argVariables)
        {
            // Look up the command
            Command command = context.GetCommand(commandAndArgs[0]);
            if (command == null)
            {
                return new TagToolError(CommandError.CmdNotFound, commandAndArgs[0]);
            }

            commandAndArgs.RemoveAt(0);

            // Replace argument variables with their values
            for (int i = 0; i < commandAndArgs.Count; i++)
                commandAndArgs[i] = ApplyUserVars(commandAndArgs[i], command.IgnoreArgumentVariables);

            CurrentCommandName = command.Name;
            object result = command.Execute(commandAndArgs);
            CurrentCommandName = "";
            return result;
        }

        private string PreprocessCommandLine(string commandLine)
        {
            // Evaluate c# expressions

            try
            {
                var evalContext = new ScriptEvaluationContext(ContextStack);
                commandLine = ContextStack.ScriptEvaluator.EvaluateInlineExpressions(evalContext, commandLine);
                if (commandLine == null)
                    return null;
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
            }

            // Allow inline comments beginning with "//"

            commandLine = commandLine.Split(new[] { "//" }, StringSplitOptions.None)[0];

            return commandLine;
        }

        public static string ApplyUserVars(string inputStr, bool ignoreArgumentVariables)
        {
            if (!ignoreArgumentVariables)
            {
                foreach (var variable in Current.ContextStack.ArgumentVariables)
                {
                    inputStr = inputStr.Replace(variable.Key, variable.Value);
                }
            }
            return inputStr;
        }
    }
}
