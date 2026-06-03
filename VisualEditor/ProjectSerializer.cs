using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Raylib_cs;

namespace VisualEditor;

public class ProjectDTO
{
    public List<string> GlobalVariables { get; set; } = [];
    public List<ThreadDTO> Threads { get; set; } = [];
}

public class ThreadDTO
{
    public string Name { get; set; } = "";
    public List<NodeDTO> Nodes { get; set; } = [];
    public List<EdgeDTO> Edges { get; set; } = [];
}

public class NodeDTO
{
    public float X { get; set; }
    public float Y { get; set; }
    public NodeType Type { get; set; }
    public string Var1 { get; set; } = "";
    public string Var2 { get; set; } = "";
    public string ConstantValue { get; set; } = "";
}

public class EdgeDTO
{
    public int StartNodeIndex { get; set; }
    public int EndNodeIndex { get; set; }
    public bool IsTruePath { get; set; }
}

public static class ProjectSerializer
{
    public static void Save(EditorContext context, string filePath)
    {
        var dto = new ProjectDTO { GlobalVariables = context.GlobalVariables.ToList() };
        
        foreach (var t in context.Threads)
        {
            var threadDto = new ThreadDTO { Name = t.Name };
            
            foreach (var n in t.Nodes)
            {
                threadDto.Nodes.Add(new NodeDTO 
                { 
                    X = n.Bounds.X, 
                    Y = n.Bounds.Y, 
                    Type = n.Type, 
                    Var1 = n.Var1 ?? "", 
                    Var2 = n.Var2 ?? "", 
                    ConstantValue = n.ConstantValue ?? "" 
                });
            }
            
            foreach (var e in t.Edges)
            {
                threadDto.Edges.Add(new EdgeDTO 
                { 
                    StartNodeIndex = t.Nodes.IndexOf(e.StartNode),
                    EndNodeIndex = t.Nodes.IndexOf(e.EndNode),
                    IsTruePath = e.IsTruePath
                });
            }
            dto.Threads.Add(threadDto);
        }

        File.WriteAllText(filePath, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void Load(EditorContext context, string filePath)
    {
        if (!File.Exists(filePath)) 
        {
            context.ShowMessage($"File not found: {filePath}", true);
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<ProjectDTO>(json)
                ?? throw new System.Exception("Deserialized data is null.");
            context.GlobalVariables.Clear();
            context.GlobalVariables.AddRange(dto.GlobalVariables);
            
            context.Threads.Clear();
            foreach (var tDto in dto.Threads)
            {
                var thread = new ThreadData(tDto.Name);
                foreach (var nDto in tDto.Nodes)
                {
                    Color bgColor = nDto.Type switch
                    {
                        NodeType.AssignVar => Color.LightGray,
                        NodeType.AssignConst => Color.Beige,
                        NodeType.Input => Color.SkyBlue,
                        NodeType.Print => Color.Lime,
                        NodeType.BranchEqual => Color.Violet,
                        NodeType.BranchLess => Color.Orange,
                        _ => Color.LightGray
                    };

                    thread.Nodes.Add(new Node(nDto.X, nDto.Y, bgColor, nDto.Type) 
                    { 
                        Var1 = nDto.Var1, 
                        Var2 = nDto.Var2, 
                        ConstantValue = nDto.ConstantValue 
                    });
                }
                
                foreach (var eDto in tDto.Edges)
                {
                    if (eDto.StartNodeIndex >= 0 && eDto.StartNodeIndex < thread.Nodes.Count &&
                        eDto.EndNodeIndex >= 0 && eDto.EndNodeIndex < thread.Nodes.Count)
                    {
                        thread.Edges.Add(new Edge(thread.Nodes[eDto.StartNodeIndex], thread.Nodes[eDto.EndNodeIndex], eDto.IsTruePath));
                    }
                }
                context.Threads.Add(thread);
            }
            
            if (context.Threads.Count == 0) context.Threads.Add(new ThreadData("Thread 1"));
            context.SwitchThread(0);
        }
        catch (System.Exception ex)
        {
            context.ShowMessage($"Failed to load project:\n{ex.Message}", true);
        }
    }
}