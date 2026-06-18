using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using TagTool.Cache;
using TagTool.Common;

namespace TagTool.Tags
{
    public static class VersionedEnum
    {
        public static object ImportValue(Type enumType, int value, CacheVersion version, CachePlatform platform)
        {
            var info = TagEnum.GetInfo(enumType, version, platform);
            if (!info.Attribute.IsVersioned)
                throw new InvalidOperationException("Cannot import to an non-versioned enum.");

            return ImportValue(info, value);
        }

        public static object ImportValue(TagEnumInfo info, int value)
        {
            var enumerable = TagEnum.GetMemberEnumerable(info);
            var members = enumerable.VersionedMembers;

            if (value < 0 || value >= members.Count)
            {
                if (enumerable.Constants.Contains(value))
                    return value;

                throw new ArgumentOutOfRangeException(nameof(value), "Value was out of range of the enum members");
            }

            return members[value].Value;
        }

        public static int ExportValue(Type enumType, object enumValue, CacheVersion version, CachePlatform platform)
        {
            var info = TagEnum.GetInfo(enumType, version, platform);
            if (!info.Attribute.IsVersioned)
                throw new InvalidOperationException("Cannot import to an non-versioned enum.");

            return ExportValue(info, enumValue);
        }

        public static int ExportValue(TagEnumInfo info, object enumValue)
        {
            var members = TagEnum.GetMemberEnumerable(info).Members;
            int actualMemberIndex = 0;
            for (int i = 0; i < members.Count; i++)
            {
                bool isConstant = (members[i].Attribute.Flags & TagEnumMemberFlags.Constant) != 0;
                if (members[i].Value.Equals(enumValue))
                {
                    if (isConstant)
                        return Convert.ToInt32(enumValue);
                    else
                        return actualMemberIndex;
                }

                if (!isConstant)
                    actualMemberIndex++;
            }

            throw new ArgumentOutOfRangeException(nameof(enumValue));
        }

        public static bool TryParse(TagEnumInfo info, string value, out object result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(value) || !info.IsVersioned)
                return false;

            NumberStyles style = NumberStyles.Integer;
            string integerString = value;
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                integerString = value[2..];
                style = NumberStyles.HexNumber;
            }

            var enumerable = TagEnum.GetMemberEnumerable(info);
            var members = enumerable.VersionedMembers;

            if (info.IsFlags)
                throw new InvalidOperationException("Versioned flags should be standard enums wrapped as BitFlags<TEnum>");
            else
            {
                if (value.Split(',').Length > 1)
                    return false;

                if (int.TryParse(integerString, style, null, out int intValue))
                {
                    if (enumerable.Constants.Contains(intValue))
                        result = Enum.ToObject(info.Type, intValue);

                    else if (intValue >= 0 && intValue < members.Count)
                        result = members[intValue].Value;
                }
                else if (Enum.TryParse(info.Type, value, true, out object enumValue)
                    && VersionHasMember(info, enumValue))
                {
                    result = enumValue;
                }

                if (result is not null)
                    return true;
            }

            return false;
        }

        public static bool VersionHasMember(TagEnumInfo info, object enumValue)
        {
            var enumerable = TagEnum.GetMemberEnumerable(info);
            var memberInfo = enumerable.VersionedMembers.FirstOrDefault(m => m.Value.Equals(enumValue));

            if (memberInfo is not null || MemberIsConstant(info, enumValue))
                return true;

            return false;
        }

        public static bool MemberIsConstant(TagEnumInfo info, object enumValue)
        {
            var enumerable = TagEnum.GetMemberEnumerable(info);
            if (enumerable.Constants.Contains((int)enumValue))
                return true;

            return false;
        }

        public static ulong ImportFlags(TagEnumInfo info, ulong value)
        {
            return BitUtils.Pdep(value, info.MemberMask);
        }

        public static ulong ExportFlags(TagEnumInfo info, ulong value)
        {
            return BitUtils.Pext(value, info.MemberMask);
        }

        public static bool ValidateFlagsForImport(TagEnumInfo info, ulong value)
        {
            int pop = BitOperations.PopCount(info.MemberMask);
            ulong validMask = pop == 64 ? ulong.MaxValue : (1UL << pop) - 1;
            return (value & ~validMask) == 0UL;
        }

        public static bool ValidateFlagsForExport(TagEnumInfo info, ulong value)
        {
            return (value & ~info.MemberMask) == 0UL;
        }
    }
}
