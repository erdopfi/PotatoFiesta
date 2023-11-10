using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using PotatoFiesta.Networking;

namespace PotatoFiesta;

[GlobalClass]
public partial class GameManager : Node
{
    [Export] private PackedScene _playerScene;
    [Export] private Node2D _rootNode;
    public static Node2D RootNode => _instance._rootNode; 
    [Export] private Potato _potato;
    public static Potato Potato => _instance._potato;

    private static GameManager _instance;

    private static Dictionary<int, Player> _players = new();
    public static List<Player> AlivePlayers { get; private set; } = new();
    public static Dictionary<int, Player> Players => _players;
    
    public override void _Ready()
    {
        base._Ready();
        _instance = this;
        
        Network.OnPeerConnected += SendPlayersToClient;
        Network.OnPeerConnected += playerId =>
        {
            var randomNumberGenerator = new RandomNumberGenerator();
            var hue = randomNumberGenerator.RandfRange(0, 1);
            var saturation = randomNumberGenerator.RandfRange(0.5f, 1);
            var value = randomNumberGenerator.RandfRange(0.6f, 1);
            var color = Color.FromHsv(hue, saturation, value);
            
            SpawnPlayer(new PlayerData
            {
                Id = playerId,
                IsDead = false,
                ColorString = color.ToHtml()
            });
        };
        Network.OnPeerDisconnected += RemovePlayer;
    }

    private Player SpawnPlayer(PlayerData playerData)
    {
        var player = _playerScene.Instantiate<Player>();
        RootNode.AddChild(player);
        
        _players.Add(playerData.Id, player);
        player.OnSpawn += () =>
        {
            AlivePlayers.Add(player);
        };
        player.OnDeath += () => AlivePlayers.Remove(player);
        
        if (Network.IsServer)
        {
            player.OnDeath += CheckForWinner;
            Network.Call(this, nameof(SpawnPlayerFromServer), JsonConvert.SerializeObject(playerData));
        }
        
        player.Load(playerData);
        
        return player;
    }

    private void CheckForWinner()
    {
        if (AlivePlayers.Count <= 1)
        {
            foreach (var player in Players.Values)
            {
                player.Spawn();
            }
        }
    }

    [NetworkCallable(NetworkAuthenticationType.Server)]
    private void SpawnPlayerFromServer(string serializedPlayerData)
    {
        var playerData = JsonConvert.DeserializeObject<PlayerData>(serializedPlayerData);
        SpawnPlayer(playerData);
    }

    private void RemovePlayer(int peerId)
    {
        if (!_players.TryGetValue(peerId, out var player))
            return;
        
        player.Die();
        _players.Remove(peerId);
        player.QueueFree();
    }

    private void SendPlayersToClient(int peerId)
    {
        if (peerId == 1)
            return;

        var playerDataList = _players.Values.Select(player => player.Save()).ToList();
        Network.CallId(peerId, this, nameof(GetPlayersFromServer), JsonConvert.SerializeObject(playerDataList));
    }

    [NetworkCallable(NetworkAuthenticationType.Server)]
    private void GetPlayersFromServer(string serializedPlayerDataList)
    {
        var playerDataList = JsonConvert.DeserializeObject<List<PlayerData>>(serializedPlayerDataList);

        foreach (var playerData in playerDataList)
        {
            var player = SpawnPlayer(playerData);
            player.ChangeColor(Color.FromString(playerData.ColorString, Colors.White));
            if(!playerData.IsDead)
                player.Spawn();
        }
    }
}