using System;
using System.Linq;
using Godot;
using PotatoFiesta.Networking;

namespace PotatoFiesta;

public partial class Potato : Node2D
{
    [Export] private float _minExplosionTime = 20;
    [Export] private float _maxExplosionTime = 35;
    [Export] private AudioStreamPlayer2D _tickAudioStreamPlayer;
    
    private float _explosionCooldown = 20;
    private float _tickingCooldown;
    
    public Player TargetPlayer { get; private set; }

    private static readonly RandomNumberGenerator RandomNumberGenerator = new();

    public override void _Ready()
    {
        base._Ready();
        Network.OnPeerConnected += SendTargetedPlayerToClient;
        _explosionCooldown = RandomNumberGenerator.RandfRange(_minExplosionTime, _maxExplosionTime);
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Network.IsServer)
        {
            if (GameManager.AlivePlayers.Count == 0)
            {
                foreach (var player in GameManager.AlivePlayers)
                {
                    player.Spawn();
                }
                return;
            }

            if (TargetPlayer == null || TargetPlayer.IsQueuedForDeletion() || TargetPlayer.IsDead)
            {
                SetTargetPlayer(GameManager.AlivePlayers.ElementAt(RandomNumberGenerator.RandiRange(0, GameManager.AlivePlayers.Count - 1)));
            }
            else
            {
                GlobalPosition = TargetPlayer.GlobalPosition;
                _explosionCooldown -= (float) delta;
                _tickingCooldown -= (float)delta;
                
                if (_explosionCooldown < 0)
                {
                    TargetPlayer.Die();
                    _explosionCooldown = RandomNumberGenerator.RandfRange(_minExplosionTime, _maxExplosionTime);
                }else if (_tickingCooldown < 0)
                {
                    _tickingCooldown = _explosionCooldown / 5;
                    _tickAudioStreamPlayer.Play();
                }
            }
        }
        else
        {
            if (TargetPlayer != null && !TargetPlayer.IsDead)
            {
                GlobalPosition = TargetPlayer.GlobalPosition;
            }
        }
    }

    public void SetTargetPlayer(Player player)
    {
        TargetPlayer = player;
        Network.Call(this, nameof(GetTargetedPlayerFromServer), player.GetMultiplayerAuthority());
    }
    
    private void SendTargetedPlayerToClient(int playerId)
    {
        if (playerId == 1)
            return;
        
        Network.CallId(playerId, this, nameof(GetTargetedPlayerFromServer), TargetPlayer.GetMultiplayerAuthority());
    }

    [NetworkCallable(NetworkAuthenticationType.Server)]
    private void GetTargetedPlayerFromServer(long playerId)
    {
        TargetPlayer = GameManager.Players[(int)playerId];
    }
}