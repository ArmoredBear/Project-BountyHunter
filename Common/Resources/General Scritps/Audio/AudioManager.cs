using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   AUDIOMANAGER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Autoloaded singleton that manages ambient and combat music across all scenes.
	**  2 - Detects scene changes and loads the appropriate music tracks.
	**  3 - Handles crossfading between ambient and combat tracks.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class AudioManager : AudioStreamPlayer
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    public static AudioManager Instance;

    private AudioStream _ambientMusic;
    private AudioStream _combatMusic;

    private bool _inCombat = false;
    private bool _isTransitioning = false;
    private bool _transitioningToCombat = false;
    private Timer _combatTimer;
    private Tween _currentTween;
    private string _currentSceneName = "";

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

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
            return;
        }

        // Route all music through the "Music" bus so its volume can be
        // controlled independently of sound effects.
        Bus = "Music";

        // Create combat timer
        _combatTimer = new Timer
        {
            WaitTime = 3.0f,
            OneShot = true
        };
        _combatTimer.Timeout += OnCombatTimerTimeout;
        AddChild(_combatTimer);
    }

    public override void _Process(double delta)
    {
        // Check if the current scene changed and update music accordingly
        var scene = GetTree().CurrentScene;
        if (scene != null && scene.Name != _currentSceneName)
        {
            _currentSceneName = scene.Name;
            GD.Print("AudioManager: Scene changed to ", scene.Name);
            OnSceneChanged(scene.Name);
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Scene Music Setup
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Sets the ambient and combat music tracks and starts playing ambient.
    /// Enables looping on both streams.
    /// </summary>
    public void Set_Music(AudioStream ambient, AudioStream combat)
    {
        // Kill any running tween before resetting state
        KillCurrentTween();

        _ambientMusic = ambient;
        _combatMusic = combat;
        _inCombat = false;
        _isTransitioning = false;
        _transitioningToCombat = false;
        _combatTimer.Stop();

        // Enable looping on streams
        SetStreamLooping(_ambientMusic);
        SetStreamLooping(_combatMusic);

        // Start playing ambient music
        if (_ambientMusic != null)
        {
            Stop();
            Stream = _ambientMusic;
            VolumeDb = 0f;
            Play();
            GD.Print("AudioManager: Playing ambient music");
        }
    }

    /// <summary>
    /// Called when the scene changes. Loads the appropriate music for the new scene.
    /// </summary>
    private void OnSceneChanged(string sceneName)
    {
        switch (sceneName)
        {
            case "Forest":
                // Removed music temporarily
                StopAudioManager();
                break;

            case "Tunnel":
                Set_Music(
                    GD.Load<AudioStream>("res://Common/Resources/SoundTracks/Vindsvept - The Youtube Collection - 65 Hollow.ogg"),
                    null
                );
                break;

            case "Clearing":
                Set_Music(
                    GD.Load<AudioStream>("res://Common/Resources/SoundTracks/HolyPoly_JolynnJChin_Vorbis.ogg"),
                    null
                );
                break;

            case "Main_Menu":
                Set_Music(
                    GD.Load<AudioStream>("res://Common/Resources/SoundTracks/Vindsvept - The Youtube Collection - 51 The Siren's Cadence.ogg"),
                    null
                );
                break;

            case "GameOver_Screen":
                StopAudioManager();
                break;

            default:
                // No music mapped for this scene - stop whatever is playing.
                StopAudioManager();
                break;
        }
    }

    /// <summary>
    /// Fully stops the AudioManager and resets all state.
    /// Used for MainMenu and GameOver scenes.
    /// </summary>
    private void StopAudioManager()
    {
        GD.Print("AudioManager: Stopping for scene: ", _currentSceneName);
        KillCurrentTween();
        Stop();
        Stream = null;
        _ambientMusic = null;
        _combatMusic = null;
        _inCombat = false;
        _isTransitioning = false;
        _transitioningToCombat = false;
        _combatTimer.Stop();
    }

    /// <summary>
    /// Enables looping on a stream. Ogg Vorbis streams use the "loop" bool
    /// property; other stream types use the "loop_mode" property.
    /// </summary>
    private void SetStreamLooping(AudioStream stream)
    {
        if (stream == null) return;

        if (stream is AudioStreamOggVorbis ogg)
            ogg.Loop = true;
        else
            stream.Set("loop_mode", 1);
    }

    /// <summary>
    /// Safely kills the current tween if it exists.
    /// </summary>
    private void KillCurrentTween()
    {
        if (_currentTween != null && _currentTween.IsValid())
        {
            _currentTween.Kill();
        }
        _currentTween = null;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Music Transition Methods
    //!---------------------------------------------------------------------------------------------------------

    public void SwitchToCombat()
    {
        if (_combatMusic == null) return;
        if (_isTransitioning)
        {
            if (!_transitioningToCombat)
            {
                // Interrupting transition to ambient, kill and switch to combat
                KillCurrentTween();
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

        GD.Print("AudioManager: Switching to combat music");
        _isTransitioning = true;
        _transitioningToCombat = true;
        _inCombat = true;

        // Fade out current music
        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "volume_db", -80f, 2f);
        _currentTween.TweenCallback(Callable.From(() =>
        {
            Stop();
            Stream = _combatMusic;
            VolumeDb = -80f;
            Play();

            // Fade in combat music
            _currentTween = CreateTween();
            _currentTween.TweenProperty(this, "volume_db", 0f, 0.5f);
            _currentTween.TweenCallback(Callable.From(() =>
            {
                _isTransitioning = false;
                _transitioningToCombat = false;
                _currentTween = null;
                _combatTimer.Start();
            }));
        }));
    }

    public void SwitchToAmbient()
    {
        if (_ambientMusic == null || !_inCombat) return;
        if (_isTransitioning) return;

        GD.Print("AudioManager: Switching back to ambient music");
        _isTransitioning = true;
        _transitioningToCombat = false;
        _inCombat = false;

        // Fade out current music
        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "volume_db", -80f, 2f);
        _currentTween.TweenCallback(Callable.From(() =>
        {
            Stop();
            Stream = _ambientMusic;
            VolumeDb = -80f;
            Play();

            // Fade in ambient music
            _currentTween = CreateTween();
            _currentTween.TweenProperty(this, "volume_db", 0f, 0.5f);
            _currentTween.TweenCallback(Callable.From(() =>
            {
                _isTransitioning = false;
                _transitioningToCombat = false;
                _currentTween = null;
                _combatTimer.Stop();
            }));
        }));
    }

    public void PlayAmbientMusic()
    {
        if (_ambientMusic != null)
        {
            Stream = _ambientMusic;
            Play();
        }
    }

    public void PlayCombatMusic()
    {
        if (_combatMusic != null)
        {
            Stream = _combatMusic;
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
        catch (ObjectDisposedException)
        {
            // Timer is disposed, ignore
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Signal Handlers
    //!---------------------------------------------------------------------------------------------------------

    private void OnCombatTimerTimeout()
    {
        SwitchToAmbient();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
