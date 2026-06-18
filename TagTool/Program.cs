using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Commands.Tags;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.IO;
using TagTool.Scripting.CSharp;

namespace TagTool.Commands
{
    public static class Program
    {
        const string AutoExecFileName = "autoexec.cmds";

        static int Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");

            AssemblyResolver.ConfigureAssemblyResolution();

            // Setup logging and output
            Log.AddHandler(new ConsoleLogHandler());
            Log.AddHandler(new RunMetricsLogHandler());
            Log.Level = LogLevel.Info; // TODO: set via command line

            AnsiConsole.Initialize();
            ConsoleHistory.Initialize();

            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine($"TagTool [{assembly.GetName().Version} (Built {FileTimeUtil.GetLinkerTimestampUtc(assembly)} UTC)]");
            Console.WriteLine();
            Console.WriteLine("Please report any bugs and/or feature requests:");
            Console.WriteLine("https://github.com/TheGuardians/TagTool/issues");

            AssemblyResolver.CheckMissingDependencies();

            try
            {
                return MainCore(args);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Log.Error(ex);
                ConsoleHistory.Dump("hott_*_crash.log");
                return -1;
            }
        }

        static int MainCore(string[] args)
        {
            var contextStack = new CommandContextStack();

            // if the first argument is a c# script, execute it and exit
            if (args.Length > 0 && args[0].Trim('\"').EndsWith(".cs"))
                return ExecuteCSharpScript(args, contextStack);

            // If there are extra arguments, use them to automatically execute a command
            string autoexecCommand = null;
            if (args.Length > 1)
                autoexecCommand = string.Join(' ', args[1..]);

            string[] autoExecLines = ReadAutoExecFile();
            string cacheFilePath = args.Length > 0 ? ResolveCacheFilePath(args[0]) : "tags.dat";

            // If there are no args, try using the first line of the autoexec file
            if (args.Length == 0 && autoExecLines.Length > 0)
            {
                string defaultCacheFilePath = ResolveCacheFilePath(autoExecLines[0]);
                if (File.Exists(defaultCacheFilePath))
                {
                    cacheFilePath = defaultCacheFilePath;
                    autoExecLines[0] = $"// {autoExecLines[0]}"; // comment it out to preserve line numbers
                }
            }
            else
            {
                autoExecLines = [];
            }

            var cacheFileInfo = new FileInfo(cacheFilePath);

            if (args.Length > 0 && !cacheFileInfo.Exists)
                Log.Error("Invalid path to a tag cache!");

            if (!cacheFileInfo.Exists)
                cacheFileInfo = PromptCacheFile();

            GameCache gameCache = OpenCacheFile(cacheFileInfo);
            if (gameCache == null)
                return -1;

            CommandContext tagsContext = TagCacheContextFactory.Create(contextStack, gameCache);
            contextStack.Push(tagsContext);

            var commandRunner = new CommandRunner(contextStack);

            if (!RunAutoExecFile(commandRunner, autoExecLines))
                return -1;

            if (autoexecCommand != null)
            {
                if (!RunAutoExec(commandRunner, args, autoexecCommand))
                    return -1;
            }
            else
            {
                RunCommandLoop(commandRunner, contextStack);
            }

            return 0;
        }

        private static void RunCommandLoop(CommandRunner commandRunner, CommandContextStack contextStack)
        {
            Console.WriteLine("\nEnter \"help\" to list available commands. Enter \"quit\" to quit.");
            while (!commandRunner.EOF)
            {
                // Read and parse a command
                Console.WriteLine();
                Console.Write("{0}> ", contextStack.GetPath());
                Console.Title = $"TagTool {contextStack.GetPath()}>";

                object result = commandRunner.RunCommand(Console.ReadLine(), printInput: false);
                if (result is TagToolError error)
                    Log.Error(error.Message);
            }
        }

        private static FileInfo PromptCacheFile()
        {
            while (true)
            {
                Console.WriteLine("\nEnter the path to a Halo cache file (.map/.dat):");
                Console.Write("> ");

                string cacheFilePath = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(cacheFilePath))
                    continue;

                switch (cacheFilePath.ToLower())
                {
                    case "exit":
                    case "quit":
                        Environment.Exit(0);
                        break;
                }

                cacheFilePath = ResolveCacheFilePath(cacheFilePath);

                // If the file was found return it
                if (File.Exists(cacheFilePath))
                    return new FileInfo(cacheFilePath);

                Log.Error("Invalid path to a tag cache!");
            }
        }

        private static GameCache OpenCacheFile(FileInfo fileInfo)
        {
#if !DEBUG
            try
            {
#endif
            return GameCache.Open(fileInfo);
#if !DEBUG
            }
            catch (Exception e)
            {
                Log.Error(e);
                ConsoleHistory.Dump("hott_*_init.log");
                return null;
            }
#endif
        }

        private static string ResolveCacheFilePath(string path)
        {
            path = path.Trim('\"', '\\', '/');

            // Legacy support for maps and root directories
            if (!path.EndsWith(".map") && !path.EndsWith(".dat"))
            {
                string append = path.EndsWith("maps") ? "tags.dat" : "maps\\tags.dat";
                path = Path.Combine(path, append);
            }

            return path;
        }

        private static string[] ReadAutoExecFile()
        {
            string autoExecFilePath = Path.Combine(DirectoryPaths.Base, AutoExecFileName);
            if (!File.Exists(autoExecFilePath))
                return [];

            return File.ReadAllLines(autoExecFilePath);
        }

        private static bool RunAutoExec(CommandRunner commandRunner, string[] args, string autoexecCommand)
        {
            object result = null;

            // Allow passing .cmds and .cs files directly
            if (args.Length > 1 && args[1].EndsWith(".cs"))
            {
                if (ExecuteCSharpScript(args[1..], commandRunner.ContextStack) != 0)
                    return false;
            }
            else if (args.Length > 1 && args[1].EndsWith(".cmds"))
            {
                result = commandRunner.RunCommandScript(args[1]);
            }
            else
            {
                // Legacy support for executing a command
                result = commandRunner.RunCommand(autoexecCommand);
            }

            if (result is TagToolError error)
            {
                Log.Error(error.Message);
                return false;
            }

            return true;
        }

        private static bool RunAutoExecFile(CommandRunner commandRunner, string[] commands)
        {
            var reader = new StringReader(string.Join(Environment.NewLine, commands));

            object result = commandRunner.RunCommandScript(AutoExecFileName, reader);
            if (result is TagToolError error)
            {
                Log.Error(error.Message);
                return false;
            }

            return true;
        }

        private static int ExecuteCSharpScript(string[] args, CommandContextStack contextStack)
        {
            try
            {
                var evalContext = new ScriptEvaluationContext(contextStack);
                contextStack.ScriptEvaluator.ExecuteScriptFile(evalContext, filePath: args[0], args: args[1..]);
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return -1;
            }
        }
    }
}