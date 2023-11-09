using Godot;

namespace PotatoFiesta.Networking;

public partial class NetworkUi : Control
{
    [Export] private Button _serverButton;
    [Export] private Button _clientButton;
    [Export] private LineEdit _ipAddressLineEdit;

    public override void _Ready()
    {
        base._Ready();
        _serverButton.Pressed += Network.CreateServer;
        _clientButton.Pressed += () => Network.CreateClient(_ipAddressLineEdit.Text);

        Network.OnServerCreated += Hide;
        Network.OnClientCreated += Hide;
    }
}