using System;
using Godot;

namespace PotatoFiesta.Networking;

[AttributeUsage(AttributeTargets.Method)]
public class NetworkCallable : Attribute
{
    public NetworkAuthenticationType NetworkAuthenticationType;

    public NetworkCallable()
    {
        NetworkAuthenticationType = NetworkAuthenticationType.Authentication;
    }

    public NetworkCallable(NetworkAuthenticationType authenticationType)
    {
        NetworkAuthenticationType = authenticationType;
    }

    public void Display()
    {
        GD.Print(NetworkAuthenticationType);
    }
}