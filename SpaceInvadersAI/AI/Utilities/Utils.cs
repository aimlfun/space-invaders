using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI.Utilities;

/// <summary>
/// Utility functions.
/// </summary>
internal static class Utils
{
    //  █   █   █████    ███    █        ███    █████    ███    █████    ███
    //  █   █     █       █     █         █       █       █     █       █   █
    //  █   █     █       █     █         █       █       █     █       █
    //  █   █     █       █     █         █       █       █     ████     ███
    //  █   █     █       █     █         █       █       █     █           █
    //  █   █     █       █     █         █       █       █     █       █   █
    //   ███      █      ███    █████    ███      █      ███    █████    ███
    
    /// <summary>
    /// RegEx to find pairs of {field}={value}. The value can contain spaces and equals signs, but not newlines.
    /// </summary>
    private static readonly string attributeValuePairsRegExPattern = @"^[\w|\s]*((\b[^\s=]+)+=(([^=]|\\=)+))*$";

    /// <summary>
    /// We do this once to make sure it's compiled (not repeatedly).
    /// </summary>
    private static readonly Regex regExToFindAttributePairs = new(attributeValuePairsRegExPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Returns a cryptographic random double between -1 and +1.
    /// </summary>
    /// <returns></returns>
    internal static double RandomNumberPlusOrMinus1()
    {
        return (double)(RandomNumberGenerator.GetInt32(0, 2000000) - 1000000) / 1000000;
    }

    /// <summary>
    /// Returns true/false with a 50% chance.
    /// </summary>
    /// <returns></returns>
    internal static bool FiftyPercentChanceAtRandom()
    {
        return RandomNumberGenerator.GetInt32(0, 100000) < 50000;
    }

    /// <summary>
    /// Generate a cryptographic random number between -0.5...+0.5.
    /// </summary>
    /// <returns></returns>
    internal static double RandomFloatBetweenMinusHalfToPlusHalf()
    {
        return (double)(RandomNumberGenerator.GetInt32(0, 100000) - 50000) / 100000;
    }

    /// <summary>
    /// Returns a cryptographic random double between the ranges specified. 
    /// Please note the result can be positive or negative. i.e. 0.1 - 0.25 => -0.25..-0.1 and +0.1..+0.25.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    internal static double RandomPlusMinusRange(float min = 0.1f, float max = 0.25f)
    {
        var range = Math.Abs(max - min);
        var delta = (double)(RandomNumberGenerator.GetInt32(0, 1000000) - 500000) / 500000 * range;

        return Math.Sign(delta) * (Math.Abs(min) + Math.Abs(delta));
    }

    /// <summary>
    /// Returns the distance between two points.
    /// </summary>
    /// <param name="pt1"></param>
    /// <param name="pt2"></param>
    /// <returns></returns>
    internal static double DistanceBetweenTwoPoints(PointF pt1, PointF pt2)
    {
        double dx = pt2.X - pt1.X;
        double dy = pt2.Y - pt1.Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Returns a list of key value pairs from a string.
    /// e.g. XYZ=10, ABC=DEF GHI=13K4 
    /// ["XYZ"] = 10
    /// ["ABC"] = "DEF"
    /// ["GHI"] = "13K4"
    /// </summary>
    /// <param name="textContainingKeyValuePairs"></param>
    /// <returns></returns>
    internal static Dictionary<string, string> RegExpParseTokens(string textContainingKeyValuePairs)
    {
        Dictionary<string, string> listOfAttributes = new();

        string modifiedTarget = textContainingKeyValuePairs;

        while (regExToFindAttributePairs.IsMatch(modifiedTarget))
        {
            string result = Regex.Match(modifiedTarget, attributeValuePairsRegExPattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)).Groups[1].Value;

            if (string.IsNullOrWhiteSpace(result)) break;

            // each token is {attribute}={value}
            string[] attributePair = result.Split('=');
            listOfAttributes.Add(attributePair[0].ToString().ToUpper(), attributePair[1].Replace("\"", ""));

            // remove the data we found, and look for another attribute
            modifiedTarget = modifiedTarget[..^result.Length];
        }

        return listOfAttributes;
    }

    /// <summary>
    /// Search the attribute list for a specific attribute. If not present returns "undefined".
    /// </summary>
    /// <param name="attributes"></param>
    /// <param name="attributeToReturnValueFor"></param>
    /// <returns>The value of the attribute.</returns>
    internal static string SafeGetAttribute(Dictionary<string, string> attributes, string attributeToReturnValueFor)
    {
        // attributes are stored with UPPER() keys.
        attributeToReturnValueFor = attributeToReturnValueFor.ToUpper();

        if (attributes.TryGetValue(attributeToReturnValueFor, out string? value))
        {
            return value.Trim();
        }

        return "undefined"; // no value specified
    }
}
