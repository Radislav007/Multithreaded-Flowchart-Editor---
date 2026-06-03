using System;
using System.Numerics;
using Raylib_cs;

namespace VisualEditor;

public class Edge(Node start, Node end, bool isTruePath = true)
{
    public Node StartNode { get; set; } = start;
    public Node EndNode { get; set; } = end;
    public bool IsTruePath { get; set; } = isTruePath;

    private Vector2 GetRectEdgePoint(Rectangle rect, Vector2 target)
    {
        Vector2 center = new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        Vector2 dir = target - center;

        if (dir.X == 0 && dir.Y == 0) return center;

        float hw = rect.Width / 2f + 2f; 
        float hh = rect.Height / 2f + 2f;

        float scaleX = Math.Abs(hw / dir.X);
        float scaleY = Math.Abs(hh / dir.Y);
        float scale = Math.Min(scaleX, scaleY);

        return center + dir * scale;
    }

    public void Draw()
    {
        Vector2 startCenter = StartNode.GetCenter();
        Vector2 endCenter = EndNode.GetCenter();

        Vector2 start = GetRectEdgePoint(StartNode.Bounds, endCenter);
        Vector2 end = GetRectEdgePoint(EndNode.Bounds, startCenter);

        Raylib.DrawLineEx(start, end, 3.0f, Color.DarkGray);

        Vector2 dir = Vector2.Normalize(end - start);
        Vector2 arrowPos = end - (dir * 6f); 
        
        Raylib.DrawCircleV(arrowPos, 6.0f, Color.Red);
        Raylib.DrawCircleLines((int)arrowPos.X, (int)arrowPos.Y, 6.0f, Color.Black);

        if (StartNode.Type == NodeType.BranchEqual || StartNode.Type == NodeType.BranchLess)
        {
            Vector2 midPoint = new((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            Raylib.DrawCircleV(midPoint, 10f, Color.White);
            Raylib.DrawCircleLines((int)midPoint.X, (int)midPoint.Y, 10f, Color.DarkGray);
            Raylib.DrawText(IsTruePath ? "T" : "F", (int)midPoint.X - 4, (int)midPoint.Y - 6, 16, IsTruePath ? Color.Green : Color.Red);
        }
    }
}