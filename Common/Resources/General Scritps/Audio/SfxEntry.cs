using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SFXENTRY
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	**
	**  1 - Resource that describes a single sound effect: which name it is
	**      called by, which files it can use, and the rules for playing it.
	**  2 - These entries are authored inside a SfxLibrary resource and are
	**      read by the SfxManager when a sound is played.
	**  3 - Nothing in here plays audio by itself. It is only data.
	**
*-----------------------------------------------------------------------------------------------------------------------**/

[GlobalClass]
public partial class SfxEntry : Resource
{
    //!---------------------------------------------------------------------------------------------------------
    #region Types
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>Audio bus a sound is routed through.</summary>
    public enum SfxBus
    {
        SFX,
        UI,
        Ambience
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>Name used to play this sound, for example "sword_swoosh".</summary>
    [Export] public StringName Id { get; set; } = "";

    /// <summary>Pool of interchangeable recordings. Handles pitch and volume variation.</summary>
    [Export] public AudioStreamRandomizer Variants { get; set; }

    /// <summary>Audio bus this sound plays through.</summary>
    [Export] public SfxBus Bus { get; set; } = SfxBus.SFX;

    /// <summary>True when the sound has a position in the world and should pan left and right.</summary>
    [Export] public bool Spatial { get; set; } = true;

    /// <summary>How far away the sound can still be heard, in world pixels.</summary>
    [Export(PropertyHint.Range, "100,20000,100,or_greater")] public float MaxDistance { get; set; } = 8000f;

    /// <summary>How quickly the sound fades with distance. 1 is linear, lower stays audible further out.</summary>
    [Export(PropertyHint.Range, "0,4,0.05,or_greater")] public float Attenuation { get; set; } = 1f;

    /// <summary>Per sound volume trim in decibels.</summary>
    [Export(PropertyHint.Range, "-40,12,0.5")] public float VolumeDb { get; set; } = 0f;

    /// <summary>How strongly the sound pans left and right. Higher exaggerates the direction.</summary>
    [Export(PropertyHint.Range, "0,3,0.05")] public float PanningStrength { get; set; } = 2f;

    /// <summary>True when the sound gets darker and quieter as it moves below the player.</summary>
    [Export] public bool HeightCue { get; set; } = true;

    /// <summary>
    /// True when this sound repeats forever instead of stopping at the end of
    /// the file. Looping sounds play their first recording directly, because
    /// the randomiser has no loop mode, and they ignore pitch/volume variation.
    /// The audio file also has to be imported with looping switched on.
    /// </summary>
    [Export] public bool Loop { get; set; } = false;

    /// <summary>Minimum seconds between two plays of this sound. Stops footsteps machine-gunning.</summary>
    [Export(PropertyHint.Range, "0,2,0.01")] public float Cooldown { get; set; } = 0f;

    /// <summary>Maximum copies of this sound allowed to play at the same time.</summary>
    [Export(PropertyHint.Range, "1,16,1")] public int MaxVoices { get; set; } = 6;

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Returns the audio bus name matching the Bus enum value.
    /// </summary>
    public string GetBusName()
    {
        if (Bus == SfxBus.UI)
            return "UI";

        if (Bus == SfxBus.Ambience)
            return "Ambience";

        return "SFX";
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------
}