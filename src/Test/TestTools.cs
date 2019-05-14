namespace Test
{
    using System;
    using KellermanSoftware.CompareNetObjects;

    public static class TestTools
    {
        private static readonly CompareLogic Logic = new CompareLogic
        {
            Config =
            {
                MaxMillisecondsDateDifference = 1,
                CompareDateTimeOffsetWithOffsets = true
            }
        };

        public static void AssertEqual<T>(T excpected, T first)
        {
            var compare = Logic.Compare(first, excpected);

            if (!TestEqual(excpected, first))
            {
                throw new Exception(compare.DifferencesString);
            }
        }

        public static bool TestEqual<T>(T excpected, T first)
        {
            var compare = Logic.Compare(first, excpected);
            return compare.AreEqual;
        }
    }
}