using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace VisualEditor;

public class UIManager(EditorContext context)
{
    private readonly EditorContext _context = context;
    private readonly TestingEngine _testingEngine = new TestingEngine(context);
    
    private List<Button> _mainButtons =
        [
            new(20, 10, 120, 40, "Add Node \u25BC"),
            new(150, 10, 120, 40, "Add Thread"),
            new(280, 10, 100, 40, "Save"),
            new(390, 10, 100, 40, "Load"),
            new(500, 10, 120, 40, "Compile"),
            new(630, 10, 140, 40, "Test Panel \u25B6")
        ];
    private List<Button> _addNodeMenu =
        [
            new(20, 50, 120, 30, "V1 = V2"),
            new(20, 80, 120, 30, "V = C"),
            new(20, 110, 120, 30, "INPUT V"),
            new(20, 140, 120, 30, "PRINT V"),
            new(20, 170, 120, 30, "V == C"),
            new(20, 200, 120, 30, "V < C")
        ];
    private bool _isAddNodeMenuOpen = false;

    private string _newVarInputText = "";
    private bool _isNewVarInputActive = false;
    private bool _isConstInputActive = false;
    
    private int _activeDropdown = 0; 
    private Rectangle _dropdownRec;

    private bool _testInputActive = false;
    private bool _testExpectedActive = false;
    private bool _kLimitActive = false;

    public bool Update(Vector2 screenMousePos)
    {
        bool isLeftPressed = Raylib.IsMouseButtonPressed(MouseButton.Left);

        if (_context.ShowPopup)
        {
            if (isLeftPressed)
            {
                int boxWidth = 500;
                int boxHeight = 200;
                int boxX = _context.ScreenWidth / 2 - boxWidth / 2;
                int boxY = _context.ScreenHeight / 2 - boxHeight / 2;
                
                Rectangle okBtn = new(boxX + boxWidth / 2 - 50, boxY + boxHeight - 60, 100, 40);
                if (Raylib.CheckCollisionPointRec(screenMousePos, okBtn))
                {
                    _context.ClosePopup();
                }
            }
            return true; 
        }

        bool uiHandledClick = false;

        if (_isNewVarInputActive) 
        {
            InputUtility.HandleTextInput(ref _newVarInputText, false);
        }
        else if (_isConstInputActive && _context.EditingNode != null) 
        {
            InputUtility.HandleTextInput(ref _context.EditingNode.ConstantValue, true);
        }
        else if (_testInputActive) 
        {
            string t = _context.TestInputData; 
            InputUtility.HandleFullTextInput(ref t); 
            _context.TestInputData = t;
        }
        else if (_testExpectedActive) 
        {
            string t = _context.TestExpectedOutput; 
            InputUtility.HandleFullTextInput(ref t); 
            _context.TestExpectedOutput = t;
        }
        else if (_kLimitActive) 
        {
            string t = _context.LimitK; 
            InputUtility.HandleTextInput(ref t, true); 
            _context.LimitK = t;
        }

        if (isLeftPressed && _activeDropdown != 0)
        {
            uiHandledClick = HandleDropdownClick(screenMousePos);
        }

        if (isLeftPressed && !uiHandledClick)
        {
            uiHandledClick = HandleSidePanelClick(screenMousePos);
        }

        if (isLeftPressed && _isAddNodeMenuOpen && !uiHandledClick)
        {
            uiHandledClick = HandleAddNodeMenuClick(screenMousePos);
        }

        if (isLeftPressed && !uiHandledClick)
        {
            uiHandledClick = HandleThreadTabsAndButtons(screenMousePos, isLeftPressed);
        }

        return uiHandledClick;
    }

