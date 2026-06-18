namespace TagTool.Common
{
    public static class FormatUtils
    {
        private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB", "PB"];

        public static string FormatBytes(long bytes)
        {
            if (bytes < 0)
                return $"-{FormatBytes(-bytes)}";

            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024 && unitIndex < SizeUnits.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{bytes:N0} B"
                : $"{value:0.##} {SizeUnits[unitIndex]}";
        }
    }
}
