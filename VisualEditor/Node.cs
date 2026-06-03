using System.Numerics;
using Raylib_cs;

namespace VisualEditor;
public class Node(float x, float y, Color bgColor, NodeType type)
{
    public Rectangle Bounds = new(x, y, 160, 60);
    public Color BgColor = bgColor;
    public bool IsSelected = false;
    public NodeType Type = type;

    public string Var1 = "";
    public string Var2 = "";
    public string ConstantValue = "0";

    public string GetDisplayText()
    {
        string v1 = string.IsNullOrEmpty(Var1) ? "V1" : Var1;
        string v2 = string.IsNullOrEmpty(Var2) ? "V2" : Var2;
        string v = string.IsNullOrEmpty(Var1) ? "V" : Var1;
        string c = string.IsNullOrEmpty(ConstantValue) ? "C" : ConstantValue;

        return Type switch
        {
            NodeType.AssignVar => $"{v1} = {v2}",
            NodeType.AssignConst => $"{v} = {c}",
            NodeType.Input => $"INPUT {v}",
            NodeType.Print => $"PRINT {v}",
            NodeType.BranchEqual => $"{v} == {c}",
            NodeType.BranchLess => $"{v} < {c}",
            _ => "Unknown",
        };
    }

    public Vector2 GetCenter() => new(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);

    public void Draw()
    {
        if (IsSelected)
        {
            Rectangle highlight = new(Bounds.X - 4, Bounds.Y - 4, Bounds.Width + 8, Bounds.Height + 8);
            Raylib.DrawRectangleRec(highlight, Color.Yellow);
        }

        Raylib.DrawRectangle((int)Bounds.X + 4, (int)Bounds.Y + 4, (int)Bounds.Width, (int)Bounds.Height, new Color(0, 0, 0, 50));
        Raylib.DrawRectangleRec(Bounds, BgColor);
        Raylib.DrawRectangleLinesEx(Bounds, 2, Color.Black);

        string text = GetDisplayText();
        int fontSize = 20;
        int textWidth = Raylib.MeasureText(text, fontSize);
        int textX = (int)(Bounds.X + (Bounds.Width / 2) - (textWidth / 2));
        int textY = (int)(Bounds.Y + (Bounds.Height / 2) - (fontSize / 2));
        
        Raylib.DrawText(text, textX, textY, fontSize, Color.Black);
    }
}