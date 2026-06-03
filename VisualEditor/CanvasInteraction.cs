using System;
using System.Numerics;
using Raylib_cs;

namespace VisualEditor;

public class CanvasInteraction(EditorContext context)
{
    private EditorContext _context = context;
    private bool _isDraggingNode = false;
    private bool _isPanningMap = false;
    private Node? _connectingStartNode = null;
    private Vector2 _dragOffset = Vector2.Zero;
    private double _lastClickTime = 0;
    private Node? _lastClickedNode = null;

    public void Update(Vector2 screenMousePos, bool uiHandledClick)
    {
        if (_connectingStartNode != null && !_context.Nodes.Contains(_connectingStartNode))
        {
            _connectingStartNode = null;
        }

        Vector2 worldMousePos = Raylib.GetScreenToWorld2D(screenMousePos, _context.Camera);
        
        bool isLeftPressed = Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool isLeftDown = Raylib.IsMouseButtonDown(MouseButton.Left);
        bool isRightPressed = Raylib.IsMouseButtonPressed(MouseButton.Right);
        bool isRightReleased = Raylib.IsMouseButtonReleased(MouseButton.Right);

        if (!uiHandledClick && Raylib.IsKeyPressed(KeyboardKey.Delete))
        {
            if (_context.SelectedNode != null)
            {
                Node nodeToDelete = _context.SelectedNode;
                
                _context.Edges.RemoveAll(e => e.StartNode == nodeToDelete || e.EndNode == nodeToDelete);
                
                _context.Nodes.Remove(nodeToDelete);

                if (_context.EditingNode == nodeToDelete) _context.EditingNode = null;
                if (_connectingStartNode == nodeToDelete) _connectingStartNode = null;
                
                _context.SelectedNode = null;
                _isDraggingNode = false;
                uiHandledClick = true;
            }
        }

        HandleZoom(screenMousePos);

        if (Raylib.IsMouseButtonReleased(MouseButton.Left)) _isDraggingNode = false;

        if (isLeftPressed && !uiHandledClick)
        {
            HandleNodeSelection(worldMousePos);
        }

        if (isLeftDown && _context.SelectedNode != null && _isDraggingNode)
        {
            _context.SelectedNode.Bounds.X = worldMousePos.X - _dragOffset.X;
            _context.SelectedNode.Bounds.Y = worldMousePos.Y - _dragOffset.Y;
        }

        if (isRightPressed && !uiHandledClick)
        {
            HandleRightClick(worldMousePos);
        }

        if (_isPanningMap && Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            Vector2 delta = Raylib.GetMouseDelta();
            _context.Camera.Target.X -= delta.X / _context.Camera.Zoom;
            _context.Camera.Target.Y -= delta.Y / _context.Camera.Zoom;
        }

        if (isRightReleased)
        {
            HandleRightRelease(worldMousePos);
        }
    }

    private void HandleZoom(Vector2 screenMousePos)
    {
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            Vector2 mouseWorldPosBeforeZoom = Raylib.GetScreenToWorld2D(screenMousePos, _context.Camera);
            _context.Camera.Offset = screenMousePos;
            _context.Camera.Target = mouseWorldPosBeforeZoom;
            
            _context.Camera.Zoom += wheel * 0.1f;
            if (_context.Camera.Zoom < 0.2f) _context.Camera.Zoom = 0.2f;
            if (_context.Camera.Zoom > 3.0f) _context.Camera.Zoom = 3.0f;
        }
    }

    private void HandleNodeSelection(Vector2 worldMousePos)
    {
        _context.SelectedNode = null;
        _isDraggingNode = false; 
        foreach (var node in _context.Nodes) { node.IsSelected = false; }

        for (int i = _context.Nodes.Count - 1; i >= 0; i--)
        {
            if (Raylib.CheckCollisionPointRec(worldMousePos, _context.Nodes[i].Bounds))
            {
                _context.SelectedNode = _context.Nodes[i];
                _context.SelectedNode.IsSelected = true;
                _dragOffset.X = worldMousePos.X - _context.SelectedNode.Bounds.X;
                _dragOffset.Y = worldMousePos.Y - _context.SelectedNode.Bounds.Y;
                _isDraggingNode = true; 

                if (_lastClickedNode == _context.SelectedNode && (Raylib.GetTime() - _lastClickTime) < 0.3)
                {
                    _context.EditingNode = _context.SelectedNode;
                }
                _lastClickedNode = _context.SelectedNode;
                _lastClickTime = Raylib.GetTime();
                break; 
            }
        }
        if (_context.SelectedNode == null) _context.EditingNode = null;
    }

    private void HandleRightClick(Vector2 worldMousePos)
    {
        for (int i = _context.Nodes.Count - 1; i >= 0; i--)
        {
            if (Raylib.CheckCollisionPointRec(worldMousePos, _context.Nodes[i].Bounds)) 
            { 
                _connectingStartNode = _context.Nodes[i]; 
                break; 
            }
        }
        if (_connectingStartNode == null) _isPanningMap = true;
    }

    private void HandleRightRelease(Vector2 worldMousePos)
    {
        if (_connectingStartNode != null)
        {
            Node? targetNode = null;
            for (int i = _context.Nodes.Count - 1; i >= 0; i--)
            {
                if (Raylib.CheckCollisionPointRec(worldMousePos, _context.Nodes[i].Bounds)) 
                { 
                    targetNode = _context.Nodes[i]; 
                    break; 
                }
            }

            if (targetNode != null && targetNode != _connectingStartNode)
            {
                bool edgeExists = _context.Edges.Exists(e => e.StartNode == _connectingStartNode && e.EndNode == targetNode);
                if (!edgeExists)
                {
                    bool isBranch = _connectingStartNode.Type == NodeType.BranchEqual || _connectingStartNode.Type == NodeType.BranchLess;
                    bool hasTrueEdge = _context.Edges.Exists(e => e.StartNode == _connectingStartNode && e.IsTruePath);
                    bool isTruePath = !isBranch || !hasTrueEdge;

                    _context.Edges.Add(new Edge(_connectingStartNode, targetNode, isTruePath));
                }
                else 
                {
                    _context.Edges.RemoveAll(e => e.StartNode == _connectingStartNode && e.EndNode == targetNode);
                }
            }
            _connectingStartNode = null; 
        }
        _isPanningMap = false;
    }

    public void Draw(Vector2 worldMousePos)
    {
        Raylib.BeginMode2D(_context.Camera);
        
        foreach (var edge in _context.Edges) { edge.Draw(); }
        
        if (_connectingStartNode != null)
        {
            Vector2 startCenter = _connectingStartNode.GetCenter();
            Vector2 start = _connectingStartNode.Bounds.X != 0 ? _connectingStartNode.GetCenter() : startCenter; 
            
            Vector2 dir = worldMousePos - startCenter;
            if (dir.X != 0 || dir.Y != 0)
            {
                float hw = _connectingStartNode.Bounds.Width / 2f;
                float hh = _connectingStartNode.Bounds.Height / 2f;
                float scale = Math.Min(Math.Abs(hw / dir.X), Math.Abs(hh / dir.Y));
                start = startCenter + dir * scale;
            }

            Raylib.DrawLineEx(start, worldMousePos, 3.0f, Color.LightGray);
            Raylib.DrawCircleV(worldMousePos, 6.0f, Color.Gray);
        }

        foreach (var node in _context.Nodes) { node.Draw(); }

        Raylib.EndMode2D();
    }
}