    private bool HandleThreadTabsAndButtons(Vector2 screenMousePos, bool isLeftPressed)
    {
        float tabX = 20;
        for (int i = 0; i < _context.Threads.Count; i++)
        {
            Rectangle tabRec = new(tabX, _context.ToolbarHeight, 100, 30);
            if (Raylib.CheckCollisionPointRec(screenMousePos, tabRec))
            {
                _context.SwitchThread(i);
                return true;
            }
            tabX += 105;
        }

        if (_mainButtons[0].IsClicked(screenMousePos, isLeftPressed)) 
        { 
            _isAddNodeMenuOpen = !_isAddNodeMenuOpen; 
            return true; 
        }
        else if (_mainButtons[1].IsClicked(screenMousePos, isLeftPressed)) 
        {
            int newThreadNum = _context.Threads.Count + 1;
            _context.Threads.Add(new ThreadData($"Thread {newThreadNum}"));
            _context.SwitchThread(_context.Threads.Count - 1); 
            return true;
        }
        else if (_mainButtons[2].IsClicked(screenMousePos, isLeftPressed)) 
        {
            string path = FileDialogHelper.SaveFile("Save Project", "*.json", "project_data.json");
            if (!string.IsNullOrWhiteSpace(path))
            {
                ProjectSerializer.Save(_context, path);
                _context.ShowMessage($"Graph data successfully saved to:\n{path}", false);
            }
            return true;
        }
        else if (_mainButtons[3].IsClicked(screenMousePos, isLeftPressed)) 
        {
            string path = FileDialogHelper.OpenFile("Load Project", "*.json");
            if (!string.IsNullOrWhiteSpace(path))
            {
                ProjectSerializer.Load(_context, path);
                if (!_context.ShowPopup) _context.ShowMessage("Project loaded successfully.", false);
            }
            return true;
        }
        else if (_mainButtons[4].IsClicked(screenMousePos, isLeftPressed)) 
        {
            string savePath = FileDialogHelper.SaveFile("Save Compiled Python App", "*.py", "generated_app.py");
            if (!string.IsNullOrWhiteSpace(savePath))
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"temp_graph_{Guid.NewGuid()}.json");
                ProjectSerializer.Save(_context, tempFile);
                
                var transpiler = new Transpiler(_context);
                if (!transpiler.CompileToFile(savePath, out string errorMsg))
                {
                    _context.ShowMessage($"Compilation Failed:\n\n{errorMsg}", true);
                }
                else
                {
                    _context.ShowMessage($"Compilation successful!\nGenerated Python app saved to:\n{savePath}", false);
                }

                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            return true;
        }
        else if (_mainButtons[5].IsClicked(screenMousePos, isLeftPressed))
        {
            _context.IsTestingMode = !_context.IsTestingMode;
            if (_context.IsTestingMode) _context.EditingNode = null; 
            return true;
        }

