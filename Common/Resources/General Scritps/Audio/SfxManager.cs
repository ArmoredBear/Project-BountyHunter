using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SFXMANAGER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	**
	**  1 - Autoloaded singleton that plays every sound effect in the game.
	**  2 - Keeps a fixed set of empty speakers ("voices") and hands one out
	**      per play, so no scene needs its own speaker node.
	**  3 - Enforces the per sound rules stored in the SfxLibrary: cooldown,
	**      maximum simultaneous copies, and pitch/volume variation.
	**  4 - Positioned sounds are placed at the spot they came from, so the
	**      player can tell left from right using the AudioListener2D "ears".
	**
	**  USAGE
	**      SfxManager.Instance.Play("ui_hover");
	**      SfxManager.Instance.Play("sword_swoosh", GlobalPosition);
	**
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class SfxManager : Node
{
    //!---------------------------------------------------------------------------------------------------------
    #region Constants
    //!---------------------------------------------------------------------------------------------------------

    private const string LIBRARY_PATH = "res://Common/Resources/Audio/SfxLibrary.tres";

    private const int WORLD_VOICE_COUNT = 16;
    private const int FLAT_VOICE_COUNT = 6;

    // Looping sounds are long lived and there are only a few of them, so they
    // get their own speakers instead of competing with one shots. The pool
    // starts at LOOP_VOICE_START_COUNT and grows one speaker at a time as more
    // owners ask for a loop, up to the limit.
    private const int LOOP_VOICE_START_COUNT = 4;
    private const int LOOP_VOICE_MAX_COUNT = 16;

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    public static SfxManager Instance;

    /// <summary>The catalog of every sound available to the game.</summary>
    public SfxLibrary Library { get; private set; }

    private AudioStreamPlayer2D[] _worldVoices;
    private AudioStreamPlayer[] _flatVoices;
    private AudioStreamPlayer2D[] _loopVoices;

    // Which entry each voice is currently playing, and when it started.
    private SfxEntry[] _worldVoiceEntry;
    private SfxEntry[] _flatVoiceEntry;
    private ulong[] _worldVoiceStart;

    // Looping voices also remember who asked for them and where that owner is, so
    // the height cue can be refreshed every frame while the sound keeps running.
    private SfxEntry[] _loopVoiceEntry;
    private Node[] _loopVoiceOwner;
    private Vector2[] _loopVoicePosition;
    private ulong[] _loopVoiceStart;

    // How many looping speakers exist so far. The arrays above are allocated at
    // the maximum size, and only this many slots are actually in use.
    private int _loopVoiceCount = 0;

    // Which looping speaker belongs to which owner. Owners are identified by
    // their instance id, so every enemy making the same sound gets its own
    // speaker instead of fighting over one shared voice.
    private Dictionary<ulong, int> _loopVoiceByOwner = new Dictionary<ulong, int>();

    // Per sound bookkeeping.
    private Dictionary<StringName, int> _activeCount = new Dictionary<StringName, int>();
    private Dictionary<StringName, ulong> _lastPlayTime = new Dictionary<StringName, ulong>();

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        if (Instance == null || !GodotObject.IsInstanceValid(Instance))
        {
            Instance = this;
        }
        else
        {
            GD.PrintErr("Multiple SfxManager instances detected!");
            QueueFree();
            return;
        }

        Library = GD.Load<SfxLibrary>(LIBRARY_PATH);

        if (Library == null)
        {
            GD.PrintErr("SfxManager: could not load SFX library at " + LIBRARY_PATH);
        }

        CreateVoices();
    }

    /// <summary>
    /// Creates the empty speakers once at startup. They are reused for the
    /// whole session, so nothing is allocated while the game is running.
    /// </summary>
    private void CreateVoices()
    {
        _worldVoices = new AudioStreamPlayer2D[WORLD_VOICE_COUNT];
        _worldVoiceEntry = new SfxEntry[WORLD_VOICE_COUNT];
        _worldVoiceStart = new ulong[WORLD_VOICE_COUNT];

        for (int i = 0; i < WORLD_VOICE_COUNT; i++)
        {
            AudioStreamPlayer2D voice = new AudioStreamPlayer2D();
            voice.Name = "WorldVoice" + i;
            AddChild(voice);

            int index = i;
            voice.Finished += () => OnWorldVoiceFinished(index);

            _worldVoices[i] = voice;
            _worldVoiceEntry[i] = null;
            _worldVoiceStart[i] = 0;
        }

        _flatVoices = new AudioStreamPlayer[FLAT_VOICE_COUNT];
        _flatVoiceEntry = new SfxEntry[FLAT_VOICE_COUNT];

        for (int i = 0; i < FLAT_VOICE_COUNT; i++)
        {
            AudioStreamPlayer voice = new AudioStreamPlayer();
            voice.Name = "FlatVoice" + i;
            AddChild(voice);

            int index = i;
            voice.Finished += () => OnFlatVoiceFinished(index);

            _flatVoices[i] = voice;
            _flatVoiceEntry[i] = null;
        }

        _loopVoices = new AudioStreamPlayer2D[LOOP_VOICE_MAX_COUNT];
        _loopVoiceEntry = new SfxEntry[LOOP_VOICE_MAX_COUNT];
        _loopVoiceOwner = new Node[LOOP_VOICE_MAX_COUNT];
        _loopVoicePosition = new Vector2[LOOP_VOICE_MAX_COUNT];
        _loopVoiceStart = new ulong[LOOP_VOICE_MAX_COUNT];

        for (int i = 0; i < LOOP_VOICE_START_COUNT; i++)
        {
            CreateLoopVoice(i);
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Public API
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Plays a sound without a position. Used for menus and anything that
    /// should not move or pan.
    /// </summary>
    public void Play(StringName id)
    {
        SfxEntry entry = ResolveEntry(id);

        if (entry == null)
            return;

        if (!CanPlay(entry))
            return;

        PlayFlat(entry);
    }

    /// <summary>
    /// Plays a sound at a spot in the world, so it fades with distance and
    /// pans left or right relative to the player.
    /// </summary>
    public void Play(StringName id, Vector2 worldPosition)
    {
        SfxEntry entry = ResolveEntry(id);

        if (entry == null)
            return;

        if (!CanPlay(entry))
            return;

        if (!entry.Spatial)
        {
            PlayFlat(entry);
            return;
        }

        PlayWorld(entry, worldPosition);
    }

    /// <summary>
    /// Stops every voice currently playing and clears the bookkeeping.
    /// </summary>
    public void StopAll()
    {
        StopAllLoops();

        for (int i = 0; i < WORLD_VOICE_COUNT; i++)
        {
            _worldVoices[i].Stop();
            _worldVoiceEntry[i] = null;
        }

        for (int i = 0; i < FLAT_VOICE_COUNT; i++)
        {
            _flatVoices[i].Stop();
            _flatVoiceEntry[i] = null;
        }

        _activeCount.Clear();

        // Cooldowns are cleared too, otherwise a sound triggered right after a
        // scene change could be swallowed by a cooldown from before the change.
        _lastPlayTime.Clear();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Looping Sounds
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Starts a looping sound for one owner, or leaves the running one alone.
    /// Safe to call every frame: a loop that is already playing for this owner
    /// is only moved, never restarted, so there is no click or gap.
    ///
    /// Each owner gets its own speaker. Sharing a single speaker per sound id
    /// does not work when several enemies play the same looping sound, because
    /// they overwrite each other's position every frame and only the last one
    /// to run is ever heard.
    /// </summary>
    public void PlayLoop(Node owner, StringName id, Vector2 worldPosition)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        SfxEntry entry = ResolveEntry(id);

        if (entry == null)
            return;

        if (entry.Loop == false)
        {
            GD.PrintErr("SfxManager: sound '" + id + "' is not marked as a looping sound.");
            return;
        }

        AudioStream stream = GetLoopStream(entry);

        if (stream == null)
            return;

        ulong ownerKey = owner.GetInstanceId();

        // Already running for this owner, so just move it to the new spot.
        if (_loopVoiceByOwner.ContainsKey(ownerKey))
        {
            int running = _loopVoiceByOwner[ownerKey];

            _loopVoicePosition[running] = worldPosition;

            // The owner asked for a different looping sound, so swap it over.
            if (_loopVoiceEntry[running] != entry)
            {
                StartLoopVoice(running, owner, entry, stream, worldPosition);
            }

            return;
        }

        StartLoopVoice(AcquireLoopVoice(), owner, entry, stream, worldPosition);
    }

    /// <summary>
    /// Moves one owner's running loop to a new spot in the world, so the sound
    /// follows whatever is making it and the height cue keeps up.
    /// </summary>
    public void SetLoopPosition(Node owner, Vector2 worldPosition)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        ulong ownerKey = owner.GetInstanceId();

        if (_loopVoiceByOwner.ContainsKey(ownerKey) == false)
            return;

        _loopVoicePosition[_loopVoiceByOwner[ownerKey]] = worldPosition;
    }

    /// <summary>
    /// Stops one owner's loop and frees its speaker. Does nothing when that
    /// owner has no loop running.
    /// </summary>
    public void StopLoop(Node owner)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        ulong ownerKey = owner.GetInstanceId();

        if (_loopVoiceByOwner.ContainsKey(ownerKey) == false)
            return;

        int index = _loopVoiceByOwner[ownerKey];
        _loopVoices[index].Stop();
        ReleaseLoopVoice(index);
    }

    /// <summary>Stops every looping sound that is currently running.</summary>
    public void StopAllLoops()
    {
        for (int i = 0; i < _loopVoiceCount; i++)
        {
            _loopVoices[i].Stop();
            ReleaseLoopVoice(i);
        }

        _loopVoiceByOwner.Clear();
    }

    /// <summary>True when this owner currently has a loop running.</summary>
    public bool IsLooping(Node owner)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return false;

        return _loopVoiceByOwner.ContainsKey(owner.GetInstanceId());
    }

    /// <summary>
    /// Refreshes every running loop. A one shot can work out its direction once,
    /// when it starts, but a loop has to keep working it out while it plays,
    /// because the thing making the sound keeps moving.
    ///
    /// Owners that were freed without stopping their own loop, for example when
    /// a whole level is torn down, are cleaned up here so they cannot linger.
    /// </summary>
    public override void _Process(double delta)
    {
        for (int i = 0; i < _loopVoiceCount; i++)
        {
            SfxEntry entry = _loopVoiceEntry[i];

            if (entry == null)
                continue;

            Node owner = _loopVoiceOwner[i];

            if (owner == null || !GodotObject.IsInstanceValid(owner))
            {
                _loopVoices[i].Stop();
                ReleaseLoopVoice(i);
                continue;
            }

            ApplyLoopCue(i, entry);
        }
    }

    /// <summary>
    /// Points one looping speaker at an owner and starts it playing.
    /// </summary>
    private void StartLoopVoice(int index, Node owner, SfxEntry entry, AudioStream stream, Vector2 worldPosition)
    {
        if (index < 0)
            return;

        AudioStreamPlayer2D voice = _loopVoices[index];

        voice.Stream = stream;
        voice.MaxDistance = entry.MaxDistance;
        voice.Attenuation = entry.Attenuation;
        voice.PanningStrength = entry.PanningStrength;

        _loopVoiceEntry[index] = entry;
        _loopVoiceOwner[index] = owner;
        _loopVoicePosition[index] = worldPosition;
        _loopVoiceStart[index] = Time.GetTicksMsec();
        _loopVoiceByOwner[owner.GetInstanceId()] = index;

        ApplyLoopCue(index, entry);
        voice.Play();
    }

    /// <summary>
    /// Applies the current position, bus, pitch and volume to one looping
    /// speaker, using the same above/below rules as a one shot.
    /// </summary>
    private void ApplyLoopCue(int index, SfxEntry entry)
    {
        AudioStreamPlayer2D voice = _loopVoices[index];
        Vector2 worldPosition = _loopVoicePosition[index];

        string busName = entry.GetBusName();
        float pitchScale = 1f;
        float volumeTrimDb = 0f;

        if (entry.HeightCue == true)
        {
            GetHeightCue(worldPosition, entry, out busName, out pitchScale, out volumeTrimDb);
        }

        voice.GlobalPosition = worldPosition;
        voice.Bus = busName;
        voice.PitchScale = pitchScale;
        voice.VolumeDb = entry.VolumeDb + volumeTrimDb;
    }

    /// <summary>
    /// Returns the recording a looping sound should use. The randomiser has no
    /// loop mode, so the first recording is played directly and the audio file
    /// itself has to be imported with looping switched on.
    /// </summary>
    private static AudioStream GetLoopStream(SfxEntry entry)
    {
        if (entry.Variants.StreamsCount == 0)
        {
            GD.PrintErr("SfxManager: looping sound '" + entry.Id + "' has no recordings.");
            return null;
        }

        AudioStream stream = entry.Variants.GetStream(0);

        if (stream is AudioStreamOggVorbis oggStream && oggStream.Loop == false)
        {
            GD.PrintErr("SfxManager: looping sound '" + entry.Id + "' uses a file imported without looping.");
            return null;
        }

        return stream;
    }

    /// <summary>
    /// Finds a free looping speaker, adding one when the pool needs to grow.
    /// If the pool is already at its limit the oldest loop is stopped and its
    /// speaker reused, so a new loop can always start.
    /// </summary>
    private int AcquireLoopVoice()
    {
        for (int i = 0; i < _loopVoiceCount; i++)
        {
            if (_loopVoiceEntry[i] == null)
                return i;
        }

        if (_loopVoiceCount < LOOP_VOICE_MAX_COUNT)
            return CreateLoopVoice(_loopVoiceCount);

        int oldest = 0;
        ulong oldestStart = ulong.MaxValue;

        for (int i = 0; i < _loopVoiceCount; i++)
        {
            if (_loopVoiceStart[i] < oldestStart)
            {
                oldestStart = _loopVoiceStart[i];
                oldest = i;
            }
        }

        _loopVoices[oldest].Stop();
        ReleaseLoopVoice(oldest);
        return oldest;
    }

    /// <summary>
    /// Creates one more looping speaker and returns its index. The pool starts
    /// small and grows only as more owners ask for a loop.
    /// </summary>
    private int CreateLoopVoice(int index)
    {
        AudioStreamPlayer2D voice = new AudioStreamPlayer2D();
        voice.Name = "LoopVoice" + index;
        AddChild(voice);

        int captured = index;

        // A properly looping stream never finishes, so this only fires if the
        // file was imported without looping and we want to clean up.
        voice.Finished += () => OnLoopVoiceFinished(captured);

        _loopVoices[index] = voice;
        _loopVoiceEntry[index] = null;
        _loopVoiceOwner[index] = null;
        _loopVoicePosition[index] = Vector2.Zero;
        _loopVoiceStart[index] = 0;
        _loopVoiceCount = index + 1;

        return index;
    }

    /// <summary>Clears the owner and entry a looping speaker was using.</summary>
    private void ReleaseLoopVoice(int index)
    {
        Node owner = _loopVoiceOwner[index];

        if (owner != null && GodotObject.IsInstanceValid(owner))
        {
            ulong ownerKey = owner.GetInstanceId();

            if (_loopVoiceByOwner.ContainsKey(ownerKey))
                _loopVoiceByOwner.Remove(ownerKey);
        }

        _loopVoiceEntry[index] = null;
        _loopVoiceOwner[index] = null;
    }

    /// <summary>
    /// Called when a looping speaker stops on its own, freeing it for reuse.
    /// </summary>
    private void OnLoopVoiceFinished(int index)
    {
        ReleaseLoopVoice(index);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Playback
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Finds the entry in the library and warns once per missing id.
    /// </summary>
    private SfxEntry ResolveEntry(StringName id)
    {
        if (Library == null)
            return null;

        SfxEntry entry = Library.Find(id);

        if (entry == null)
        {
            GD.PrintErr("SfxManager: no sound with id '" + id + "' in the library.");
            return null;
        }

        if (entry.Variants == null)
        {
            GD.PrintErr("SfxManager: sound '" + id + "' has no variants assigned.");
            return null;
        }

        return entry;
    }

    /// <summary>
    /// Checks the cooldown and the maximum simultaneous copies for a sound.
    /// </summary>
    private bool CanPlay(SfxEntry entry)
    {
        ulong now = Time.GetTicksMsec();

        if (_lastPlayTime.ContainsKey(entry.Id))
        {
            ulong last = _lastPlayTime[entry.Id];
            double elapsedSeconds = (now - last) / 1000.0;

            if (elapsedSeconds < entry.Cooldown)
                return false;
        }

        int active = 0;

        if (_activeCount.ContainsKey(entry.Id))
            active = _activeCount[entry.Id];

        if (active >= entry.MaxVoices)
            return false;

        return true;
    }

    /// <summary>
    /// Plays an entry through one of the positioned speakers.
    /// </summary>
    private void PlayWorld(SfxEntry entry, Vector2 worldPosition)
    {
        int index = AcquireWorldVoice();

        if (index < 0)
            return;

        AudioStreamPlayer2D voice = _worldVoices[index];

        // Work out whether this sound came from above, beside, or below the
        // player. Godot 2D only pans left/right, so up/down is sold with a
        // darker bus, a lower pitch, and a quieter level.
        string busName = entry.GetBusName();
        float pitchScale = 1f;
        float volumeTrimDb = 0f;

        if (entry.HeightCue == true)
        {
            GetHeightCue(worldPosition, entry, out busName, out pitchScale, out volumeTrimDb);
        }

        voice.Stream = entry.Variants;
        voice.Bus = busName;
        voice.MaxDistance = entry.MaxDistance;
        voice.Attenuation = entry.Attenuation;
        voice.VolumeDb = entry.VolumeDb + volumeTrimDb;
        voice.PitchScale = pitchScale;
        voice.PanningStrength = entry.PanningStrength;
        voice.GlobalPosition = worldPosition;

        _worldVoiceEntry[index] = entry;
        _worldVoiceStart[index] = Time.GetTicksMsec();
        _lastPlayTime[entry.Id] = _worldVoiceStart[index];

        IncrementActive(entry);
        voice.Play();
    }

    /// <summary>
    /// Plays an entry through one of the flat speakers.
    /// </summary>
    private void PlayFlat(SfxEntry entry)
    {
        int index = AcquireFlatVoice();

        if (index < 0)
            return;

        AudioStreamPlayer voice = _flatVoices[index];

        voice.Stream = entry.Variants;
        voice.Bus = entry.GetBusName();
        voice.VolumeDb = entry.VolumeDb;
        voice.PitchScale = 1f;

        _flatVoiceEntry[index] = entry;
        _lastPlayTime[entry.Id] = Time.GetTicksMsec();

        IncrementActive(entry);
        voice.Play();
    }

    /// <summary>
    /// Returns the world position audio is measured from. The AudioListener2D
    /// rides on the player, so the player position is the listener position.
    /// </summary>
    private static Vector2 GetListenerPosition()
    {
        if (Player.Instance != null && GodotObject.IsInstanceValid(Player.Instance))
            return Player.Instance.GlobalPosition;

        return Vector2.Zero;
    }

    /// <summary>
    /// Works out how a sound should sound based on whether it is above, beside,
    /// or below the player.
    ///
    /// Godot 2D audio only pans left and right, so "up" and "down" have to be
    /// sold some other way. Sounds from above stay bright, slightly raised in
    /// pitch. Sounds from below drop in pitch, get quieter, and switch to a
    /// low-passed bus, which reads as "that is behind/below me".
    /// </summary>
    private void GetHeightCue(Vector2 worldPosition, SfxEntry entry, out string busName, out float pitchScale, out float volumeTrimDb)
    {
        busName = entry.GetBusName();
        pitchScale = 1f;
        volumeTrimDb = 0f;

        Vector2 offset = worldPosition - GetListenerPosition();

        // A sound on top of the player has no meaningful direction.
        if (offset.LengthSquared() <= 0.01f)
            return;

        Vector2 direction = offset.Normalized();

        // In Godot 2D, +Y points DOWN the screen, so -Y is above the player.
        // This is +1 directly above, 0 to the side, -1 directly below.
        float height = -direction.Y;

        if (height >= 0f)
        {
            pitchScale = Mathf.Lerp(1f, Library.AbovePitchScale, height);
            return;
        }

        float below = -height;

        pitchScale = Mathf.Lerp(1f, Library.BelowPitchScale, below);
        volumeTrimDb = Mathf.Lerp(0f, Library.BelowVolumeDb, below);

        // Only clearly-below sounds get swapped onto the darker bus.
        if (below >= Library.BelowBusThreshold)
            busName = Library.BelowBus;
    }

    /// <summary>
    /// Finds a free positioned speaker. If every one is busy the oldest
    /// sound is stopped and its speaker reused, so audio never drops out.
    /// </summary>
    private int AcquireWorldVoice()
    {
        for (int i = 0; i < WORLD_VOICE_COUNT; i++)
        {
            if (_worldVoices[i].Playing)
                continue;

            ReleaseWorldVoice(i);
            return i;
        }

        int oldest = 0;
        ulong oldestStart = ulong.MaxValue;

        for (int i = 0; i < WORLD_VOICE_COUNT; i++)
        {
            if (_worldVoiceStart[i] < oldestStart)
            {
                oldestStart = _worldVoiceStart[i];
                oldest = i;
            }
        }

        _worldVoices[oldest].Stop();
        ReleaseWorldVoice(oldest);
        return oldest;
    }

    /// <summary>
    /// Finds a free flat speaker, reusing the oldest one if all are busy.
    /// </summary>
    private int AcquireFlatVoice()
    {
        for (int i = 0; i < FLAT_VOICE_COUNT; i++)
        {
            if (_flatVoices[i].Playing)
                continue;

            ReleaseFlatVoice(i);
            return i;
        }

        _flatVoices[0].Stop();
        ReleaseFlatVoice(0);
        return 0;
    }

    /// <summary>
    /// Adds one to the running count for a sound.
    /// </summary>
    private void IncrementActive(SfxEntry entry)
    {
        int active = 0;

        if (_activeCount.ContainsKey(entry.Id))
            active = _activeCount[entry.Id];

        _activeCount[entry.Id] = active + 1;
    }

    /// <summary>
    /// Removes one from the running count for a sound, if it was counted.
    /// </summary>
    private void DecrementActive(SfxEntry entry)
    {
        if (!_activeCount.ContainsKey(entry.Id))
            return;

        int active = _activeCount[entry.Id] - 1;

        if (active < 0)
            active = 0;

        _activeCount[entry.Id] = active;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Signal Handlers
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Called when a positioned speaker finishes, freeing it for reuse.
    /// </summary>
    private void OnWorldVoiceFinished(int index)
    {
        ReleaseWorldVoice(index);
    }

    /// <summary>
    /// Called when a flat speaker finishes, freeing it for reuse.
    /// </summary>
    private void OnFlatVoiceFinished(int index)
    {
        ReleaseFlatVoice(index);
    }

    /// <summary>
    /// Clears the entry a positioned speaker was using.
    /// </summary>
    private void ReleaseWorldVoice(int index)
    {
        SfxEntry entry = _worldVoiceEntry[index];

        if (entry == null)
            return;

        _worldVoiceEntry[index] = null;
        DecrementActive(entry);
    }

    /// <summary>
    /// Clears the entry a flat speaker was using.
    /// </summary>
    private void ReleaseFlatVoice(int index)
    {
        SfxEntry entry = _flatVoiceEntry[index];

        if (entry == null)
            return;

        _flatVoiceEntry[index] = null;
        DecrementActive(entry);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------
}