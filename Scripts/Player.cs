using System;
using Godot;
using PotatoFiesta.Misc;
using PotatoFiesta.Networking;

namespace PotatoFiesta;

[GlobalClass]
public partial class Player : CharacterBody2D
{
    public int Id => GetMultiplayerAuthority();
    
    [Export] private AnimationPlayer _animationPlayer;
    [Export] private Sprite2D _sprite;
    [Export] private PackedScene _splatScene;
    [Export] private AudioStreamPlayer2D _explosionAudioStreamPlayer;
    
    private EventTicker _eventTicker;

    public Action OnDeath;
    public Action OnSpawn;

    public bool IsDead { get; set; } = true;

    public override void _Ready()
    {
        base._Ready();
        _sprite.Visible = false;
        _eventTicker = new EventTicker(0.05f);

        if (Network.IsMultiplayerAuthority(this))
        {
            _eventTicker.TickEvent += SendDataToOthers;
            CameraController.TargetNode = this;
        }
    }

    private Vector2 _targetPosition;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (Network.IsMultiplayerAuthority(this))
            Velocity = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * 70;
        else
        {
            Velocity = (_targetPosition - GlobalPosition).Normalized() * 70;
            if (_targetPosition.DistanceSquaredTo(GlobalPosition) < 0.5f)
                Velocity = Vector2.Zero;
        }
        
        ApplyGraphics();
        
        _eventTicker.Tick((float)delta);
        MoveAndSlide();
    }

    private void ApplyGraphics()
    {
        if (Velocity.Length() == 0)
        {
            _animationPlayer.Play("idle");
        }
        else
        {
            _animationPlayer.Play("run");
        }

        if (Velocity.X < 0)
        {
            _sprite.FlipH = true;
        }
        else if (Velocity.X > 0)
        {
            _sprite.FlipH = false;
        }
    }
    
    private void SendDataToOthers()
    {
        Network.Call(this, nameof(GetDataFromAuthority), GlobalPosition.X, GlobalPosition.Y);
    }

    [NetworkCallable]
    private void GetDataFromAuthority(double posX, double posY)
    {
        _targetPosition = new Vector2((float)posX, (float)posY);
    }

    public void Die()
    {
        if (IsDead)
            return;
        
        var splat = _splatScene.Instantiate<Splat>();
        splat.GlobalPosition = GlobalPosition;
        GameManager.RootNode.AddChild(splat);
        _sprite.Visible = false;
        IsDead = true;
        _explosionAudioStreamPlayer.Play();
        OnDeath?.Invoke();
        GD.Print($"Killed player {Id}");
        
        if(Network.IsServer)
            Network.Call(this, nameof(GetDeathFromServer));
    }

    public void Spawn()
    {
        if (!IsDead)
            return;
        
        _sprite.Visible = true;
        IsDead = false;
        OnSpawn?.Invoke();
        GD.Print($"Spawned player {Id}");
        
        if(Network.IsServer)
            Network.Call(this, nameof(GetSpawnFromServer));
    }

    [NetworkCallable(NetworkAuthenticationType.Server)]
    public void GetDeathFromServer()
    {
        Die();
    }

    [NetworkCallable(NetworkAuthenticationType.Server)]
    public void GetSpawnFromServer()
    {
        Spawn();
    }
}