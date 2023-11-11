using System;
using System.Numerics;
using Godot;
using Godot.Collections;
using PotatoFiesta.Misc;
using PotatoFiesta.Networking;
using Vector2 = Godot.Vector2;

namespace PotatoFiesta;

[GlobalClass]
public partial class Player : CharacterBody2D
{
    public int Id => GetMultiplayerAuthority();
    
    [Export] private AnimationPlayer _animationPlayer;
    [Export] private Sprite2D _sprite;
    [Export] private PackedScene _splatScene;
    [Export] private CollisionShape2D _collisionShape;
    [Export] private AudioStreamPlayer2D _explosionAudioStreamPlayer;
    [Export] private CpuParticles2D _explosionParticles;
    
    public Color Color { get; private set; }
    
    private EventTicker _eventTicker;

    public Action OnDeath;
    public Action OnSpawn;

    public bool IsDead { get; set; } = true;

    public override void _Ready()
    {
        base._Ready();
        
        _eventTicker = new EventTicker(0.05f);
    }

    public void ChangeColor(Color color)
    {
        Color = color;
        _explosionParticles.Color = color;
        var playerMaterial = (ShaderMaterial)Material.Duplicate();
        playerMaterial.SetShaderParameter("player_color", color);
        Material = playerMaterial;
    }

    private Vector2 _targetPosition;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsDead)
        {
            Velocity = Vector2.Zero;
            ApplyGraphics();
            return;
        }

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
        
        _collisionShape.Disabled = true;
        var splat = _splatScene.Instantiate<Splat>();
        splat.GlobalPosition = GlobalPosition.Round();
        splat.Material = Material;
        GameManager.RootNode.AddChild(splat);
        _sprite.Visible = false;
        IsDead = true;
        _explosionAudioStreamPlayer.Play();
        _explosionParticles.Restart();
        _explosionParticles.Emitting = true;
        GD.Print($"Killed player {Id}");
        if(Network.IsServer)
            Network.Call(this, nameof(GetDeathFromServer));
        OnDeath?.Invoke();
    }

    public void Spawn()
    {
        if (!IsDead)
            return;

        _collisionShape.Disabled = false;
        _sprite.Visible = true;
        IsDead = false;
        GD.Print($"Spawned player {Id}");
        if(Network.IsServer)
            Network.Call(this, nameof(GetSpawnFromServer));
        GlobalPosition = Vector2.Zero;
        OnSpawn?.Invoke();
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

    public PlayerData Save()
    {
        return new PlayerData
        {
            Id = Id,
            IsDead = IsDead,
            ColorString = Color.ToHtml()
        };
    }

    public void Load(PlayerData playerData)
    {
        Name += playerData.Id;
        SetMultiplayerAuthority(playerData.Id);
        ChangeColor(Color.FromString(playerData.ColorString, Colors.White));

        if (!playerData.IsDead)
            Spawn();
        else
        {
            _collisionShape.Disabled = true;
            _sprite.Visible = false;
        }
        
        if (Network.IsMultiplayerAuthority(this))
        {
            _eventTicker.TickEvent += SendDataToOthers;
            CameraController.TargetPlayer = this;
        }
    }
}