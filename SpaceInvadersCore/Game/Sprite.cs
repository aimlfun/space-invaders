using System.Text;

namespace SpaceInvadersCore.Game;

/// <summary>
/// Represents a sprite.
/// </summary>
public class Sprite
{
    /// <summary>
    /// Stores the height of the sprite in pixels.
    /// </summary>
    internal readonly int HeightInPX;

    /// <summary>
    /// Stores the width of the sprite in pixels.
    /// </summary>
    internal readonly int WidthInPX;

    /// <summary>
    /// Contains the pixels of the sprite.
    /// </summary>
    internal readonly byte[,] Pixels;

    /// <summary>
    /// Instantiate sprite from the definition from the original EEPROMs.
    /// </summary>
    /// <param name="height"></param>
    /// <param name="originalGameByteDefinitionExpressedInHexBytesSpaceDelimited"></param>
    public Sprite(int height, string originalGameByteDefinitionExpressedInHexBytesSpaceDelimited)
    {
        // trailing or leading spaces will break the tokenisation, so we fix up.
        originalGameByteDefinitionExpressedInHexBytesSpaceDelimited = originalGameByteDefinitionExpressedInHexBytesSpaceDelimited.Trim().Replace("  ", " ");

        string[] hexByteTokens = originalGameByteDefinitionExpressedInHexBytesSpaceDelimited.Split(' ');

        HeightInPX = height;
        WidthInPX = hexByteTokens.Length/(height/8);

        Pixels = new byte[WidthInPX, HeightInPX];

        int x = 0;
        int y = 0;

        // the original game stores the sprite pixels as bits, and the EEPROM dump has them in hex.
        // we need to convert the hex to bits, and then store them in the pixels array.
        foreach (string token in hexByteTokens)
        {
            byte value = Convert.ToByte(token, 16); // hex to byte

            // each byte contains 8 pixels, so we need to iterate over each bit.
            for (int bit = 0; bit < 8; bit++)
            {
                byte pixel = (byte)(value >> bit & 1); // returns 0 or 1, depending on the bit at position i.

                Pixels[y, HeightInPX - x - 1] = pixel; // y & x are swapped to rotate it.
                x++;

                // when we've reached the end of the raster line, move to the next raster line.
                if (x == HeightInPX)
                {
                    x = 0;
                    y++;
                }
            }
        }
    }

    /// <summary>
    /// Render bitmap visually as string, for verification.
    /// </summary>
    /// <returns></returns>
    public override string? ToString()
    {
        StringBuilder sb = new(HeightInPX + 1);
        sb.AppendLine($"width: {WidthInPX} X height: {HeightInPX}");

        // write complete pixels array to sb
        for (int y = 0; y < HeightInPX; y++)
        {
            for (int x = 0; x < WidthInPX; x++)
            {
                sb.Append(Pixels[x, y] == 1 ? "█" : " ");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}