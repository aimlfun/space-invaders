using System.Diagnostics;
using System.Text;

namespace SpaceInvadersCore.Game;

/// <summary>
/// Sprites used in Space Invaders, taken from the 1978 original EEPROM.
/// See: https://www.computerarcheology.com/Arcade/SpaceInvaders/Code.html#1B32, the sprites are near the bottom of the page.
/// </summary>
public static class OriginalSpritesFrom1978
{
    //    █      ███    █████    ███             ███    ████    ████     ███    █████   █████    ███
    //   ██     █   █       █   █   █           █   █   █   █   █   █     █       █     █       █   █
    //    █     █   █      █    █   █           █       █   █   █   █     █       █     █       █
    //    █      ████     █      ███             ███    ████    ████      █       █     ████     ███
    //    █         █    █      █   █               █   █       █ █       █       █     █           █
    //    █        █     █      █   █           █   █   █       █  █      █       █     █       █   █
    //   ███    ███      █       ███             ███    █       █   █    ███      █     █████    ███
    
    /// <summary>
    /// Contains sprites that are used in the game, indexed by a name.
    /// </summary>
    public static readonly Dictionary<string, Sprite> Sprites;

    /// <summary>
    /// Static Constructor. Registers all the sprites.
    /// </summary>
    static OriginalSpritesFrom1978()
    {        
        // "@hex" is the offset in the EPROM where the sprite is stored.
        Sprites = new()
        {
            { "AlienSprCYA", new Sprite(8, "00 03 04 78 14 13 08 1A 3D 68 FC FC 68 3D 1A 00" ) }, // @1BA0
            { "AlienSprCYB", new Sprite(8, "00 00 03 04 78 14 0B 19 3A 6D FA FA 6D 3A 19 00" ) }, // @1BD0
            
            // these are the aliens (3 types, A B C) and 2 animation frames
            { "AlienSprA0", new Sprite(8, "00 00 39 79 7A 6E EC FA FA EC 6E 7A 79 39 00 00" ) }, // @1C00
            { "AlienSprB0", new Sprite(8, "00 00 00 78 1D BE 6C 3C 3C 3C 6C BE 1D 78 00 00" ) }, // @1C10
            { "AlienSprC0", new Sprite(8, "00 00 00 00 19 3A 6D FA FA 6D 3A 19 00 00 00 00" ) }, // @1C20               
            { "AlienSprA1", new Sprite(8, "00 00 38 7A 7F 6D EC FA FA EC 6D 7F 7A 38 00 00" ) }, // @1C30
            { "AlienSprB1", new Sprite(8, "00 00 00 0E 18 BE 6D 3D 3C 3D 6D BE 18 0E 00 00" ) }, // @1C40
            { "AlienSprC1", new Sprite(8, "00 00 00 00 1A 3D 68 FC FC 68 3D 1A 00 00 00 00" ) }, // @1C50

            // player ship, with two animation frames for it blowing up
            { "Player", new Sprite(8, "00 00 0F 1F 1F 1F 1F 7F FF 7F 1F 1F 1F 1F 0F 00" ) }, // @1C60
            { "PlrBlowupSprites-1", new Sprite(8, "00 04 01 13 03 07 B3 0F 2F 03 2F 49 04 03 00 01" ) }, // @1C70
            { "PlrBlowupSprites-2", new Sprite(8, "40 08 05 A3 0A 03 5B 0F 27 27 0B 4B 40 84 11 48" ) }, // @1C80
            
            // player shot
            { "PlayerShotSpr", new Sprite(8, "0F" ) }, // @1C90
            { "ShotExploding", new Sprite(8, "99 3C 7E 3D BC 3E 7C 99" ) }, // @1C91
            
            { "AlienExplode", new Sprite(8, "00 08 49 22 14 81 42 00 42 81 14 22 49 08 00 00" ) }, // @1CC0
            
            // Squiggly bullets
            { "SquigglyShot-1", new Sprite(8, "44 AA 10" ) }, // @1CD0
            { "SquigglyShot-2", new Sprite(8, "88 54 22" ) }, // @1CD3
            { "SquigglyShot-3", new Sprite(8, "10 AA 44" ) }, // @1CD6
            { "SquigglyShot-4", new Sprite(8, "22 54 88" ) }, // @1CD9
            
            // alien shot explosion
            { "AShotExplo", new Sprite(8, "4A 15 BE 3F 5E 25" ) }, // @1CDC

            // Plunger bullets
            { "PlungerShot-1", new Sprite(8, "04 FC 04" ) }, // @1CE2
            { "PlungerShot-2", new Sprite(8, "10 FC 10" ) }, // @1CE5
            { "PlungerShot-3", new Sprite(8, "20 FC 20" ) }, // @1CE8
            { "PlungerShot-4", new Sprite(8, "80 FC 80" ) }, // @1CEB
      
            // Rolling bullets
            { "RollShot-1", new Sprite(8, "00 FE 00" ) }, // @1CEE
            { "RollShot-2", new Sprite(8, "24 FE 12" ) }, // @1CF1
            { "RollShot-3", new Sprite(8, "00 FE 00" ) }, // @1CF4
            { "RollShot-4", new Sprite(8, "48 FE 90" ) }, // @1CF7
            
            // shields
            { "Shield", new Sprite(16, "FF 0F FF 1F FF 3F FF 7F FF FF FC FF F8 FF F0 FF F0 FF F0 FF F0 FF " +
                                       "F0 FF F0 FF F0 FF F8 FF FC FF FF FF FF FF FF 7F FF 3F FF 1F FF 0F")
            }, // @1D20

            // flying saucer, with explosion
            { "SpriteSaucer", new Sprite(8,"00 00 00 00 04 0C 1E 37 3E 7C 74 7E 7E 74 7C 3E 37 1E 0C 04 00 00 00 00") }, // @1D64
            { "SpriteSaucerExp", new Sprite(8, "00 22 00 A5 40 08 98 3D B6 3C 36 1D 10 48 62 B6 1D 98 08 42 90 08 00 00") }, // @1D7C

            // letters, numbers and symbols (SI font)
            { "A", new Sprite(8, "00 1F 24 44 24 1F 00 00") }, // @1E00
            { "B", new Sprite(8, "00 7F 49 49 49 36 00 00") }, // @1E08
            { "C", new Sprite(8, "00 3E 41 41 41 22 00 00") }, // @1E10
            { "D", new Sprite(8, "00 7F 41 41 41 3E 00 00") }, // @1E18
            { "E", new Sprite(8, "00 7F 49 49 49 41 00 00") }, // @1E20
            { "F", new Sprite(8, "00 7F 48 48 48 40 00 00") }, // @1E28
            { "G", new Sprite(8, "00 3E 41 41 45 47 00 00") }, // @1E30
            { "H", new Sprite(8, "00 7F 08 08 08 7F 00 00") }, // @1E38

            { "I", new Sprite(8, "00 00 41 7F 41 00 00 00") }, // @1E40
            { "J", new Sprite(8, "00 02 01 01 01 7E 00 00") }, // @1E48                
            { "K", new Sprite(8, "00 7F 08 14 22 41 00 00") }, // @1E50
            { "L", new Sprite(8, "00 7F 01 01 01 01 00 00") }, // @1E50
            { "M", new Sprite(8, "00 7F 20 18 20 7F 00 00") }, // @1E58
            { "N", new Sprite(8, "00 7F 10 08 04 7F 00 00") }, // @1E60
            { "O", new Sprite(8, "00 3E 41 41 41 3E 00 00") }, // @1E68
            { "P", new Sprite(8, "00 7F 48 48 48 30 00 00") }, // @1E70
            
            { "Q", new Sprite(8, "00 3E 41 45 42 3D 00 00") }, // @1E80
            { "R", new Sprite(8, "00 7F 48 4C 4A 31 00 00") }, // @1E88
            { "S", new Sprite(8, "00 32 49 49 49 26 00 00") }, // @1E90
            { "T", new Sprite(8, "00 40 40 7F 40 40 00 00") }, // @1E98
            { "U", new Sprite(8, "00 7E 01 01 01 7E 00 00") }, // @1EA0
            { "V", new Sprite(8, "00 7C 02 01 02 7C 00 00") }, // @1EA8
            { "W", new Sprite(8, "00 7F 02 0C 02 7F 00 00") }, // @1EB0
            { "X", new Sprite(8, "00 63 14 08 14 63 00 00") }, // @1EB8

            { "Y", new Sprite(8, "00 60 10 0F 10 60 00 00") }, // @1EC0
            { "Z", new Sprite(8, "00 43 45 49 51 61 00 00") }, // @1EC8
            { "0", new Sprite(8, "00 3E 45 49 51 3E 00 00") }, // @1ED0
            { "1", new Sprite(8, "00 00 21 7F 01 00 00 00") }, // @1ED8
            { "2", new Sprite(8, "00 23 45 49 49 31 00 00") }, // @1EE0
            { "3", new Sprite(8, "00 42 41 49 59 66 00 00") }, // @1EE8
            { "4", new Sprite(8, "00 0C 14 24 7F 04 00 00") }, // @1EF0
            { "5", new Sprite(8, "00 72 51 51 51 4E 00 00") }, // @1EF8

            { "6", new Sprite(8, "00 1E 29 49 49 46 00 00") }, // @1F00
            { "7", new Sprite(8, "00 40 47 48 50 60 00 00") }, // @1F08
            { "8", new Sprite(8, "00 36 49 49 49 36 00 00") }, // @1F10
            { "9", new Sprite(8, "00 31 49 49 4A 3C 00 00") }, // @1F18
            { "<", new Sprite(8, "00 08 14 22 41 00 00 00") }, // @1F20
            { ">", new Sprite(8, "00 00 41 22 14 08 00 00") }, // @1F28
            { " ", new Sprite(8, "00 00 00 00 00 00 00 00") }, // @1F30
            { "=", new Sprite(8, "00 14 14 14 14 14 00 00") }, // @1F38

            { "*", new Sprite(8, "00 22 14 7F 14 22 00 00") }, // @1F40
            { "}", new Sprite(8, "00 03 04 78 04 03 00 00") }, // @1F48 - Y upside down

            { "AlienSprCA", new Sprite(8, "60 10 0F 10 60 30 18 1A 3D 68 FC FC 68 3D 1A 00" ) }, // @1F80
            { "AlienSprCB", new Sprite(8, "00 60 10 0F 10 60 38 19 3A 6D FA FA 6D 3A 19 00" ) }, // @1FB0
            { "?", new Sprite(8,"00 20 40 4D 50 20 00 00") },
            { "-", new Sprite(8,"00 08 08 08 08 08 00 00") }
        };
    }

