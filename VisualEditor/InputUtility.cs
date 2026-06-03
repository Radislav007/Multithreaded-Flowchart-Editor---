using Raylib_cs;

namespace VisualEditor;

public static class InputUtility
{
    public static void HandleTextInput(ref string textToUpdate, bool numbersOnly)
    {
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (numbersOnly)
            {
                if (key >= 48 && key <= 57) textToUpdate += (char)key; 
            }
            else
            {
                if ((key >= 32) && (key <= 125)) textToUpdate += (char)key; 
            }
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && textToUpdate.Length > 0)
        {
            textToUpdate = textToUpdate[..^1];
        }
    }

    public static void HandleFullTextInput(ref string textToUpdate)
    {
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (key >= 32 && key <= 125) textToUpdate += (char)key; 
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && textToUpdate.Length > 0)
        {
            textToUpdate = textToUpdate[..^1];
        }
    }
}