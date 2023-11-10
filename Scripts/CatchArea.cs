using Godot;
using PotatoFiesta.Networking;

namespace PotatoFiesta;

public partial class CatchArea : Area2D
{
    private float _cooldown;
    
    public override void _Ready()
    {
        base._Ready();
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _cooldown -= (float) delta;
    }

    private void OnBodyEntered(Node2D enteredNode)
    {
        if (!Network.IsServer)
            return;

        if (_cooldown > 0)
            return;
        
        if (enteredNode is not Player player)
            return;

        GameManager.Potato.SetTargetPlayer(player);
        _cooldown = 0.5f;
    }
}