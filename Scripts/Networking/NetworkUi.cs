using Godot;

namespace PotatoFiesta.Networking;

public partial class NetworkUi : Control
{
    [Export] private TextureButton _serverButton;
    [Export] private TextureButton _clientButton;
    [Export] private LineEdit _ipAddressLineEdit;
    [Export] private AudioStreamPlayer _musicAudioStreamPlayer;

    public override void _Ready()
    {
        base._Ready();
        _serverButton.Pressed += Network.CreateServer;
        _clientButton.Pressed += () => Network.CreateClient(_ipAddressLineEdit.Text);

        Network.OnServerCreated += MakeInvisible;
        Network.OnClientCreated += MakeInvisible;

        Network.OnServerClosed += MakeVisible;
        Network.OnServerDisconnected += MakeVisible;
        _musicAudioStreamPlayer.Play();
    }

    private void MakeVisible()
    {
        Show();
        _musicAudioStreamPlayer.Play();
    }

    private void MakeInvisible()
    {
        Hide();
        _musicAudioStreamPlayer.Stop();
    }
}