using Godot;

namespace PotatoFiesta;

public partial class CameraController : Node2D
{
    public static Node2D TargetNode;
        
    private static CameraController _instance;

    public override void _Ready()
    {
        base._Ready();
        _instance = this;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (TargetNode == null)
            return;
            
        GlobalPosition = GlobalPosition.Lerp(TargetNode.GlobalPosition, (float) delta * 10);
    }
}