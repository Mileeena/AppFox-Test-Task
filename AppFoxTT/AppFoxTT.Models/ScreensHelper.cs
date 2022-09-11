using Avalonia.Platform;

namespace AppFoxTT.Models;

public class ScreensHelper
{
   public IReadOnlyList<Screen> ScreensList { get; }

   public ScreensHelper(IReadOnlyList<Screen> screens)
   {
        ScreensList = screens;
   }
}