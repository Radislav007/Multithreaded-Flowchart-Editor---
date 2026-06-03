using System.Numerics;
using Raylib_cs;

namespace VisualEditor;

public class Button(float x, float y, float width, float height, string text)
{
    public Rectangle Bounds = new(x, y, width, height);
    public string Text = text;
    public Color BgColor = Color.LightGray;
    public Color HoverColor = Color.Gray;

    public void Draw()
    {
        Vector2 mousePos = Raylib.GetMousePosition();
        bool isHovered = Raylib.CheckCollisionPointRec(mousePos, Bounds);

        Raylib.DrawRectangleRec(Bounds, isHovered ? HoverColor : BgColor);
        Raylib.DrawRectangleLinesEx(Bounds, 1, Color.DarkGray);

        int fontSize = 16;
        int textWidth = Raylib.MeasureText(Text, fontSize);
        int textX = (int)(Bounds.X + (Bounds.Width / 2) - (textWidth / 2));
        int textY = (int)(Bounds.Y + (Bounds.Height / 2) - (fontSize / 2));
        
        Raylib.DrawText(Text, textX, textY, fontSize, Color.Black);
    }

    public bool IsClicked(Vector2 mousePos, bool isMousePressed) => 
        isMousePressed && Raylib.CheckCollisionPointRec(mousePos, Bounds);
}