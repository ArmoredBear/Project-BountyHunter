using Godot;
using System;

public partial class AudioManager : AudioStreamPlayer2D
{
    public static AudioManager Instance;

    [Export] public AudioStream AmbientMusic;
    [Export] public AudioStream CombatMusic;

    private bool _inCombat = false;
    private bool _isTransitioning = false;
    private bool _transitioningToCombat = false;
    private Timer _combatTimer;
    private Tween _currentTween;

    public override void _Ready()
    {
        if (Instance == null || !GodotObject.IsInstanceValid(Instance))
        {
            Instance = this;
        }
        else
        {
            GD.PrintErr("Multiple AudioManager instances detected!");
            QueueFree();
        }

        // Create combat timer
        _combatTimer = new Timer
        {
            WaitTime = 3.0f,
            OneShot = true
        };
        _combatTimer.Timeout += OnCombatTimerTimeout;
        AddChild(_combatTimer);

        // Start with ambient music
        if (AmbientMusic != null)
        {
            Stream = AmbientMusic;
            Play();
        }
    }

    public void SwitchToCombat()
    {
        if (CombatMusic == null) return;
        if (_isTransitioning)
        {
            if (!_transitioningToCombat)
            {
                // Interrupting transition to ambient, kill and switch to combat
                _currentTween?.Kill();
                _currentTween = null;
                _isTransitioning = false;
            }
            else
            {
                // Already transitioning to combat, do nothing
                return;
            }
        }
        if (_inCombat)
        {
            // Already in combat, just restart the timer
            _combatTimer.Start();
            return;
        }

        _isTransitioning = true;
        _transitioningToCombat = true;
        _inCombat = true;
        // Fade out current music
        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "volume_db", -80f, 2f);
        _currentTween.Finished += () =>
        {
            if (Stream != CombatMusic)
            {
                Stop();
                Stream = CombatMusic;
                VolumeDb = -80f;
                Play();
            }
            else
            {
                VolumeDb = -80f;
            }
            _currentTween = CreateTween();
            _currentTween.TweenProperty(this, "volume_db", 0f, 0.5f);
            _currentTween.Finished += () =>
            {
                _isTransitioning = false;
                _transitioningToCombat = false;
                _currentTween = null;
                _combatTimer.Start();
            };
        };
    }

    public void SwitchToAmbient()
    {
        if (AmbientMusic == null || _isTransitioning || !_inCombat) return;

        _isTransitioning = true;
        _transitioningToCombat = false;
        _inCombat = false;
        // Fade out current music
        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "volume_db", -80f, 2f);
        _currentTween.Finished += () =>
        {
            if (Stream != AmbientMusic)
            {
                Stop();
                Stream = AmbientMusic;
                VolumeDb = -80f;
                Play();
            }
            else
            {
                VolumeDb = -80f;
            }
            _currentTween = CreateTween();
            _currentTween.TweenProperty(this, "volume_db", 0f, 0.5f);
            _currentTween.Finished += () =>
            {
                _isTransitioning = false;
                _transitioningToCombat = false;
                _currentTween = null;
                _combatTimer.Stop();
            };
        };
    }

    private void OnCombatTimerTimeout()
    {
        SwitchToAmbient();
    }

    public void PlayAmbientMusic()
    {
        if (AmbientMusic != null)
        {
            Stream = AmbientMusic;
            Play();
        }
    }

    public void PlayCombatMusic()
    {
        if (CombatMusic != null)
        {
            Stream = CombatMusic;
            Play();
        }
    }

    public void StartAmbientTimer()
    {
        try
        {
            if (_combatTimer != null && _combatTimer.IsStopped())
            {
                _combatTimer.Start();
            }
        }
        catch (System.ObjectDisposedException)
        {
            // Timer is disposed, ignore
        }
    }
}