using System.Collections.Generic;
using Raylib_cs;

namespace VisualEditor;

public class EditorContext
{
    public List<ThreadData> Threads { get; } = [];
    public int ActiveThreadIndex { get; private set; } = 0;

    public ThreadData ActiveThread => Threads[ActiveThreadIndex];
    
    public List<Node> Nodes => ActiveThread.Nodes;
    public List<Edge> Edges => ActiveThread.Edges;

    public List<string> GlobalVariables { get; } = [];
    
    public Node? SelectedNode { get; set; }
    public Node? EditingNode { get; set; }
    
    public Camera2D Camera;
    public int ScreenWidth { get; set; } = 1280;
    public int ScreenHeight { get; set; } = 720;
    
    public float ToolbarHeight { get; } = 60;
    public float PanelWidth { get; } = 280;

    public bool ShowPopup { get; private set; } = false;
    public string PopupMessage { get; private set; } = "";
    public bool IsErrorPopup { get; private set; } = false;

    public bool IsTestingMode { get; set; } = false;
    public string TestInputData { get; set; } = "10";
    public string TestExpectedOutput { get; set; } = "V1 = 10";
    public string LimitK { get; set; } = "20";
    public bool IsExploring { get; set; } = false;
    public int ExploredPathsCount { get; set; } = 0;
    public int FailedPathsCount { get; set; } = 0;
    public double ProgressPercentage { get; set; } = 0.0;

    public EditorContext()
    {
        Threads.Add(new ThreadData("Thread 1"));
    }

    public void SwitchThread(int index)
    {
        if (index >= 0 && index < Threads.Count)
        {
            ActiveThreadIndex = index;
            SelectedNode = null;
            EditingNode = null;
        }
    }

    public void ShowMessage(string message, bool isError = false)
    {
        PopupMessage = message;
        IsErrorPopup = isError;
        ShowPopup = true;
    }

    public void ClosePopup()
    {
        ShowPopup = false;
    }
}