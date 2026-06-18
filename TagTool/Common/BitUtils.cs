using System.Runtime.Intrinsics.X86;

namespace TagTool.Common
{
    public static class BitUtils
    {
        private static readonly bool HasBmi2 = Bmi2.X64.IsSupported;

        public static ulong Pdep(ulong value, ulong mask)
        {
            if (HasBmi2)
                return Bmi2.X64.ParallelBitDeposit(value, mask);
            else
                return SoftwarePdep(value, mask);
        }

        public static ulong Pext(ulong value, ulong mask)
        {
            if (HasBmi2)
                return Bmi2.X64.ParallelBitExtract(value, mask);
            else
                return SoftwarePext(value, mask);
        }

        private static ulong SoftwarePdep(ulong value, ulong mask)
        {
            ulong result = 0;
            ulong bit = 1;

            while (mask != 0)
            {
                ulong lowest = mask & (~mask + 1);

                if ((value & bit) != 0)
                    result |= lowest;

                mask ^= lowest;
                bit <<= 1;
            }

            return result;
        }

        private static ulong SoftwarePext(ulong value, ulong mask)
        {
            ulong result = 0;
            ulong bit = 1;

            while (mask != 0)
            {
                ulong lowest = mask & (~mask + 1);

                if ((value & lowest) != 0)
                    result |= bit;

                mask ^= lowest;
                bit <<= 1;
            }

            return result;
        }
    }
}
