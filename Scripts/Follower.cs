using Godot;

namespace PotatoFiesta;

[GlobalClass]
public partial class Follower : Node2D
{
    [Export] public float FollowSpeed = 10;
    [Export] public Node2D Target;

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Target == null)
            return;

        GlobalPosition = GlobalPosition.Lerp(Target.GlobalPosition, (float)delta * FollowSpeed);
    }
}