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

    // Manual scroll wiring (replaces the old ScrollContainer): the window that
    // clips the list, the box that slides inside it, and the bar that drives it.
    private Control _scrollListClip;
    private Control _bindingsBox;
    private VScrollBar _bindingsScrollBar;

    // Wheel tick distance for the manual scrollbar.
    private const float ScrollStep = 40f;

    // Horizontal offset applied to the right of each binding button.
    private const float BindButtonLeftOffset = 40f;

    // The action currently waiting for input, or null when not listening.
    private string _listeningAction;

    // Screen-level overlay that shows the "press a key" prompt while listening,
    // replacing the truncated in-button text.
    private Control _captureOverlay;
    private Label _captureLabel;

    // Keep listening until the player acts; used by the overlay click-to-cancel.
    private static readonly Color CapturePromptColor = new Color(0.9098039f, 0.8745098f, 0.73333335f);

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

        SetupManualScroll();

        if (ResetButton != null)
        {
            if (ButtonFont != null)
                ResetButton.AddThemeFontOverride("font", ButtonFont);
            ResetButton.AddThemeFontSizeOverride("font_size", ButtonFontSize);
            ResetButton.AddThemeColorOverride("font_color", Colors.Black);
            ResetButton.AddThemeColorOverride("font_hover_color", new Color(0.6039216f, 0.56078434f, 0.3764706f));
            ResetButton.AddThemeColorOverride("font_pressed_color", new Color(0.9098039f, 0.8745098f, 0.7333333f));
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

        // The overlay lives on the scene root, so it must be freed with the page.
        if (_captureOverlay != null)
        {
            _captureOverlay.QueueFree();
            _captureOverlay = null;
            _captureLabel = null;
        }
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
            CustomMinimumSize = new Vector2(200, 28),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            Text = BindingText(action)
        };
        if (ButtonFont != null)
            bindButton.AddThemeFontOverride("font", ButtonFont);
        bindButton.AddThemeFontSizeOverride("font_size", ButtonFontSize);
        bindButton.AddThemeColorOverride("font_color", Colors.Black);
        bindButton.AddThemeColorOverride("font_hover_color", new Color(0.6039216f, 0.56078434f, 0.3764706f));
        bindButton.AddThemeColorOverride("font_pressed_color", new Color(0.9098039f, 0.8745098f, 0.7333333f));
        bindButton.AddThemeColorOverride("font_focus_color", Colors.Black);
        bindButton.Pressed += () => StartListening(action, bindButton);

        row.AddChild(nameLabel);
        row.AddChild(bindButton);

        // Occupies the space to the button's right, pulling it away from the rail.
        var rightSpacer = new Control
        {
            CustomMinimumSize = new Vector2(BindButtonLeftOffset, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        row.AddChild(rightSpacer);

        return row;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Manual Scroll
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Locates the manual scroll nodes (clip window, content box and scrollbar)
    /// and connects them. The old ScrollContainer was replaced by a plain clip
    /// Control so the scrollbar can be authored directly in the inspector.
    /// </summary>
    private void SetupManualScroll()
    {
        _scrollListClip = GetNodeOrNull<Control>("Control - ScrollListClip");
        _bindingsBox = GetNodeOrNull<Control>("Control - ScrollListClip/BoxContainer - Bindings");
        _bindingsScrollBar = GetNodeOrNull<VScrollBar>("Control - ScrollListClip/VScrollBar");

        if (_scrollListClip == null || _bindingsBox == null || _bindingsScrollBar == null)
        {
            GD.PrintErr("ControlsPage: manual scroll nodes (ScrollListClip/BoxContainer/VScrollBar) not found!");
            return;
        }

        // Let wheel events bubble up to this page's _GuiInput.
        _scrollListClip.MouseFilter = Control.MouseFilterEnum.Pass;

        _bindingsBox.Position = Vector2.Zero;
        _bindingsScrollBar.MinValue = 0f;
        _bindingsScrollBar.ValueChanged += OnScrollChanged;
        _scrollListClip.Resized += UpdateScrollExtents;

        // Run once the layout pass has finished so all sizes are final.
        CallDeferred(nameof(UpdateScrollExtents));
    }

    /// <summary>
    /// Slides the content box upward as the scrollbar value grows.
    /// </summary>
    private void OnScrollChanged(double value)
    {
        if (_bindingsBox == null) return;
        _bindingsBox.Position = new Vector2(_bindingsBox.Position.X, -(float)value);
    }

    /// <summary>
    /// Sizes the content box to its rows and matches the scrollbar range to the
    /// amount of overflow. Hides the bar when the list fits the clip window.
    /// </summary>
    private void UpdateScrollExtents()
    {
        if (_bindingsBox == null || _bindingsScrollBar == null || _scrollListClip == null) return;

        float contentHeight = _bindingsBox.GetCombinedMinimumSize().Y;
        float clipHeight = _scrollListClip.Size.Y;

        // Keep the list clear of the scrollbar strip it shares the clip window with.
        float boxWidth = Mathf.Max(
            _bindingsBox.GetCombinedMinimumSize().X,
            _bindingsScrollBar.Position.X - 4f);
        _bindingsBox.Size = new Vector2(boxWidth, contentHeight);

        _bindingsScrollBar.Visible = contentHeight > clipHeight + 1f;
        _bindingsScrollBar.MaxValue = contentHeight;
        _bindingsScrollBar.Page = clipHeight;

        double maxScroll = Mathf.Max(0d, contentHeight - clipHeight);
        _bindingsScrollBar.Value = Mathf.Min(_bindingsScrollBar.Value, maxScroll);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Capturing
    //!---------------------------------------------------------------------------------------------------------

    private void StartListening(string action, Button button)
    {
        _listeningAction = action;

        string prompt;
        if (IsKeyboardAction(action))
        {
            prompt = "Pressione uma tecla...";
        }
        else
        {
            prompt = "Pressione um botão...";
        }
        ShowCaptureOverlay(prompt);

        // Release focus so the button cannot swallow the incoming key press
        // (a focused button on Linux/X11 can otherwise drop keyboard events).
        button.ReleaseFocus();
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (_bindingsScrollBar == null || !_bindingsScrollBar.Visible) return;

        if (@event is InputEventMouseButton mb
            && mb.Pressed
            && (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
        {
            double delta = mb.ButtonIndex == MouseButton.WheelUp ? -ScrollStep : ScrollStep;
            _bindingsScrollBar.Value += delta;
            AcceptEvent();
        }
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
        HideCaptureOverlay();
        _listeningAction = null;
    }

    private void FinishListening()
    {
        HideCaptureOverlay();
        _listeningAction = null;
        // The SettingsChanged refresh fired during SetBinding while we were
        // still listening (and was suppressed), so push a refresh now.
        RefreshAllBindings();
    }

    /// <summary>
    /// Builds the screen-level capture prompt once and shows it above the menu.
    /// </summary>
    private void ShowCaptureOverlay(string prompt)
    {
        EnsureCaptureOverlay();
        if (_captureLabel == null || _captureOverlay == null) return;
        _captureLabel.Text = prompt;
        _captureOverlay.Visible = true;
    }

    private void HideCaptureOverlay()
    {
        if (_captureOverlay != null)
            _captureOverlay.Visible = false;
    }

    private void EnsureCaptureOverlay()
    {
        if (_captureOverlay != null) return;

        _captureOverlay = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _captureOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var dim = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.55f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _captureOverlay.AddChild(dim);

        // Clicking anywhere on the dimmed backdrop cancels the capture.
        _captureOverlay.GuiInput += (InputEvent ev) =>
        {
            if (ev is InputEventMouseButton click && click.Pressed)
                CancelListening();
        };

        var center = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _captureOverlay.AddChild(center);

        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.06f, 0.95f)
        };
        style.SetContentMarginAll(30f);
        style.SetCornerRadiusAll(8);
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);

        _captureLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (LabelFont != null)
            _captureLabel.AddThemeFontOverride("font", LabelFont);
        _captureLabel.AddThemeFontSizeOverride("font_size", 26);
        _captureLabel.AddThemeColorOverride("font_color", CapturePromptColor);
        panel.AddChild(_captureLabel);

        // Host it inside the menu's own CanvasLayer so it shares the same canvas
        // space and is drawn above every page.
        Node host = GetTree().CurrentScene;
        if (host == null)
            host = GetTree().Root;
        host.AddChild(_captureOverlay);
        _captureOverlay.Visible = false;
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
