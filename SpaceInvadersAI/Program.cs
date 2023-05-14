using SpaceInvadersAI.AI;
using SpaceInvadersCore;
using SpaceInvadersCore.Game;
using SpaceInvadersCore.Tests;
using System.Drawing;
using System.Drawing.Imaging;

namespace SpaceInvadersAI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //   ███    ████      █      ███    █████            ███    █   █   █   █     █     ████    █████   ████     ███              █      ███
            //  █   █   █   █    █ █    █   █   █                 █     █   █   █   █    █ █    █   █   █       █   █   █   █            █ █      █
            //  █       █   █   █   █   █       █                 █     ██  █   █   █   █   █   █   █   █       █   █   █               █   █     █
            //   ███    ████    █   █   █       ████              █     █ █ █   █   █   █   █   █   █   ████    ████     ███            █   █     █
            //      █   █       █████   █       █                 █     █  ██   █   █   █████   █   █   █       █ █         █           █████     █
            //  █   █   █       █   █   █   █   █                 █     █   █    █ █    █   █   █   █   █       █  █    █   █           █   █     █
            //   ███    █       █   █    ███    █████            ███    █   █     █     █   █   ████    █████   █   █    ███            █   █    ███

            //TestHarnessForBrainSerialisation.PerformTest();
            //TestHarnessForVideo.PerformTest();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new FormSpaceInvaders());
        }
    }
}