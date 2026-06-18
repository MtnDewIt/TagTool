using TagTool.Common.Logging;

namespace TagTool.Commands.Common
{
    public enum CommandError
    {
        None,
        CustomError,
        CmdScriptError, // internal use only
        CmdNotFound,
        ArgCount,
        ArgInvalid,
        OperationFailed,
        TagInvalid,
        SyntaxInvalid,
        DirectoryNotFound,
        FileNotFound,
        FileIO,
        FileType,
        CacheUnsupported,
        YesNoSyntax,
    }

    /// <remarks>
    /// Prefer using <see cref="Log.Error(string)"/> directly when outside of commands
    /// </remarks>
    public class TagToolError
    {
        public readonly CommandError Error;
        public readonly string Message;

        public TagToolError(CommandError cmdError, string customMessage = null)
        {
            Error = cmdError;
            Message = FormatErrorMessage(Error, customMessage);
        }

        private static string FormatErrorMessage(CommandError cmdError, string customMessage)
        {
            bool showHelpMessage = false;
            string output = "";

            string checkedMsg = string.IsNullOrEmpty(customMessage)
                ? $"[{cmdError} without message!]"
                : customMessage;

            if (cmdError == CommandError.CmdScriptError)
                return checkedMsg;

            if (cmdError != CommandError.CustomError)
            {
                switch (cmdError)
                {
                    case CommandError.CmdNotFound:
                        output += "Unrecognized command. Use \"help\" to list available commands";
                        break;
                    case CommandError.ArgCount:
                        output += "Incorrect amount of arguments supplied";
                        showHelpMessage = true;
                        break;
                    case CommandError.ArgInvalid:
                        output += "An invalid argument was specified";
                        showHelpMessage = true;
                        break;
                    case CommandError.OperationFailed:
                        output += "An internal operation failed to evaluate";
                        break;
                    case CommandError.TagInvalid:
                        output += "The specified tag does not exist in the current tag cache";
                        break;
                    case CommandError.SyntaxInvalid:
                        output += "Invalid syntax used";
                        break;
                    case CommandError.DirectoryNotFound:
                        output += "The specified directory could not be found!";
                        break;
                    case CommandError.FileNotFound:
                        output += "The specified file could not be found!";
                        break;
                    case CommandError.FileIO:
                        output += "A file IO operation could not be completed";
                        break;
                    case CommandError.FileType:
                        output += "The specified file is of the incorrect type";
                        showHelpMessage = true;
                        break;
                    case CommandError.CacheUnsupported:
                        output += "The specified blam cache is not supported";
                        break;
                    case CommandError.YesNoSyntax:
                        output += "A response option other than \"y\" or \"n\" was given";
                        showHelpMessage = true;
                        break;
                }
            }
            else
                return checkedMsg;

            if (customMessage is not null)
                output += $"\n> {customMessage}";

            if (showHelpMessage)
                output += $"\nEnter \"Help {CommandRunner.Current?.CurrentCommandName}\" for command syntax.";

            return output;
        }
    }
}
