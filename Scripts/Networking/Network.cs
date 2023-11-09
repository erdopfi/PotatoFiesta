using System;
using System.Reflection;
using Godot;
using Newtonsoft.Json;

namespace PotatoFiesta.Networking;

public partial class Network : Node
{
    private static SceneTree _tree;
    private static readonly ENetMultiplayerPeer MultiplayerPeer = new();
    private static MultiplayerApi _multiplayerApi;

    private static Network _instance;

    public static bool IsServer => _multiplayerApi?.IsServer() ?? false;
    public static int UniqueId => _multiplayerApi.GetUniqueId();
    public static int SenderId => _multiplayerApi.GetRemoteSenderId();
    public static bool IsSentByServer => SenderId == 1;

    public static int Port { get; private set; } = 25565;
    public static int MaxClients { get; private set; } = 10;
    public static bool CreateClientOnServer = true;

    public static Action OnServerCreated;
    public static Action OnServerClosed;

    public static Action<int> OnPeerConnected;
    public static Action<int> OnPeerDisconnected;

    public static Action OnClientCreated;
    public static Action OnServerConnected;
    public static Action OnServerDisconnected;

    public override void _Ready()
    {
        base._Ready();
        _instance = this;
        _tree = GetTree();

        _multiplayerApi = MultiplayerApi.CreateDefaultInterface();
        _tree.SetMultiplayer(_multiplayerApi);

        _multiplayerApi.PeerConnected += PeerConnected;
        _multiplayerApi.PeerDisconnected += PeerDisconnected;

        _multiplayerApi.ConnectedToServer += ConnectedToServer;
        _multiplayerApi.ServerDisconnected += DisconnectedFromServer;
    }

    public static void Call(Node node, string methodName, params object[] parameters)
    {
        var serializedParameters = JsonConvert.SerializeObject(parameters);
        _instance.Rpc(nameof(GetNetworkCall), node.GetPath(), methodName, serializedParameters);
    }

    public static void CallId(int peerId, Node node, string methodName, params object[] parameters)
    {
        var serializedParameters = JsonConvert.SerializeObject(parameters);
        _instance.RpcId(peerId, nameof(GetNetworkCall), node.GetPath(), methodName, serializedParameters);
    }

    public static void UnreliableCall(Node node, string methodName, params object[] parameters)
    {
        var serializedParameters = JsonConvert.SerializeObject(parameters);
        _instance.Rpc(nameof(GetUnreliableNetworkCall), node.GetPath(), methodName, serializedParameters);
    }

    public static void UnreliableCallId(int peerId, Node node, string methodName, params object[] parameters)
    {
        var serializedParameters = JsonConvert.SerializeObject(parameters);
        _instance.RpcId(peerId, nameof(GetUnreliableNetworkCall), node.GetPath(), methodName, serializedParameters);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void GetNetworkCall(string nodePath, string methodName, string serializedParameters)
    {
        ProcessCall(nodePath, methodName, serializedParameters);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void GetUnreliableNetworkCall(string nodePath, string methodName, string serializedParameters)
    {
        ProcessCall(nodePath, methodName, serializedParameters);
    }

    private void ProcessCall(string nodePath, string methodName, string serializedParameters)
    {
        var node = GetNodeOrNull(nodePath);
        if (node == null)
        {
            GD.PushWarning($"Node path, which sender sent, does not exist: {nodePath}, method: {methodName}");
            return;
        }

        var type = node.GetType();
        var methodInfo = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var parameters = JsonConvert.DeserializeObject<object[]>(serializedParameters);

        if (methodInfo != null)
        {
            if (!Attribute.IsDefined(methodInfo, typeof(NetworkCallable)))
            {
                GD.PushWarning("Method is not network callable");
                return;
            }

            var networkCallable = (NetworkCallable)Attribute.GetCustomAttribute(methodInfo, typeof(NetworkCallable));
            if (networkCallable.NetworkAuthenticationType == NetworkAuthenticationType.Server && SenderId != 1)
            {
                GD.PushWarning($"Sender {SenderId} is not permitted to use method {methodName}");
                return;
            }
            if (networkCallable.NetworkAuthenticationType == NetworkAuthenticationType.Authentication && node.GetMultiplayerAuthority() != SenderId)
            {
                GD.PushWarning($"Sender {SenderId} is not permitted to use method {methodName}");
                return;
            }

            try
            {
                methodInfo.Invoke(node, parameters);
            }
            catch (Exception e)
            {
                GD.PushWarning($"Sender sent wrong parameters for method {methodName}: {e.Message}");
            }
        }
        else
        {
            GD.PushWarning($"Calling method {methodName} not found in type {type}");
        }
    }

    public static bool IsMultiplayerAuthority(Node node)
    {
        return UniqueId == node.GetMultiplayerAuthority();
    }

    #region Server

    public static void CreateServer()
    {
        MultiplayerPeer.CreateServer(Port, MaxClients);
        _multiplayerApi.MultiplayerPeer = MultiplayerPeer;

        GD.Print("Created server");

        OnServerCreated?.Invoke();

        if (CreateClientOnServer)
            OnPeerConnected?.Invoke(1);
    }

    private static void PeerConnected(long longPeerId)
    {
        if (!IsServer)
            return;
        
        var peerId = (int)longPeerId;
        GD.Print($"Connected peer {peerId}");
        OnPeerConnected?.Invoke(peerId);
    }

    private static void PeerDisconnected(long longPeerId)
    {
        if (!IsServer)
            return;
        
        var peerId = (int)longPeerId;
        GD.Print($"Disconnected peer {peerId}");
        OnPeerDisconnected?.Invoke(peerId);
    }

    public static void CloseServer()
    {
        MultiplayerPeer.Close();
        OnServerClosed?.Invoke();

        GD.Print("Closed server");
    }

    public static void DisconnectClient(int id)
    {
        MultiplayerPeer.DisconnectPeer(id);

        GD.Print($"Disconnected peer {id}");
    }

    #endregion

    #region Client

    public static void CreateClient(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return;
        }

        MultiplayerPeer.CreateClient(ipAddress, Port);
        _multiplayerApi.MultiplayerPeer = MultiplayerPeer;

        GD.Print("Created client");

        OnClientCreated?.Invoke();
    }

    private static void ConnectedToServer()
    {
        GD.Print("Connected to server");
        OnServerConnected?.Invoke();
    }

    private static void DisconnectedFromServer()
    {
        GD.Print("Disconnected from server");
        OnServerDisconnected?.Invoke();
    }

    public static void CloseClient()
    {
        MultiplayerPeer.Close();
    }

    #endregion
}