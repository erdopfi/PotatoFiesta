using System;
using System.Linq;
using Godot;
using PotatoFiesta.Networking;

namespace PotatoFiesta;

public partial class Potato : Node2D
{
    [Export] private Sprite2D _potatoSprite;
    [Export] private Sprite2D _crownSprite;
    
    [Export] private float _minExplosionTime = 20;
    [Export] private float _maxExplosionTime = 35;
    [Export] private AudioStreamPlayer2D _tickAudioStreamPlayer;
    [Export] private AudioStreamPlayer _musicAudioStreamPlayer;
    [Export] private AudioStreamPlayer2D _getPositiveAudioStreamPlayer;
    [Export] private AudioStreamPlayer2D _getNegativeAudioStreamPlayer;
    [Export] private Area2D _catchArea;
    
    private float _explosionCooldown = 20;
    private float _catchCooldown;
    private float _tickingCooldown;
    private float _tickAmount;

    private bool _isTicking;
    private Action OnCountdownStart;
    
    public Player TargetPlayer { get; private set; }

    private static readonly RandomNumberGenerator RandomNumberGenerator = new();

    public override void _Ready()
    {
        base._Ready();
        Network.OnPeerConnected += SendTargetedPlayerToClient;
        Network.OnPeerConnected += SendExplosionCooldownToClient;
        GameManager.OnPlayerDeath += SelectRandomPlayer;
        GameManager.OnRoundWon += _ => SetCrown(true);
        GameManager.OnRoundStart += () => SetCrown(false);
        _catchArea.BodyEntered += OnCatchAreaEntered;

        if (Network.IsServer)
            SetExplosionCooldown((float)_musicAudioStreamPlayer.Stream.GetLength());
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (TargetPlayer == null)
        {
            return;
        }
        
        _catchArea.GlobalPosition = TargetPlayer.GlobalPosition;
        GlobalPosition = GlobalPosition.Lerp(TargetPlayer.GlobalPosition, (float)delta * 7);
        if (GameManager.AlivePlayers.Count > 1)
        {
            if (!_isTicking)
            {
                _isTicking = true;
                _musicAudioStreamPlayer.Play((float)_musicAudioStreamPlayer.Stream.GetLength() - _explosionCooldown);
                OnCountdownStart?.Invoke();
            }

            if (!_musicAudioStreamPlayer.Playing)
                _musicAudioStreamPlayer.Play();
            
            _explosionCooldown -= (float) delta;
            _tickingCooldown -= (float)delta;
            _catchCooldown -= (float)delta;
            _tickAmount = Mathf.Max(0, _tickAmount - (float)delta);

            Material.Set("shader_parameter/tick_amount", _tickAmount);

            if (Network.IsServer && _explosionCooldown < 0)
            {
                TargetPlayer.Die();
                SetExplosionCooldown((float)_musicAudioStreamPlayer.Stream.GetLength());
            }else if (_tickingCooldown < 0)
            {
                _tickAmount = 1;
                _tickingCooldown = _explosionCooldown / 10;
                _tickAudioStreamPlayer.Play();
            }
        }
        else
        {
            Material.Set("shader_parameter/tick_amount", 0);
            _isTicking = false;
            _musicAudioStreamPlayer.Stop();
        }
    }

    private void SetCrown(bool settingCrown)
    {
        _potatoSprite.Visible = !settingCrown;
        _crownSprite.Visible = settingCrown;
    }

    public void SetTargetPlayer(Player player)
    {
        TargetPlayer = player;
        if (player.IsMultiplayerAuthority())
        {
            _getNegativeAudioStreamPlayer.Play();
        }
        else
        {
            _getPositiveAudioStreamPlayer.Play();
        }
        if(Network.IsServer)
            Network.Call(this, nameof(GetTargetedPlayerFromServer), player.Id);
    }

    private void SelectRandomPlayer(Player player)
    {
        if (!Network.IsServer || player != TargetPlayer)
            return;

        if (GameManager.AlivePlayers.Count == 0)
        {
            TargetPlayer = null;
            return;
        }
        
        SetTargetPlayer(
            GameManager.AlivePlayers.ElementAt(
                RandomNumberGenerator.RandiRange(0, GameManager.AlivePlayers.Count - 1)));
    }
    
    private void OnCatchAreaEntered(Node enteredNode)
    {
        if (!Network.IsServer)
            return;
        
        if (_catchCooldown > 0)
            return;
        
        if (enteredNode is not Player player)
            return;

        GameManager.Potato.SetTargetPlayer(player);
        _catchCooldown = 0.5f;
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
        SetTargetPlayer(GameManager.Players[(int)playerId]);
    }

    private void SetExplosionCooldown(float explosionCooldown)
    {
        _explosionCooldown = explosionCooldown;
        
        if(Network.IsServer)
            Network.Call(this, nameof(GetExplosionCooldownFromServer), explosionCooldown);
    }

    private void SendExplosionCooldownToClient(int playerId)
    {
        if (playerId == 1)
            return;
        
        Network.CallId(playerId, this, nameof(GetExplosionCooldownFromServer), _explosionCooldown);
    }

    [NetworkCallable(NetworkAuthenticationType.Server)]
    private void GetExplosionCooldownFromServer(double explosionCooldown)
    {
        _explosionCooldown = (float)explosionCooldown;
    }
}