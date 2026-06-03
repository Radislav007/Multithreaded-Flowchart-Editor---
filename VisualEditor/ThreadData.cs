using System.Collections.Generic;

namespace VisualEditor;

public class ThreadData(string name)
{
    public string Name { get; set; } = name;
    public List<Node> Nodes { get; } = [];
    public List<Edge> Edges { get; } = [];
}