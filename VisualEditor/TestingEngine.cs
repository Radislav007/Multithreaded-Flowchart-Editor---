using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VisualEditor;

public class ExecutionState
{
    public int[] PCs { get; set; } = [];
    public Dictionary<string, int> Vars { get; set; } = [];
    public List<int> Inputs { get; set; } = [];
    public int InputIndex { get; set; } = 0;
    public List<string> Outputs { get; set; } = [];
    public List<(int choiceIndex, int totalChoices)> ProgressPath { get; set; } = [];
    public int Depth { get; set; } = 0;

    public ExecutionState Clone()
    {
        return new ExecutionState
        {
            PCs = (int[])PCs.Clone(),
            Vars = new Dictionary<string, int>(Vars),
            Inputs = Inputs,
            InputIndex = InputIndex,
            Outputs = [.. Outputs],
            ProgressPath = [.. ProgressPath],
            Depth = Depth
        };
    }
}

public class TestingEngine(EditorContext context)
{
    private readonly EditorContext _context = context;
    private CancellationTokenSource? _cts;

    public void StartExploration(int kLimit, string inputStr, string expectedOutStr)
    {
        _cts = new CancellationTokenSource();
        _context.IsExploring = true;
        _context.ExploredPathsCount = 0;
        _context.FailedPathsCount = 0;
        _context.ProgressPercentage = 0.0;

        List<int> inputs = inputStr.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => int.TryParse(s, out int v) ? v : 0).ToList();
        
        List<string> expectedOut = expectedOutStr.Split(['\n', ','], StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(s => s.Trim()).ToList();

        Task.Run(() => ExploreSpace(kLimit, inputs, expectedOut, _cts.Token));
    }

    public void StopExploration()
    {
        _cts?.Cancel();
    }

    private void ExploreSpace(int kLimit, List<int> inputs, List<string> expectedOut, CancellationToken token)
    {
        var rootState = new ExecutionState
        {
            PCs = new int[_context.Threads.Count],
            Inputs = inputs
        };

        for (int i = 0; i < _context.Threads.Count; i++)
        {
            rootState.PCs[i] = _context.Threads[i].Nodes.Count > 0 ? 0 : -1;
        }

        Stack<ExecutionState> stack = new();
        stack.Push(rootState);

        while (stack.Count > 0)
        {
            if (token.IsCancellationRequested) break;

            var state = stack.Pop();
            _context.ProgressPercentage = CalculateProgress(state.ProgressPath);

            bool allTerminated = state.PCs.All(pc => pc == -1);

            if (allTerminated || state.Depth >= kLimit)
            {
                _context.ExploredPathsCount++;
                if (!CheckOutput(state.Outputs, expectedOut))
                {
                    _context.FailedPathsCount++;
                }
                continue;
            }

            var activeThreads = new List<int>();
            for (int i = 0; i < state.PCs.Length; i++)
            {
                if (state.PCs[i] != -1) activeThreads.Add(i);
            }

            if (activeThreads.Count == 0) continue;

            for (int i = 0; i < activeThreads.Count; i++)
            {
                int tIdx = activeThreads[i];
                var nextState = Step(state, tIdx);
                
                var newProgress = new List<(int, int)>(state.ProgressPath)
                {
                    (i, activeThreads.Count)
                };
                nextState.ProgressPath = newProgress;
                
                stack.Push(nextState);
            }
        }

        if (!token.IsCancellationRequested)
        {
            _context.ProgressPercentage = 100.0;
        }
        _context.IsExploring = false;
    }

    private ExecutionState Step(ExecutionState current, int threadIdx)
    {
        var next = current.Clone();
        var thread = _context.Threads[threadIdx];
        var node = thread.Nodes[current.PCs[threadIdx]];

        string v1 = node.Var1 ?? "";
        string v2 = node.Var2 ?? "";
        int c = int.TryParse(node.ConstantValue, out int val) ? val : 0;

        switch (node.Type)
        {
            case NodeType.AssignVar:
                next.Vars[v1] = next.Vars.GetValueOrDefault(v2);
                break;
            case NodeType.AssignConst:
                next.Vars[v1] = c;
                break;
            case NodeType.Input:
                if (next.InputIndex < next.Inputs.Count)
                {
                    next.Vars[v1] = next.Inputs[next.InputIndex++];
                }
                break;
            case NodeType.Print:
                next.Outputs.Add($"{v1} = {next.Vars.GetValueOrDefault(v1)}");
                break;
        }

        var edges = thread.Edges.Where(e => e.StartNode == node).ToList();
        Node? targetNode = null;

        if (node.Type == NodeType.BranchEqual || node.Type == NodeType.BranchLess)
        {
            bool condition = false;
            int v1Val = next.Vars.GetValueOrDefault(v1);
            if (node.Type == NodeType.BranchEqual) condition = v1Val == c;
            else condition = v1Val < c;

            var edge = edges.FirstOrDefault(e => e.IsTruePath == condition);
            if (edge != null) targetNode = edge.EndNode;
        }
        else
        {
            targetNode = edges.FirstOrDefault()?.EndNode;
        }

        if (targetNode != null)
        {
            next.PCs[threadIdx] = thread.Nodes.IndexOf(targetNode);
        }
        else
        {
            next.PCs[threadIdx] = -1;
        }

        next.Depth++;
        return next;
    }

    private bool CheckOutput(List<string> actual, List<string> expected)
    {
        if (actual.Count != expected.Count) return false;
        for (int i = 0; i < actual.Count; i++)
        {
            if (actual[i] != expected[i]) return false;
        }
        return true;
    }

    private double CalculateProgress(List<(int choiceIndex, int totalChoices)> currentPath)
    {
        double progress = 0;
        double currentSlice = 1.0;
        foreach (var step in currentPath)
        {
            progress += step.choiceIndex * (currentSlice / step.totalChoices);
            currentSlice /= step.totalChoices;
        }
        return Math.Round(progress * 100.0, 2);
    }
}