using Godot;
using System;

public partial class GamePerformanceDebug : CanvasLayer
{
    [Export] private Label _debugTextLabel;
    
    // Start with the debugger hidden by default
    [Export] private bool _showDebug = false;

    public override void _Ready()
    {
        Visible = _showDebug;
    }

    public override void _Input(InputEvent @event)
    {
        // Toggle the debug overlay on/off using the F3 key
        if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.F3 && keyEvent.Pressed && !keyEvent.Echo)
        {
            _showDebug = !_showDebug;
            Visible = _showDebug;
        }
    }

    public override void _Process(double delta)
    {
        // Don't waste CPU cycles calculating strings if the UI is hidden
        if (!_showDebug || _debugTextLabel == null) return;

        // 1. Rendering Metrics
        double fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
        double frameTimeMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        double drawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);

        // 2. Node Lifecycle Metrics
        double nodeCount = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        double orphans = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        // 3. Memory Metrics (Converted from Bytes to Megabytes)
        double godotMemoryMB = Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1048576.0;
        double csharpMemoryMB = GC.GetTotalMemory(false) / 1048576.0;

        // Build the formatted string
        string debugString = $"FPS: {fps}\n" +
                             $"Frame Time: {frameTimeMs:0.00} ms\n" +
                             $"Draw Calls: {drawCalls}\n" +
                             $"Nodes: {nodeCount}\n" +
                             $"Orphans: {orphans}\n" +
                             $"Engine Mem: {godotMemoryMB:0.00} MB\n" +
                             $"C# Mem: {csharpMemoryMB:0.00} MB";

        _debugTextLabel.Text = debugString;
    }
}