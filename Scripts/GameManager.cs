using System.Collections.Generic;
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
        Network.OnPeerConnected += playerId => SpawnPlayer(playerId);
        Network.OnPeerDisconnected += RemovePlayer;
    }

    private Player SpawnPlayer(int playerId)
    {
        var player = _playerScene.Instantiate<Player>();
        player.SetMultiplayerAuthority(playerId);
        player.Name += playerId;
        RootNode.AddChild(player);
        
        _players.Add(playerId, player);
        player.OnSpawn += () =>
        {
            AlivePlayers.Add(player);
        };
        player.OnDeath += () => AlivePlayers.Remove(player);

        if (Network.IsServer)
        {
            Network.Call(this, nameof(SpawnPlayerFromServer), playerId);
            player.Spawn();
        }

        return player;
    }

    [NetworkCallable(NetworkAuthenticationType.Server)]
    private void SpawnPlayerFromServer(long playerId)
    {
        SpawnPlayer((int)playerId);
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

        var playerDataList = new List<PlayerData>();
        foreach (var (playerId, player) in _players)
        {
            var playerData = new PlayerData
            {
                PlayerId = playerId,
                IsDead = player.IsDead
            };
            playerDataList.Add(playerData);
        }
        
        Network.CallId(peerId, this, nameof(GetPlayersFromServer), JsonConvert.SerializeObject(playerDataList));
    }

    [NetworkCallable(NetworkAuthenticationType.Server)]
    private void GetPlayersFromServer(string serializedPlayerDataList)
    {
        var playerDataList = JsonConvert.DeserializeObject<List<PlayerData>>(serializedPlayerDataList);

        foreach (var playerData in playerDataList)
        {
            var player = SpawnPlayer(playerData.PlayerId);
            if(!playerData.IsDead)
                player.Spawn();
        }
    }

    private struct PlayerData
    {
        public int PlayerId { get; set; }
        public bool IsDead { get; set; }
    }
}