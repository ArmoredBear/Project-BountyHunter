using Godot;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SFXLIBRARY
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	**
	**  1 - Resource that holds every SfxEntry in the game.
	**  2 - Sounds are added by editing this resource in the Inspector, so
	**      replacing an audio file never requires touching code.
	**  3 - The SfxManager loads this and looks entries up by name.
	**
*-----------------------------------------------------------------------------------------------------------------------**/

[GlobalClass]
public partial class SfxLibrary : Resource
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>Every sound effect available to the game.</summary>
    [Export] public SfxEntry[] Entries { get; set; }

    /// <summary>
    /// Bus used for sounds coming from below the player. It is low-passed so
    /// they sound muffled, and it feeds into the "SFX" bus so the SFX volume
    /// slider and the limiter still apply to it.
    /// </summary>
    [Export] public string BelowBus { get; set; } = "SFX_Below";

    /// <summary>How far below the player a sound must be before it switches to the darker bus.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float BelowBusThreshold { get; set; } = 0.35f;

    /// <summary>Pitch multiplier for a sound coming from directly below the player.</summary>
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float BelowPitchScale { get; set; } = 0.88f;

    /// <summary>Pitch multiplier for a sound coming from directly above the player.</summary>
    [Export(PropertyHint.Range, "1,1.5,0.01")] public float AbovePitchScale { get; set; } = 1.04f;

    /// <summary>Extra volume in dB (negative) for a sound coming from directly below the player.</summary>
    [Export(PropertyHint.Range, "-24,0,0.5")] public float BelowVolumeDb { get; set; } = -5f;

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization
    //!---------------------------------------------------------------------------------------------------------

    private Dictionary<StringName, SfxEntry> _lookup;

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Finds the entry with the given name. Returns null when not found.
    /// </summary>
    public SfxEntry Find(StringName id)
    {
        BuildLookup();
        if (_lookup.ContainsKey(id))
            return _lookup[id];

        return null;
    }

    /// <summary>
    /// Builds the name lookup table once, then reuses it.
    /// </summary>
    private void BuildLookup()
    {
        if (_lookup != null)
            return;

        _lookup = new Dictionary<StringName, SfxEntry>();

        if (Entries == null)
            return;

        foreach (SfxEntry entry in Entries)
        {
            if (entry == null)
                continue;

            if (entry.Id == "")
                continue;

            _lookup[entry.Id] = entry;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!\---------------------------------------------------------------------------------------------------------
}