    /// <summary>
    /// Outputs all the registered sprites to the debug output.
    /// </summary>
    internal static void DumpSprites()
    {
        foreach (string name in Sprites.Keys)
        {
            Debug.WriteLine(name);
            Debug.WriteLine(Sprites[name]);
        }
    }

    /// <summary>
    /// Enables us to write a line of text in the original font.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private static string[] SpritePixelsByRow(char c)
    {
        Sprite sprite = OriginalSpritesFrom1978.Sprites[c.ToString()];

        List<string> sb = new();

        // write complete pixels array as string[]
        for (int y = 0; y < sprite.HeightInPX; y++)
        {
            string pixelRowAsText = "";

            for (int x = 0; x < sprite.WidthInPX; x++)
            {
                pixelRowAsText += (sprite.Pixels[x, y] == 1 ? "█" : " ");
            }

            sb.Add(pixelRowAsText);
        }

        return sb.ToArray();
    }

    /// <summary>
    /// Returns a string with the text rendered in the original font.
    /// </summary>
    public static string RenderTextInSpaceInvaderFont(string text, string prefix = "")
    {
        text = text.ToUpper(); // all chars in the font are uppercase
        List<string[]> lines = new();

        foreach (char c in text)
        {
            // if the character is not in the font, we'll just skip it. What else did you expect?
            if (OriginalSpritesFrom1978.Sprites.ContainsKey(c.ToString())) lines.Add(SpritePixelsByRow(c));
        }

        int heightInPX = lines[0].Length;

        StringBuilder sb = new();
        for (int y = 0; y < heightInPX; y++)
        {
            if (!string.IsNullOrWhiteSpace(prefix)) sb.Append(prefix);

            for (int charIndex = 0; charIndex < lines.Count; charIndex++)
            {
                sb.Append(lines[charIndex][y]);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Text to sprite font, and output as comment.
    /// </summary>
    public static void OutputAsComment()
    {
        Console.WriteLine("Enter the text, and it will be rendered in the original font in C# comment style!");

        while (true)
        {
            string? text = Console.ReadLine();
            if (string.IsNullOrEmpty(text)) break;

            Console.WriteLine(RenderTextInSpaceInvaderFont(text, "// "));
        }
    }

}
