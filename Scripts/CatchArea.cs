using Godot;
using PotatoFiesta.Networking;

namespace PotatoFiesta;

public partial class CatchArea : Area2D
{
    private float _coolDown;
    
    public override void _Ready()
    {
        base._Ready();
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _coolDown -= (float) delta;
    }

    private void OnBodyEntered(Node2D enteredNode)
    {
        if (!Network.IsServer)
            return;

        if (_coolDown > 0)
            return;
        
        if (enteredNode is not Player player)
            return;

        GameManager.Potato.SetTargetPlayer(player);
        _coolDown = 0.5f;
    }
}