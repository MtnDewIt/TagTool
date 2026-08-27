using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Commands.Files;
using TagTool.Common.Logging;
using TagTool.Porting;

namespace TagTool.Commands.Porting
{
    public partial class PortTagCommand : Command
    {
        private GameCacheHaloOnlineBase CacheContext { get; }
        private GameCache BlamCache;
        private PortingContext PortContext;

        public PortTagCommand(GameCacheHaloOnlineBase cacheContext, GameCache blamCache, PortingContext portContext) :
            base(true,

                "PortTag",
                PortTagCommand.GetPortingFlagsDescription(),
                "PortTag [Options] <Tag>",
                "")
        {
            CacheContext = cacheContext;
            BlamCache = blamCache;
            PortContext = portContext;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count < 1)
                return new TagToolError(CommandError.ArgCount);

            PortingFlags flags = ParseFlags(args.Take(args.Count - 1).ToList());
            List<CachedTag> tagList = ParseLegacyTag(args.Last(), flags);

            using Stream cacheStream = CacheContext.OpenCacheReadWrite();
            using Stream blamCacheStream = BlamCache is GameCacheModPackage package ? package.OpenCacheRead(cacheStream) : BlamCache.OpenCacheRead();
            using var portScope = PortContext.CreateScope(flags);

            foreach (var blamTag in tagList)
            {
                if (blamTag == null)
                    return new TagToolError(CommandError.TagInvalid, args.Last());

                try
                {
                    PortContext.ConvertTag(cacheStream, blamCacheStream, blamTag);
                }
                catch (Exception ex)
                {
                    Log.Error($"{ex.GetType().Name} while porting '{blamTag}':");
                    throw;
                }
            }

            return true;
        }

        /// <summary>
		/// Parses porting flag options from a <see cref="List{T}"/> of <see cref="string"/>.
		/// </summary>
		/// <param name="args"></param>
		public PortingFlags ParseFlags(List<string> args)
        {
            var Flags = PortingFlags.Default;

            var flagNames = Enum.GetNames(typeof(PortingFlags)).Select(name => name.ToLower());
            var flagValues = Enum.GetValues(typeof(PortingFlags)) as PortingFlags[];

            for (var a = 0; a < args.Count(); a++)
            {
                string[] argSegments = args[a].Split('[');

                var arg = argSegments[0].ToLower();

                // Support legacy arguments
                arg = arg.Replace("single", $"!{nameof(PortingFlags.Recursive)}");
                arg = arg.Replace("noshaders", $"!{nameof(PortingFlags.MatchShaders)}");
                arg = arg.Replace("silent", $"!{nameof(PortingFlags.Print)}");
                arg = arg.ToLower(); // do this again incase the argument was replaced

                // Use '!' or 'No' to negate an argument.
                var toggleOn = !(arg.StartsWith("!") || arg.StartsWith("no"));
                if (!toggleOn && arg.StartsWith("!"))
                    arg = arg.Remove(0, 1);
                else if (!toggleOn && arg.StartsWith("no"))
                    arg = arg.Remove(0, 2);

                // Throw exceptions at clumsy typers.
                if (!flagNames.Contains(arg))
                    throw new FormatException($"Invalid {typeof(PortingFlags).FullName}: {args[0]}");

                // Add/remove flags based on if they appeared as arguments, 
                // and whether they were negated with '!' or 'No'
                for (var i = 0; i < flagNames.Count(); i++)
                    if (arg == flagNames.ElementAt(i))
                        if (toggleOn)
                            Flags |= flagValues[i];
                        else
                            Flags &= ~flagValues[i];
            }
            return Flags;
        }

        /// <summary>
		/// Generates a <see cref="Command.Description"/> based on the <see cref="PortingFlags"/> <see cref="Enum"/>.
		/// </summary>
		/// <returns>A <see cref="Command.Description"/> based on <see cref="PortingFlags"/>.</returns>
		private static string GetPortingFlagsDescription()
        {
            var info =
                "Ports a tag from the current cache file." + Environment.NewLine +
                "Available Options:" + Environment.NewLine +
                Environment.NewLine;

            var padCount = Enum.GetNames(typeof(PortingFlags)).Max(flagName => flagName.Length);

            foreach (var portingFlagInfo in typeof(PortingFlags).GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = portingFlagInfo.GetCustomAttribute<DescriptionAttribute>(false);

                // Use the attribute description for the flags help-description.
                if (attr != null)
                    info += $"{portingFlagInfo.Name.PadRight(padCount)} - " +
                        $"{attr.Description}" + Environment.NewLine;

                // Use the flags sub-set of the flag for the flags help-description
                else
                {
                    var portingFlags = (PortingFlags)Enum.Parse(typeof(PortingFlags), portingFlagInfo.Name);
                    info += $"{portingFlagInfo.Name.PadRight(padCount)} - " +
                        $"{string.Join(", ", portingFlags.GetIndividualFlags())}" + Environment.NewLine;
                }
            }

            return info + Environment.NewLine +
                "*Any option can be negated by prefixing it with `!`." + Environment.NewLine;
        }

        private List<CachedTag> ParseLegacyTag(string tagSpecifier, PortingFlags flags)
        {
            List<CachedTag> result = new List<CachedTag>();

            if ((flags & PortingFlags.Regex) != 0)
            {
                var regex = new Regex(tagSpecifier);
                result = BlamCache.TagCache.TagTable.ToList().FindAll(item => item != null && regex.IsMatch(item.ToString() + "." + item.Group.Tag));
                if (result.Count == 0)
                {
                    Log.Error($"Invalid regex: {tagSpecifier}");
                    return new List<CachedTag>();
                }
                return result;
            }

            int extIndex = tagSpecifier.LastIndexOf('.');
            if (tagSpecifier.Length == 0 || extIndex == -1 || extIndex == tagSpecifier.Length - 1)
            {
                Log.Error($"Invalid tag name: {tagSpecifier}");
                return new List<CachedTag>();
            }

            string tagGroup = tagSpecifier[(extIndex + 1)..];

            if (!CacheContext.TagCache.TryParseGroupTag(tagGroup, out var groupTag))
            {
                Log.Error($"Invalid tag name: {tagSpecifier}");
                return new List<CachedTag>();
            }

            string tagName = tagSpecifier[..extIndex];

            // find the CacheFile.IndexItem(s)
            if (tagName == "*") result = BlamCache.TagCache.TagTable.ToList().FindAll(
                item => item != null && item.IsInGroup(groupTag));
            else result.Add(BlamCache.TagCache.TagTable.ToList().Find(
                item => item != null && item.IsInGroup(groupTag) && tagName == item.Name));

            if (result.Count == 0)
            {
                Log.Error($"Invalid tag name: {tagSpecifier}");
                return new List<CachedTag>();
            }

            return result;
        }
    }
}