        return false;
    }

    private bool HandleSidePanelClick(Vector2 screenMousePos)
    {
        Rectangle panelBounds = new(_context.ScreenWidth - _context.PanelWidth, _context.ToolbarHeight, _context.PanelWidth, _context.ScreenHeight - _context.ToolbarHeight);

        if (Raylib.CheckCollisionPointRec(screenMousePos, panelBounds))
        {
            if (_context.IsTestingMode)
            {
                Rectangle inRec = new(panelBounds.X + 20, panelBounds.Y + 60, panelBounds.Width - 40, 30);
                Rectangle outRec = new(panelBounds.X + 20, panelBounds.Y + 120, panelBounds.Width - 40, 30);
                Rectangle kRec = new(panelBounds.X + 20, panelBounds.Y + 180, panelBounds.Width - 40, 30);
                
                _testInputActive = Raylib.CheckCollisionPointRec(screenMousePos, inRec);
                _testExpectedActive = Raylib.CheckCollisionPointRec(screenMousePos, outRec);
                _kLimitActive = Raylib.CheckCollisionPointRec(screenMousePos, kRec);

                Rectangle startBtn = new(panelBounds.X + 20, panelBounds.Y + 230, 110, 40);
                Rectangle stopBtn = new(panelBounds.X + 140, panelBounds.Y + 230, 110, 40);

                if (Raylib.CheckCollisionPointRec(screenMousePos, startBtn) && !_context.IsExploring)
                {
                    int k = int.TryParse(_context.LimitK, out int kv) ? kv : 10;
                    _testingEngine.StartExploration(k, _context.TestInputData, _context.TestExpectedOutput);
                }
                else if (Raylib.CheckCollisionPointRec(screenMousePos, stopBtn) && _context.IsExploring)
                {
                    _testingEngine.StopExploration();
                }
            }
            else if (_context.EditingNode != null)
            {
                Rectangle newVarRec = new(panelBounds.X + 20, panelBounds.Y + 60, panelBounds.Width - 100, 30);
                _isNewVarInputActive = Raylib.CheckCollisionPointRec(screenMousePos, newVarRec);
                
                Rectangle addBtnRec = new(panelBounds.X + panelBounds.Width - 70, panelBounds.Y + 60, 50, 30);
                if (Raylib.CheckCollisionPointRec(screenMousePos, addBtnRec) && !string.IsNullOrWhiteSpace(_newVarInputText))
                {
                    if (!_context.GlobalVariables.Contains(_newVarInputText)) _context.GlobalVariables.Add(_newVarInputText);
                    _newVarInputText = "";
                    _isNewVarInputActive = false;
                }

                float propY = panelBounds.Y + 140;
                bool needsVar2 = _context.EditingNode?.Type == NodeType.AssignVar;
                bool needsConst = _context.EditingNode?.Type == NodeType.AssignConst
                    || _context.EditingNode?.Type == NodeType.BranchEqual
                    || _context.EditingNode?.Type == NodeType.BranchLess;

                Rectangle var1DropRec = new(panelBounds.X + 20, propY + 50, panelBounds.Width - 40, 30);
                if (Raylib.CheckCollisionPointRec(screenMousePos, var1DropRec))
                {
                    _activeDropdown = 1;
                    _dropdownRec = new Rectangle(var1DropRec.X, var1DropRec.Y + 30, var1DropRec.Width, 0); 
                }

                if (needsVar2)
                {
                    Rectangle var2DropRec = new(panelBounds.X + 20, propY + 110, panelBounds.Width - 40, 30);
                    if (Raylib.CheckCollisionPointRec(screenMousePos, var2DropRec))
                    {
                        _activeDropdown = 2;
                        _dropdownRec = new Rectangle(var2DropRec.X, var2DropRec.Y + 30, var2DropRec.Width, 0);
                    }
                }

                _isConstInputActive = false;
                if (needsConst)
                {
                    Rectangle constRec = new(panelBounds.X + 20, propY + 110, panelBounds.Width - 40, 30);
                    if (Raylib.CheckCollisionPointRec(screenMousePos, constRec)) _isConstInputActive = true;
                }
            }
            return true;
        }

        _isNewVarInputActive = false;
        _isConstInputActive = false;
        _testInputActive = false;
        _testExpectedActive = false;
        _kLimitActive = false;
        return false;
    }

    private bool HandleDropdownClick(Vector2 screenMousePos)
    {
        bool clickedInsideDropdown = false;
        for (int i = 0; i < _context.GlobalVariables.Count; i++)
        {
            Rectangle itemRec = new(_dropdownRec.X, _dropdownRec.Y + (i * 30), _dropdownRec.Width, 30);
            if (Raylib.CheckCollisionPointRec(screenMousePos, itemRec))
            {
                if (_context.EditingNode != null)
                {
                    if (_activeDropdown == 1) _context.EditingNode.Var1 = _context.GlobalVariables[i];
                    if (_activeDropdown == 2) _context.EditingNode.Var2 = _context.GlobalVariables[i];
                }

                clickedInsideDropdown = true;
                _activeDropdown = 0; 
                break;
            }
        }
        if (!clickedInsideDropdown) _activeDropdown = 0; 
        return true;
    }

    private bool HandleAddNodeMenuClick(Vector2 screenMousePos)
    {
        bool isLeftPressed = Raylib.IsMouseButtonPressed(MouseButton.Left);
        for (int i = 0; i < _addNodeMenu.Count; i++)
        {
            if (_addNodeMenu[i].IsClicked(screenMousePos, isLeftPressed))
            {
                Vector2 spawnPos = Raylib.GetScreenToWorld2D(new Vector2((_context.ScreenWidth - _context.PanelWidth) / 2f - 70, _context.ScreenHeight / 2f - 30), _context.Camera);

                switch (i)
                {
                    case 0: _context.Nodes.Add(new Node(spawnPos.X, spawnPos.Y, Color.LightGray, NodeType.AssignVar)); break;
                    case 1: _context.Nodes.Add(new Node(spawnPos.X, spawnPos.Y, Color.Beige, NodeType.AssignConst)); break;
                    case 2: _context.Nodes.Add(new Node(spawnPos.X, spawnPos.Y, Color.SkyBlue, NodeType.Input)); break;
                    case 3: _context.Nodes.Add(new Node(spawnPos.X, spawnPos.Y, Color.Lime, NodeType.Print)); break;
                    case 4: _context.Nodes.Add(new Node(spawnPos.X, spawnPos.Y, Color.Violet, NodeType.BranchEqual)); break;
                    case 5: _context.Nodes.Add(new Node(spawnPos.X, spawnPos.Y, Color.Orange, NodeType.BranchLess)); break;
                }
                _isAddNodeMenuOpen = false;
                return true;
            }
        }
        if (!_mainButtons[0].IsClicked(screenMousePos, isLeftPressed)) _isAddNodeMenuOpen = false;
        return false;
    }

    public void Draw(Vector2 screenMousePos)
    {
        Rectangle panelBounds = new(_context.ScreenWidth - _context.PanelWidth, _context.ToolbarHeight, _context.PanelWidth, _context.ScreenHeight - _context.ToolbarHeight);

        if (_context.IsTestingMode)
        {
            DrawTestingPanel(panelBounds);
        }
        else if (_context.EditingNode != null)
        {
            DrawNodePropertiesPanel(panelBounds, screenMousePos);
        }

        Raylib.DrawRectangle(0, 0, _context.ScreenWidth, (int)_context.ToolbarHeight, Color.DarkBlue);
        foreach (var button in _mainButtons) 
        { 
            if (button.Text.StartsWith("Test Panel") && _context.IsTestingMode)
            {
                Raylib.DrawRectangleRec(button.Bounds, Color.SkyBlue);
                Raylib.DrawText(button.Text, (int)button.Bounds.X + 15, (int)button.Bounds.Y + 12, 16, Color.Black);
                Raylib.DrawRectangleLinesEx(button.Bounds, 2, Color.Black);
            }
            else button.Draw(); 
        }
        
        float tabX = 20;
        for (int i = 0; i < _context.Threads.Count; i++)
        {
            Rectangle tabRec = new(tabX, _context.ToolbarHeight, 100, 30);
            bool isActive = i == _context.ActiveThreadIndex;
            
            Raylib.DrawRectangleRec(tabRec, isActive ? Color.White : Color.LightGray);
            Raylib.DrawRectangleLinesEx(tabRec, 1, Color.DarkGray);
            Raylib.DrawText(_context.Threads[i].Name, (int)tabX + 10, (int)_context.ToolbarHeight + 8, 16, isActive ? Color.Black : Color.DarkGray);
            
            tabX += 105;
        }

        if (_isAddNodeMenuOpen) { foreach (var btn in _addNodeMenu) { btn.Draw(); } }

        if (_context.ShowPopup) DrawPopup(screenMousePos);
    }

    private void DrawTestingPanel(Rectangle bounds)
    {
        Raylib.DrawRectangleRec(bounds, Color.LightGray);
        Raylib.DrawRectangleLinesEx(bounds, 2, Color.DarkGray);

        Raylib.DrawText("State-Space Explorer", (int)bounds.X + 20, (int)bounds.Y + 20, 20, Color.Black);
        
        Raylib.DrawText("Inputs (comma separated):", (int)bounds.X + 20, (int)bounds.Y + 60, 16, Color.DarkGray);
        Rectangle inRec = new(bounds.X + 20, bounds.Y + 80, bounds.Width - 40, 30);
        Raylib.DrawRectangleRec(inRec, _testInputActive ? Color.White : Color.RayWhite);
        Raylib.DrawRectangleLinesEx(inRec, 1, _testInputActive ? Color.Blue : Color.DarkGray);
        Raylib.DrawText(_context.TestInputData, (int)inRec.X + 5, (int)inRec.Y + 5, 16, Color.Black);

        Raylib.DrawText("Expected Output:", (int)bounds.X + 20, (int)bounds.Y + 120, 16, Color.DarkGray);
        Rectangle outRec = new(bounds.X + 20, bounds.Y + 140, bounds.Width - 40, 30);
        Raylib.DrawRectangleRec(outRec, _testExpectedActive ? Color.White : Color.RayWhite);
        Raylib.DrawRectangleLinesEx(outRec, 1, _testExpectedActive ? Color.Blue : Color.DarkGray);
        Raylib.DrawText(_context.TestExpectedOutput, (int)outRec.X + 5, (int)outRec.Y + 5, 16, Color.Black);

        Raylib.DrawText("Max Depth (K, 1-20):", (int)bounds.X + 20, (int)bounds.Y + 180, 16, Color.DarkGray);
        Rectangle kRec = new(bounds.X + 20, bounds.Y + 200, bounds.Width - 40, 30);
        Raylib.DrawRectangleRec(kRec, _kLimitActive ? Color.White : Color.RayWhite);
        Raylib.DrawRectangleLinesEx(kRec, 1, _kLimitActive ? Color.Blue : Color.DarkGray);
        Raylib.DrawText(_context.LimitK, (int)kRec.X + 5, (int)kRec.Y + 5, 16, Color.Black);

        Rectangle startBtn = new(bounds.X + 20, bounds.Y + 250, 110, 40);
        Raylib.DrawRectangleRec(startBtn, _context.IsExploring ? Color.Gray : Color.DarkGreen);
        Raylib.DrawText("Start Test", (int)startBtn.X + 15, (int)startBtn.Y + 10, 16, Color.White);

        Rectangle stopBtn = new(bounds.X + 140, bounds.Y + 250, 110, 40);
        Raylib.DrawRectangleRec(stopBtn, !_context.IsExploring ? Color.Gray : Color.Red);
        Raylib.DrawText("Stop & Eval", (int)stopBtn.X + 10, (int)stopBtn.Y + 10, 16, Color.White);

        Raylib.DrawLine((int)bounds.X, (int)bounds.Y + 310, (int)(bounds.X + bounds.Width), (int)bounds.Y + 310, Color.DarkGray);
        Raylib.DrawText("Execution Results", (int)bounds.X + 20, (int)bounds.Y + 330, 20, Color.Black);

        Raylib.DrawText($"Status: {(_context.IsExploring ? "Running..." : "Stopped")}", (int)bounds.X + 20, (int)bounds.Y + 370, 16, _context.IsExploring ? Color.Orange : Color.DarkGray);
        Raylib.DrawText($"Paths Explored: {_context.ExploredPathsCount}", (int)bounds.X + 20, (int)bounds.Y + 400, 16, Color.Black);
        Raylib.DrawText($"Failed Paths: {_context.FailedPathsCount}", (int)bounds.X + 20, (int)bounds.Y + 430, 16, _context.FailedPathsCount > 0 ? Color.Red : Color.DarkGreen);
        
        Raylib.DrawText($"Verified Space (K):", (int)bounds.X + 20, (int)bounds.Y + 470, 16, Color.DarkBlue);
        Raylib.DrawText($"{_context.ProgressPercentage:F2}%", (int)bounds.X + 20, (int)bounds.Y + 500, 30, Color.DarkBlue);
    }

    private void DrawNodePropertiesPanel(Rectangle panelBounds, Vector2 screenMousePos)
    {
        Raylib.DrawRectangleRec(panelBounds, Color.LightGray);
        Raylib.DrawRectangleLinesEx(panelBounds, 2, Color.DarkGray);

        Raylib.DrawText("Global Variables", (int)panelBounds.X + 20, (int)panelBounds.Y + 20, 20, Color.Black);
        
        Rectangle newVarRec = new(panelBounds.X + 20, panelBounds.Y + 60, panelBounds.Width - 100, 30);
        Raylib.DrawRectangleRec(newVarRec, _isNewVarInputActive ? Color.White : Color.RayWhite);
        Raylib.DrawRectangleLinesEx(newVarRec, 1, _isNewVarInputActive ? Color.Blue : Color.DarkGray);
        Raylib.DrawText(_newVarInputText, (int)newVarRec.X + 5, (int)newVarRec.Y + 5, 20, Color.Black);

        Rectangle addBtnRec = new(panelBounds.X + panelBounds.Width - 70, panelBounds.Y + 60, 50, 30);
        Raylib.DrawRectangleRec(addBtnRec, Color.DarkBlue);
        Raylib.DrawText("ADD", (int)addBtnRec.X + 10, (int)addBtnRec.Y + 8, 16, Color.White);

        string varListStr = "Vars: " + string.Join(", ", _context.GlobalVariables);
        if (_context.GlobalVariables.Count == 0) varListStr = "Vars: (None)";
        Raylib.DrawText(varListStr, (int)panelBounds.X + 20, (int)panelBounds.Y + 100, 16, Color.DarkGray);

        Raylib.DrawLine((int)panelBounds.X, (int)panelBounds.Y + 130, (int)(panelBounds.X + panelBounds.Width), (int)panelBounds.Y + 130, Color.DarkGray);
        float propY = panelBounds.Y + 140;
        Raylib.DrawText("Node Properties", (int)panelBounds.X + 20, (int)propY, 20, Color.Black);

        bool needsVar2 = _context.EditingNode!.Type == NodeType.AssignVar;
        bool needsConst = _context.EditingNode.Type == NodeType.AssignConst || _context.EditingNode.Type == NodeType.BranchEqual || _context.EditingNode.Type == NodeType.BranchLess;

        Raylib.DrawText(needsVar2 ? "Target Variable (V1):" : "Variable (V):", (int)panelBounds.X + 20, (int)propY + 30, 16, Color.DarkGray);
        Rectangle var1DropRec = new(panelBounds.X + 20, propY + 50, panelBounds.Width - 40, 30);
        Raylib.DrawRectangleRec(var1DropRec, Color.White);
        Raylib.DrawRectangleLinesEx(var1DropRec, 1, Color.DarkGray);
        Raylib.DrawText(string.IsNullOrEmpty(_context.EditingNode.Var1) ? "Select..." : _context.EditingNode.Var1, (int)var1DropRec.X + 5, (int)var1DropRec.Y + 5, 20, Color.Black);

        if (needsVar2)
        {
            Raylib.DrawText("Source Variable (V2):", (int)panelBounds.X + 20, (int)propY + 90, 16, Color.DarkGray);
            Rectangle var2DropRec = new(panelBounds.X + 20, propY + 110, panelBounds.Width - 40, 30);
            Raylib.DrawRectangleRec(var2DropRec, Color.White);
            Raylib.DrawRectangleLinesEx(var2DropRec, 1, Color.DarkGray);
            Raylib.DrawText(string.IsNullOrEmpty(_context.EditingNode.Var2) ? "Select..." : _context.EditingNode.Var2, (int)var2DropRec.X + 5, (int)var2DropRec.Y + 5, 20, Color.Black);
        }
        else if (needsConst)
        {
            Raylib.DrawText("Constant Value (C):", (int)panelBounds.X + 20, (int)propY + 90, 16, Color.DarkGray);
            Rectangle constRec = new(panelBounds.X + 20, propY + 110, panelBounds.Width - 40, 30);
            Raylib.DrawRectangleRec(constRec, _isConstInputActive ? Color.White : Color.RayWhite);
            Raylib.DrawRectangleLinesEx(constRec, 1, _isConstInputActive ? Color.Blue : Color.DarkGray);
            Raylib.DrawText(_context.EditingNode.ConstantValue, (int)constRec.X + 5, (int)constRec.Y + 5, 20, Color.Black);
        }

        if (_activeDropdown != 0 && _context.GlobalVariables.Count > 0)
        {
            _dropdownRec.Height = _context.GlobalVariables.Count * 30;
            Raylib.DrawRectangleRec(_dropdownRec, Color.White);
            Raylib.DrawRectangleLinesEx(_dropdownRec, 1, Color.Black);

            for (int i = 0; i < _context.GlobalVariables.Count; i++)
            {
                Rectangle itemRec = new(_dropdownRec.X, _dropdownRec.Y + (i * 30), _dropdownRec.Width, 30);
                bool isHovered = Raylib.CheckCollisionPointRec(screenMousePos, itemRec);
                if (isHovered) Raylib.DrawRectangleRec(itemRec, Color.SkyBlue);
                Raylib.DrawText(_context.GlobalVariables[i], (int)itemRec.X + 5, (int)itemRec.Y + 5, 20, Color.Black);
            }
        }
    }

    private void DrawPopup(Vector2 screenMousePos)
    {
        Raylib.DrawRectangle(0, 0, _context.ScreenWidth, _context.ScreenHeight, new Color(0, 0, 0, 150));
        
        int boxWidth = 500;
        int boxHeight = 220;
        int boxX = _context.ScreenWidth / 2 - boxWidth / 2;
        int boxY = _context.ScreenHeight / 2 - boxHeight / 2;

        Raylib.DrawRectangle(boxX, boxY, boxWidth, boxHeight, Color.RayWhite);
        Raylib.DrawRectangleLinesEx(new Rectangle(boxX, boxY, boxWidth, boxHeight), 4, _context.IsErrorPopup ? Color.Red : Color.DarkGreen);

        string title = _context.IsErrorPopup ? "VALIDATION ERROR" : "SUCCESS";
        Raylib.DrawText(title, boxX + 20, boxY + 20, 24, _context.IsErrorPopup ? Color.Red : Color.DarkGreen);

        string[] lines = _context.PopupMessage.Split('\n');
        int textY = boxY + 70;
        foreach (var line in lines)
        {
            Raylib.DrawText(line, boxX + 20, textY, 16, Color.Black);
            textY += 20;
        }

        Rectangle okBtn = new(boxX + boxWidth / 2 - 50, boxY + boxHeight - 60, 100, 40);
        bool isHovered = Raylib.CheckCollisionPointRec(screenMousePos, okBtn);
        Raylib.DrawRectangleRec(okBtn, isHovered ? Color.LightGray : Color.DarkGray);
        Raylib.DrawRectangleLinesEx(okBtn, 2, Color.Black);
        Raylib.DrawText("OK", (int)okBtn.X + 35, (int)okBtn.Y + 10, 20, Color.White);
    }
}