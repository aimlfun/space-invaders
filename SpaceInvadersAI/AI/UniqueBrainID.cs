using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI
{
    internal static class UniqueBrainID
    {
        /// <summary>
        /// This is the next brain id to be used, they are numbered sequentially.
        /// </summary>
        private static int s_nextBrainIdSequence = 0;

        /// <summary>
        /// Returns a brain ID in base 36, this takes up less space than base 10.
        /// Originally I tried base 10, but 100 is 3 characters, 1000000 is 7 characters. But the latter in base 36 is "LFLS", a mere 4.
        /// </summary>
        /// <returns></returns>
        internal static string GetNextBrainId()
        {
            // increment the next brain id
            s_nextBrainIdSequence++;

            // convert the next brain id to a base 64 string
            return ConvertToBase36(s_nextBrainIdSequence);
        }

        /// <summary>
        /// Convert a number to a base 36 string. This shortens the brain id names.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static string ConvertToBase36(int value)
        {
            const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string result = string.Empty;

            while (value != 0) // as we loop, we keep dividing by 36, and the remainder is the index into the base36 string
            {
                result = base36[value % 36] + result;
                value /= 36;
            }

            return result;
        }
    }
}
