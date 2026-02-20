using Godot;
using System;

namespace ExtraShoot.scripts.Utilities;

public class GameTimer
{
    private Timer _timer;
    private Action _callback;
    private float _interval;
    private bool _loop;

    public bool IsRunning => _timer != null && !_timer.IsStopped();

    public GameTimer(Node owner)
    {
        _timer = new Timer();
        owner.AddChild(_timer);

        _timer.Timeout += OnTimeout;
    }

    public void Start(
        Action callback,
        float interval,
        bool loop = false,
        float initialDelay = 0f)
    {
        _callback = callback;
        _interval = interval;
        _loop = loop;

        _timer.Stop();
        _timer.WaitTime = initialDelay > 0f ? initialDelay : interval;
        _timer.OneShot = !loop;

        _timer.Start();
    }

    private void OnTimeout()
    {
        _callback?.Invoke();

        // If looping and we used an initial delay,
        // switch to the real interval after first fire.
        if (_loop && _timer.WaitTime != _interval)
        {
            _timer.WaitTime = _interval;
        }
    }

    public void Stop()
    {
        _timer?.Stop();
    }
}
