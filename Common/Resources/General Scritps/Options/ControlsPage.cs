using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   CONTROLSPAGE
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Controls tab of the Options panel; builds a rebind row per input action (keyboard and gamepad lists).
	**  2 - Lets the player capture a new key/button by clicking its binding button; rebinds are applied immediately
	**	through the Settings autoload and persist to user://settings.cfg.
	**  3 - Temp/debug actions (toggle_console, Heal, Damage, TempSave, TempLoad) are intentionally not rebindable.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class ControlsPage : Control
{
    // Vertical lists that receive one row per rebindable action.
    [Export] public BoxContainer KeyboardList;
    [Export] public BoxContainer GamepadList;

    // Optional button that restores every binding to its project default.
    [Export] public Button ResetButton;

    // Font used by the action-name labels. If empty, the project theme font is used.
    [Export] public Font LabelFont;
    [Export(PropertyHint.Range, "8,72")] public int LabelFontSize = 20;

    // Font used by the binding buttons. If empty, the project theme font is used.
    [Export] public Font ButtonFont;
    [Export(PropertyHint.Range, "8,72")] public int ButtonFontSize = 20;

    // The action currently waiting for input, or null when not listening.
    private string _listeningAction;

    // The row button that shows the "press a key" prompt while listening.
    private Button _listeningButton;

    private static readonly string[] KeyboardActions =
    {
        "Keyboard_Up",
        "Keyboard_Down",
        "Keyboard_Left",
        "Keyboard_Right",
        "Keyboard_Evade",
        "Keyboard_Run",
        "Keyboard_Interact",
        "Keyboard_UseItem",
        "Keyboard_Light_Attack",
        "Keyboard_Heavy_Attack",
        "Dialogue_Interact",
        "Menu",
    };

    private static readonly string[] GamepadActions =
    {
        "Game_Pad_Up",
        "Game_Pad_Down",
        "Game_Pad_Left",
        "Game_Pad_Right",
        "Game_Pad_Evade",
        "Game_Pad_Run",
        "Game_Pad_Interact",
        "Game_Pad_UseItem",
        "Game_Pad_Light_Attack",
        "Game_Pad_Heavy_Attack",
    };

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        if (Settings.Instance == null)
        {
            GD.PrintErr("ControlsPage: Settings autoload not found!");
            return;
        }

        BuildRowList(KeyboardList, KeyboardActions);
        BuildRowList(GamepadList, GamepadActions);

        if (ResetButton != null)
        {
            if (ButtonFont != null)
                ResetButton.AddThemeFontOverride("font", ButtonFont);
            ResetButton.AddThemeFontSizeOverride("font_size", ButtonFontSize);
            ResetButton.AddThemeColorOverride("font_color", Colors.Black);
            ResetButton.AddThemeColorOverride("font_hover_color", Colors.Black);
            ResetButton.AddThemeColorOverride("font_pressed_color", Colors.Black);
            ResetButton.AddThemeColorOverride("font_focus_color", Colors.Black);
            ResetButton.Pressed += () => Settings.Instance.ResetAllBindings();
        }

        Settings.Instance.SettingsChanged += RefreshAllBindings;
    }

    /// <summary>
    /// Unsubscribes from the Settings signal when the node is removed so a
    /// freed ControlsPage is never called after the scene changes.
    /// </summary>
    public override void _ExitTree()
    {
        base._ExitTree();
        if (Settings.Instance != null)
            Settings.Instance.SettingsChanged -= RefreshAllBindings;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Row Building
    //!---------------------------------------------------------------------------------------------------------

    private void BuildRowList(BoxContainer list, string[] actions)
    {
        if (list == null) return;
        foreach (string action in actions)
            list.AddChild(CreateRow(action));
    }

    /// <summary>
    /// Creates one HBox row: a label with the action's display name and a
    /// button showing its current binding.
    /// </summary>
    private Control CreateRow(string action)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 34)
        };
        row.SetMeta("action", action);

        var nameLabel = new Label
        {
            Text = DisplayName(action),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (LabelFont != null)
            nameLabel.AddThemeFontOverride("font", LabelFont);
        nameLabel.AddThemeFontSizeOverride("font_size", LabelFontSize);
        nameLabel.AddThemeColorOverride("font_color", Colors.Black);

        var bindButton = new Button
        {
            CustomMinimumSize = new Vector2(110, 28),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            Text = BindingText(action)
        };
        if (ButtonFont != null)
            bindButton.AddThemeFontOverride("font", ButtonFont);
        bindButton.AddThemeFontSizeOverride("font_size", ButtonFontSize);
        bindButton.AddThemeColorOverride("font_color", Colors.Black);
        bindButton.AddThemeColorOverride("font_hover_color", Colors.Black);
        bindButton.AddThemeColorOverride("font_pressed_color", Colors.Black);
        bindButton.AddThemeColorOverride("font_focus_color", Colors.Black);
        bindButton.Pressed += () => StartListening(action, bindButton);

        row.AddChild(nameLabel);
        row.AddChild(bindButton);
        return row;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Capturing
    //!---------------------------------------------------------------------------------------------------------

    private void StartListening(string action, Button button)
    {
        _listeningAction = action;
        _listeningButton = button;
        button.Text = "Pressione uma tecla...";
        // Release focus so the button cannot swallow the incoming key press
        // (a focused button on Linux/X11 can otherwise drop keyboard events).
        button.ReleaseFocus();
    }

    public override void _Input(InputEvent @event)
    {
        if (_listeningAction == null) return;

        // Escape always cancels capture.
        if (@event is InputEventKey cancelKey
            && cancelKey.Pressed
            && cancelKey.PhysicalKeycode == Key.Escape)
        {
            CancelListening();
            GetViewport().SetInputAsHandled();
            return;
        }

        bool isKeyboardAction = IsKeyboardAction(_listeningAction);
        InputEvent capture = null;

        if (isKeyboardAction && @event is InputEventKey key && key.Pressed && !key.Echo)
        {
            capture = key;
        }
        else if (!isKeyboardAction && @event is InputEventJoypadButton joyBtn && joyBtn.Pressed)
        {
            capture = joyBtn;
        }
        else if (!isKeyboardAction
                 && @event is InputEventJoypadMotion motion
                 && motion.AxisValue != 0f)
        {
            capture = motion;
        }

        if (capture != null)
        {
            Settings.Instance.SetBinding(_listeningAction, capture);
            FinishListening();
            GetViewport().SetInputAsHandled();
        }
    }

    private void CancelListening()
    {
        if (_listeningButton != null)
            _listeningButton.Text = BindingText(_listeningAction);
        _listeningAction = null;
        _listeningButton = null;
    }

    private void FinishListening()
    {
        // Show the freshly captured binding before clearing the listening state,
        // since the SettingsChanged refresh is suppressed while we are listening.
        if (_listeningButton != null)
            _listeningButton.Text = BindingText(_listeningAction);
        _listeningAction = null;
        _listeningButton = null;
    }

    private static bool IsKeyboardAction(string action)
    {
        return System.Array.IndexOf(KeyboardActions, action) >= 0;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Refresh
    //!---------------------------------------------------------------------------------------------------------

    private void RefreshAllBindings()
    {
        if (_listeningAction != null) return;
        RefreshList(KeyboardList);
        RefreshList(GamepadList);
    }

    private void RefreshList(BoxContainer list)
    {
        if (list == null) return;
        foreach (Node child in list.GetChildren())
        {
            if (child is not BoxContainer row) continue;
            string action = (string)row.GetMeta("action", "");
            foreach (Node rowChild in row.GetChildren())
            {
                // Re-setting the text re-translates it to the current locale,
                // so rows follow a language change made at runtime.
                if (rowChild is Label label)
                    label.Text = DisplayName(action);
                else if (rowChild is Button bindButton)
                    bindButton.Text = BindingText(action);
            }
        }
    }

    /// <summary>
    /// Returns the display text for an action's current primary binding.
    /// Key names are kept as-is; the two dynamic formats ("Botão N" and
    /// "Eixo N +/-") are looked up through tr() so they can be translated.
    /// </summary>
    private string BindingText(string action)
    {
        if (!InputMap.HasAction(action)) return "-";
        var events = InputMap.ActionGetEvents(action);
        if (events.Count == 0) return "-";
        InputEvent ev = events[0];

        if (ev is InputEventKey key)
        {
            if (key.PhysicalKeycode == Key.None)
            {
                return "-";
            }
            return OS.GetKeycodeString(key.PhysicalKeycode);
        }
        if (ev is InputEventJoypadButton btn)
            return string.Format(Tr("Botão {0}"), (int)btn.ButtonIndex);
        if (ev is InputEventJoypadMotion motion)
        {
            string sign;
            if (motion.AxisValue < 0f)
            {
                sign = "-";
            }
            else
            {
                sign = "+";
            }
            return string.Format(Tr("Eixo {0} {1}"), (int)motion.Axis, sign);
        }
        return "-";
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Localization
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Friendly display names for the rebind rows. The Portuguese strings are
    /// the translation-table keys: the Label's auto-translate turns them into
    /// English when the game language is set to "en", and RefreshList re-applies
    /// them whenever the language changes at runtime.
    /// </summary>
    private string DisplayName(string action)
    {
        return action switch
        {
            "Keyboard_Up" or "Game_Pad_Up" => "Cima",
            "Keyboard_Down" or "Game_Pad_Down" => "Baixo",
            "Keyboard_Left" or "Game_Pad_Left" => "Esquerda",
            "Keyboard_Right" or "Game_Pad_Right" => "Direita",
            "Keyboard_Evade" or "Game_Pad_Evade" => "Esquivar",
            "Keyboard_Run" or "Game_Pad_Run" => "Correr",
            "Keyboard_Interact" or "Game_Pad_Interact" => "Interagir",
            "Keyboard_UseItem" or "Game_Pad_UseItem" => "Usar Item",
            "Keyboard_Light_Attack" or "Game_Pad_Light_Attack" => "Ataque Leve",
            "Keyboard_Heavy_Attack" or "Game_Pad_Heavy_Attack" => "Ataque Pesado",
            "Dialogue_Interact" => "Diálogo",
            "Menu" => "Menu",
            _ => action,
        };
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
