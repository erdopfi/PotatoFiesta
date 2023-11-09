using System;

namespace PotatoFiesta.Misc;

public class EventTicker
{
    public Action TickEvent;

    private readonly float _tickTimeInSeconds;
    private float _currentTickTimeInSeconds;

    public EventTicker(float tickTimeInSeconds)
    {
        _tickTimeInSeconds = tickTimeInSeconds;
    }

    public void Tick(float deltaTime)
    {
        _currentTickTimeInSeconds -= deltaTime;
        
        if (!(_currentTickTimeInSeconds < 0)) 
            return;
        TickEvent?.Invoke();
        _currentTickTimeInSeconds = _tickTimeInSeconds;
    }
}