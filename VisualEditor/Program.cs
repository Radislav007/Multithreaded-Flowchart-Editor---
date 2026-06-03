using System.Numerics;
using Raylib_cs;

namespace VisualEditor;

class Program
{
    static void Main(string[] args)
    {
        var context = new EditorContext();
        
        Raylib.InitWindow(context.ScreenWidth, context.ScreenHeight, "Multithreaded Flowchart Editor");
        Raylib.SetTargetFPS(60);

        context.Camera = new Camera2D
        {
            Zoom = 1.0f,
            Offset = new Vector2(0, 0),
            Target = new Vector2(0, 0)
        };

        var uiManager = new UIManager(context);
        var canvasInteraction = new CanvasInteraction(context);

        while (!Raylib.WindowShouldClose())
        {
            Vector2 screenMousePos = Raylib.GetMousePosition();
            Vector2 worldMousePos = Raylib.GetScreenToWorld2D(screenMousePos, context.Camera);

            bool uiHandledClick = uiManager.Update(screenMousePos);
            canvasInteraction.Update(screenMousePos, uiHandledClick);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(245, 245, 245, 255));

            canvasInteraction.Draw(worldMousePos);
            uiManager.Draw(screenMousePos);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
