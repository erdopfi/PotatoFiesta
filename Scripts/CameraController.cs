using Godot;

namespace PotatoFiesta;

public partial class CameraController : Node2D
{
    public static Player TargetPlayer;
        
    private static CameraController _instance;

    public override void _Ready()
    {
        base._Ready();
        _instance = this;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (TargetPlayer == null)
        {
            return;
        }

        if (!TargetPlayer.IsDead)
        {
            GlobalPosition = GlobalPosition.Lerp(TargetPlayer.GlobalPosition, (float) delta * 10);
        }
        else
        {
            if(GameManager.Potato.TargetPlayer != null)
                GlobalPosition = GlobalPosition.Lerp(GameManager.Potato.TargetPlayer.GlobalPosition, (float) delta * 10);
        }

    }
}