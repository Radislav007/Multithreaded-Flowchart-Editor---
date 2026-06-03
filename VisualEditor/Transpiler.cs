using System;
using System.IO;
using System.Linq;
using System.Text;

namespace VisualEditor;

public class Transpiler(EditorContext context)
{
    private EditorContext _context = context;

    public bool CompileToFile(string filePath, out string errorMessage)
    {
        errorMessage = "";
        try 
        {
            for (int i = 0; i < _context.Threads.Count; i++)
            {
                var t = _context.Threads[i];
                if (t.Nodes.Count == 0) continue; 

                foreach (var n in t.Nodes)
                {
                    bool needsVar1 = n.Type != NodeType.AssignConst;
                    bool needsVar2 = n.Type == NodeType.AssignVar;

                    if (needsVar1 && string.IsNullOrWhiteSpace(n.Var1))
                    {
                        errorMessage = $"Error in {t.Name}:\nNode '{n.GetDisplayText()}' is missing a Target Variable (V1).";
                        return false;
                    }
                    if (needsVar2 && string.IsNullOrWhiteSpace(n.Var2))
                    {
                        errorMessage = $"Error in {t.Name}:\nNode '{n.GetDisplayText()}' is missing a Source Variable (V2).";
                        return false;
                    }

                    if (n.Type == NodeType.BranchEqual || n.Type == NodeType.BranchLess)
                    {
                        int trueCount = t.Edges.Count(e => e.StartNode == n && e.IsTruePath);
                        int falseCount = t.Edges.Count(e => e.StartNode == n && !e.IsTruePath);
                        
                        if (trueCount == 0 || falseCount == 0)
                        {
                            errorMessage = $"Error in {t.Name}:\nBranch node '{n.GetDisplayText()}' must have\nboth a True (T) and False (F) connection.";
                            return false;
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("import threading");
            sb.AppendLine();
            sb.AppendLine("console_lock = threading.Lock()");
            
            foreach (var v in _context.GlobalVariables)
            {
                sb.AppendLine($"{v} = 0");
            }
            sb.AppendLine();

            for (int i = 0; i < _context.Threads.Count; i++)
            {
                sb.AppendLine($"def thread_{i}_worker():");
                
                if (_context.GlobalVariables.Count > 0)
                {
                    sb.AppendLine($"    global {string.Join(", ", _context.GlobalVariables)}");
                }
                
                var threadData = _context.Threads[i];
                var nodes = threadData.Nodes;
                var edges = threadData.Edges;

                if (nodes.Count == 0)
                {
                    sb.AppendLine("    pass");
                    sb.AppendLine();
                    continue;
                }

                Node entryNode = nodes[0];
                sb.AppendLine($"    state = {nodes.IndexOf(entryNode)}");
                sb.AppendLine("    while True:");

                for (int n = 0; n < nodes.Count; n++)
                {
                    var node = nodes[n];
                    string ifStmt = n == 0 ? "if" : "elif";
                    sb.AppendLine($"        {ifStmt} state == {n}:");
                    
                    string v1 = string.IsNullOrEmpty(node.Var1) ? "0" : node.Var1;
                    string v2 = string.IsNullOrEmpty(node.Var2) ? "0" : node.Var2;
                    string c = string.IsNullOrEmpty(node.ConstantValue) ? "0" : node.ConstantValue;

                    switch (node.Type)
                    {
                        case NodeType.AssignVar:
                            sb.AppendLine($"            {v1} = {v2}");
                            break;
                        case NodeType.AssignConst:
                            sb.AppendLine($"            {v1} = {c}");
                            break;
                        case NodeType.Input:
                            sb.AppendLine("            with console_lock:");
                            sb.AppendLine("                try:");
                            sb.AppendLine($"                    {v1} = int(input(\"Input {v1}: \"))");
                            sb.AppendLine("                except ValueError:");
                            sb.AppendLine("                    pass");
                            break;
                        case NodeType.Print:
                            sb.AppendLine("            with console_lock:");
                            sb.AppendLine($"                print(\"{v1} = \" + str({v1}))");
                            break;
                    }

                    var outgoingEdges = edges.Where(e => e.StartNode == node).ToList();

                    if (node.Type == NodeType.BranchEqual || node.Type == NodeType.BranchLess)
                    {
                        string condition = node.Type == NodeType.BranchEqual ? "==" : "<";
                        var trueEdge = outgoingEdges.FirstOrDefault(e => e.IsTruePath);
                        var falseEdge = outgoingEdges.FirstOrDefault(e => !e.IsTruePath);

                        if (trueEdge != null)
                        {
                            sb.AppendLine($"            if {v1} {condition} {c}:");
                            sb.AppendLine($"                state = {nodes.IndexOf(trueEdge.EndNode)}");
                        }
                        if (falseEdge != null)
                        {
                            sb.AppendLine(trueEdge != null ? "            else:" : "            if True:");
                            sb.AppendLine($"                state = {nodes.IndexOf(falseEdge.EndNode)}");
                        }
                        if (trueEdge == null && falseEdge == null)
                        {
                            sb.AppendLine("            return");
                        }
                        else if ((trueEdge != null && falseEdge == null) || (trueEdge == null && falseEdge != null))
                        {
                            sb.AppendLine(trueEdge != null ? "            else:" : "            else:");
                            sb.AppendLine("                return");
                        }
                    }
                    else
                    {
                        var edge = outgoingEdges.FirstOrDefault();
                        if (edge != null)
                        {
                            sb.AppendLine($"            state = {nodes.IndexOf(edge.EndNode)}");
                        }
                        else
                        {
                            sb.AppendLine("            return");
                        }
                    }
                }
                sb.AppendLine("        else:");
                sb.AppendLine("            break");
                sb.AppendLine();
            }

            sb.AppendLine("if __name__ == \"__main__\":");
            sb.AppendLine("    threads = []");
            for (int i = 0; i < _context.Threads.Count; i++)
            {
                sb.AppendLine($"    t{i} = threading.Thread(target=thread_{i}_worker)");
                sb.AppendLine($"    threads.append(t{i})");
                sb.AppendLine($"    t{i}.start()");
            }
            for (int i = 0; i < _context.Threads.Count; i++)
            {
                sb.AppendLine($"    t{i}.join()");
            }

            File.WriteAllText(filePath, sb.ToString());
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"System exception during compilation:\n{ex.Message}";
            return false;
        }
    }
}