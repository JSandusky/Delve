using System;
using System.Threading.Tasks;

namespace Delve
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //using (var game1 = new Delve())
            //    game1.Run();
            //using (var game1 = new ImGuiCLITest.ImGuiViewport())
            //    game1.Run();
            //using (var game1 = new Testing_Project.Game1())
            //    game1.Run();
            using (var game1 = new EscherEdit())
                game1.Run();
        }
    }
#endif
}
