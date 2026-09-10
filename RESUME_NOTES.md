# RESUME NOTES — Options Menu / Controls Tab

---

## CURRENT STATE (LAST UPDATE: 2026-09-09) — Inventory system

> Work is GREEN: `dotnet build` = 0 errors, clean headless boot (only pre-existing
> `invalid UID` warnings + "resources still in use at exit", both benign).

### Inventory open/close UI wiring (DONE)
- New `Inventory` input action in `project.godot` = keyboard **I** (physical_keycode 73).
  Gamepad **Select** still opens the pause menu (unchanged `Menu` action).
- `UI_Scripts/InventoryMenu.cs` (NEW, attached to `Inventory_UI.tscn` root CanvasLayer):
  - Layer starts hidden; **I** toggles the inventory.
  - `Toggle(bool close_player_menu_on_open)` / `Open()` / `Close()` visibility helpers.
  - `Setup_Category_Tabs()`: maps the 5 tab buttons (Consumables/Weapons/Armor/Tools/Etc)
    to the 5 category scroll lists via `Show_Tab(index)`; Consumables is default.
- `UI_Scripts/PlayerMenu.cs` (CanvasLayer, `Player_Menu.tscn`):
  - `_Ready` starts with menu hidden.
  - **Esc / Select restore logic**: remember the last open overlay state
    (`_resume_menu_visible` / `_resume_inventory_visible`). When everything is closed, the
    next open reopens the previous overlay(s); when something is open, it captures state and
    closes everything in ONE press. First-ever open shows the pause menu.
  - Inventory button (`On_Inventory_Pressed` -> `InventoryMenu.Toggle(false)`) TOGGLES the
    inventory WITHOUT closing the pause menu. Keyboard **I** (`Toggle(true)`) DOES close the menu.
  - `Save & Quit` handler unchanged (Save -> MainMenu).

### Items defined (NEW starter items, all `Common/Resources/Player/PlayerScripts/PlayerInventory/`)
- `ItemData.cs` (template: ItemID, Name, Icon, Type, SlotPerItem, EffectID) — pre-existing.
- `ItemType` enum EXTENDED to: `Consumable, Weapon, Armor, Tool, Etc` (Etc = 4 NEW).
- Starter items granted on a NEW GAME (`Player_Data_Autoload.Reset()` ->
  `Add_Starter_Items(inventory)`):
  - `Pill.tres` (Consumable / type 0, EffectID "pill")
  - `Sword.tres` (Weapon / type 1)
  - `Plate.tres` (Armor / type 2)
  - `Lantern.tres` (Tool / type 3)
  - `UpgradeStone.tres` (Etc / type 4)
- Inventory data flows: `PlayerInventory` (Node, autoload under `/root/Player/Inventory`)
  stores by type; `Player_Data_Autoload` serializes into `Player_Data.Inventory` on save and
  restores on load (now includes the Etc category).

### Inventory list display (DONE)
- `UI_Scripts/InventoryUI.cs` attached to the 4 (now 5) category `BoxContainer` lists in
  `Inventory_UI.tscn`. Each list sets `Item_Category` (0..4) via an `[Export]`.
- `RefreshUI()` clears old buttons and rebuilds them from
  `PlayerInventory.GetItemsByType(Item_Category)` as `"Name xQty"` buttons (fixed size,
  `SizeFlagsVertical = ShrinkCenter` — matches the original placeholder sizing; no stretch).
- Auto-refreshes on `ItemAdded` / `ItemRemoved` / `InventoryUpdated`.
- CONSUMABLE USE (DONE): in the Consumables list each button's `Pressed` ->
  `Use_Item(item)` -> `Messenger.Use_Item(item)` (runs the EffectID) then
  `_playerInventory.RemoveItem(item, 1)` -> UI auto-refreshes. Non-consumable lists stay inert.

### Tabs in the scene
- 5 tabs: Consumables, Weapons, Armor, Tools, **Tools (Button4)**, **Etc (Button5)**.
- 5 category lists: `ScrollContainer - Inventory_List_Consumables / Weapons / Armors / Tools /
  Etc`. The Etc list was ADDED; lists other than the default (Consumables) are `visible = false`.

### Messenger cleanup (DONE) — `Common/Resources/General Scritps/Messenger.cs`
- REMOVED: legacy quick-items path + hardcoded `/root/Main/Player/...` wiring,
  per-frame `Items_Use_Emitter()` poller (and its null hazard), unused `Usable_Item_`
  surface and the Item use short-circuit that bypassed effects.
- KEPT (reserved-empty for future logic per user): `Fixed_Items*` members.
- KEPT: `Initialize()` / `Signals_Setter()` (wires `Item_Use_ -> ItemEffects.Use`),
  `Use_Item(ItemInstance)`, and the 4 stat-relay emit helpers
  (`Emit_Player_Health/Stamina/Armor_Changed`, `Emit_Player_Took_Damage`).

### ItemEffects (`ItemEffects.cs`, pre-existing)
- Registry maps EffectID -> action: `heal_small`, `heal_large`, `pill`.
- Pill = regen-over-time via `Player_Data_Autoload.Start_Pill_Regen(totalHeal)`.

---

Save this so work can be resumed later. Update this file as you go.

Project root: `/mnt/STORAGE/GodotProjects/Project-BountyHunter`

---

## Session Rules
1. **Build after every edit batch** — run `dotnet build` before finishing. 0 errors required.
2. **Every `[Export]` gets a `/// <summary>`** — add descriptions to all new exported variables.
   Remember: summary must go ABOVE `[ExportGroup]`, not between `[ExportGroup]` and `[Export]`.
3. **No ternary operators** — use simple if/else blocks instead. Ternaries (`? :`) are hard to
   read, especially for someone learning the codebase.

---

## Status (LAST UPDATE: 2026-08-15)

Armor bar (shader + damage flash + damage wiring) and ECG trace shadow are DONE. Build is green
(`dotnet build`, 0 errors) and all shaders import clean. See "Just finished (session 5)" below.

### Just finished (session 6) — Armor bar glint: angle + pass interval
- `ArmorBar_Shader.gdshader` glint rework:
  - NEW `Glint_Angle` (0-360, default 17.0) — rotates the glint sweep direction.
    Old fixed direction was UV.x*0.5 + UV.y*0.15 (angle ~16.7deg).
  - NEW `Glint_Interval` (0.5-10, default 3.0) — seconds between glint passes. This splits
    "repeat frequency" (interval) from `Glint_Speed` (band travel velocity), which were the
    same knob before (old period = 1/Glint_Speed, e.g. 0.35 -> ~2.86s).
  - Sweep math: `center = mod(TIME, interval) * Glint_Speed - 0.2`,
    `band = exp(-pow((dot(UV, glint_dir) - center) * 2.0, 2.0) * 40.0)`. NO fract, NO gate/step.
  - CRITICAL (flicker fix): the band must FULLY exit off-screen before the cycle resets, else
    it's still visible at the far edge when `center` jumps back -> visible snap = flicker.
    `dot_max = abs(dir.x) + abs(dir.y)`, `span = dot_max + 0.4`, `travel = span / Glint_Speed`,
    `interval = max(Glint_Interval, travel)`. Band fades in/out via its own gaussian, so the
    reset jump happens entirely off-screen.
  - Defaults keep the pre-session continuous look (travel ~2.9s vs interval 3.0).
  - Lesson: with a diagonal glint the bar's dot extent is cos+sin (~1.25 for magnitude-1 dir,
    ~0.62 for the *0.5 dir) — never assume it spans 0..1.

### Just finished (session 5) — Armor bar shader + damage flash + armor damage wiring + ECG shadow
- Armor bar UI: `Status/Armor_Bar` in Player_UI.tscn is now a `TextureRect` (was NinePatchRect) with
  `expand_mode=1`, texture `Common/Resources/Player/User Interface/UI_Art/Armor_Bar.png` (400x45,
  drawn 400x24), material `ShaderMaterial_f24em` (ArmorBar_Shader.gdshader). Script
  `UI_Scripts/Player_Armorbar_UI.cs` is attached to that node.
- `Common/Resources/Shaders/ArmorBar_Shader.gdshader` — FINAL STATE: metal glint + steel glow +
  procedural scratches + edge wear + damage flash. ALL crack code was REMOVED (user judged the
  cracks bad; may draw a custom texture instead). Uniforms: Glint_Speed / Glint_Intensity /
  Glint_Color, Steel_Glow, Scratch_Amount, Edge_Wear, Flash_Color, Flash_Intensity (0-2),
  Flash (script-driven 0..1).
- IMPORTANT shader gotcha: Godot 4 REMOVED the `MODULATE` built-in (it existed in 3.5). The armor
  shader starts from the built-in `COLOR` (which already = texture * Modulate * Self Modulate)
  instead of `texture(TEXTURE, UV)` — sampling TEXTURE raw would IGNORE the node's Modulate /
  Self Modulate. This fixed the armor bar being untintable in the inspector.
- `Player_Armorbar_UI.cs` (extends TextureRect): polls `Player_Data_Autoload.Data.CURRENT_Armor`
  each frame in `_Process`; when it DROPS (a hit absorbed by armor) it sets the shader's `Flash`
  uniform to 1.0 and fades it to 0 over 0.25s (const Armor_Flash_Time). Flash color/intensity are
  tuned on the material in the inspector (Flash_Color / Flash_Intensity).
- Armor damage wiring (`Player_Data.cs` `TakeDamage`): if `Armored && CURRENT_Armor > 0` the armor
  absorbs the hit with HALVED damage (`ApplyArmorDamage(damage / 2)`) and health takes NOTHING; if
  halved damage exceeds remaining armor the hit is FULLY absorbed and armor BREAKS
  (`CURRENT_Armor = 0; Armored = false;`) — no overflow to health. Otherwise -> `ApplyHealthDamage`.
- `Player_Data_Autoload.cs`: added `Player_Armorbar` export + GetNode wiring +
  `Update_Player_Armor_UI()` (mirrors the health wiring). TEMP TEST config in `_Ready`:
  `Data.MAX_Armor = 100; Data.CURRENT_Armor = 100; Data.Armored = true;` (a real armor-equip
  system doesn't exist yet).
- `Player_Healthbar_UI.cs`: armor fields/methods REMOVED (extracted into Player_Armorbar_UI.cs);
  ECG + health logic untouched. Stale `Armor_Bar` NodePath also removed from `Health_Bar` in the tscn.
- ECG trace SHADOW: `Player_Healthbar_UI.cs` mirrors the ECG points onto an optional
  `Monitor_Shadow` (Line2D) offset by `Shadow_Offset` (default (2.5, 3.0)), auto-found at
  `Heart_Monitor/Monitor_Shadow`. EDITOR SETUP: duplicate `Monitor_Line`, rename to
  `Monitor_Shadow`, REMOVE its material (no glow shader), set a dark Default Color (e.g.
  Color(0,0,0,0.5)), keep the same Scale (0.5, 0.5), and place it ABOVE `Monitor_Line` in the tree
  so it draws behind. `Shadow_Offset` is tweaked on the `Health_Bar` inspector.
- Build verified green (`dotnet build`, 0 errors); headless `--import` validates all shaders.

### Previously unlogged (sessions between 4 and 5 — health UI)
- ECG heart monitor was MERGED into `Player_Healthbar_UI.cs` (Points-based Line2D trace driving the
  `Heart_Monitor/Monitor_Line`, recolors with the Lines bar, beats faster as health drops, flatlines
  at zero). `_Debug/Heart_Monitor_Line.cs` was deleted.
- `WeatherChooser.cs` rain/snow no longer picks the combined "rain + snow" option (removed the
  50/50 branch); `Player_Data_Autoload.cs` / `GameOverScreen.cs` now only set `Lines.Value`.
- Snow-particle spin: investigated, left UNRESOLVED (known Godot 4.6 bug #115547 — Angular Velocity
  on GPUParticles2D with a particle material did not visibly rotate; user moved on).

### PENDING / user's future intentions (armor)
- Damage flash currently fires only when ARMOR drops. If the user wants a flash on ANY hit (even
  with no armor left), hook the hit flow instead of polling CURRENT_Armor.
- User may draw a custom armor texture / replace the shader look ("ill try to draw something myself").
- Future: hide Armor_Bar when the player has no armor equipped; show a fully-broken bar state when
  armor is broken — planned once the armor equip system lands.

### Just finished (session 4) — Main Menu lightning + vignette effects
- `Common/Resources/Shaders/LightningFlash.gdshader` created — fullscreen blue-white flash.
  Samples `screen_texture`, driven by a `flash_strength` uniform (0..1) + per-frame hash
  flicker. Knobs: flash_color, white_amount, flicker_amount, flicker_speed.
- `Common/Resources/Misc/Effects/LightningEffect.cs` created (Node2D) — random auto-strikes.
  Generates jagged main bolts + side branches (random perpendicular jitter), drives the flash
  material's `flash_strength` every frame. Exports: MinInterval (5) / MaxInterval (10)
  [real seconds between strikes], FlashDuration (0.8), DrawBolts (true), BoltCount (3),
  BoltWidth (2.5), BoltGlowWidth (14), BoltGlowAlpha (0.3), BoltTipTaper (0.2).
- BOLT TIPS ARE POINTY: `_Draw` no longer uses constant-width DrawPolyline. It builds a
  TAPERED POLYGON (`BuildTaperedPolygon`, `DirectionAt`) whose width goes to 0 at both ends,
  drawn with `DrawColoredPolygon` (glow pass + core pass) + a thin white core line on top.
- `Common/Resources/Shaders/Vignette.gdshader` created — edge darkening. IMPORTANT: the first
  version sampled `screen_texture` and the WHOLE SCREEN went black (screen_texture returns
  black in this CanvasLayer setup). Rewritten to NOT sample the screen: it just outputs a
  translucent `vignette_color` alpha gradient (mix/alpha only), which darkens whatever is
  beneath it. Knobs: vignette_color, vignette_intensity (0-2), vignette_radius, vignette_softness,
  vignette_roundness (1=round, 0=square).
- Main_Menu.tscn currently has: `ColorRect - LightningFlash` (full-rect, ShaderMaterial_obmtt
  using LightningFlash.gdshader) + `Node2D - LightningEffect` (LightningEffect.cs,
  FlashRect = "../ColorRect - LightningFlash"). Both placed right after the black background —
  SEE PENDING EDITOR WORK below (they must be moved later in the tree).
- KNOWN BENIGN EDITOR WARNING: `ext_resource, invalid UID: uid://bg2shqbn8mqfe - using text path
  instead` for LightningFlash.gdshader. The .uid file DOES match; it's just the editor's
  `.godot/uid_cache.bin` being stale. Fix: close/reopen the editor, or delete `.godot/uid_cache.bin`
  (auto-regenerates). Not an error.

### Side note (session 4) — user's microphone was fixed (not project work)
- PipeWire had the ALC1220 card on profile `output:analog-stereo` (output only, no input).
- Fix: `pactl set-card-profile alsa_card.pci-0000_31_00.4 output:analog-stereo+input:analog-stereo`
  then `pactl set-source-port .../analog-stereo analog-input-rear-mic`. Mic source appeared.
- If the mic ever disappears again: check `pactl list short sources`; re-run the set-card-profile
  command (an app or profile-restore may reset it to output-only).

### Main Menu effects - EDITOR INSTRUCTIONS (pending, session 4) — USER
1. **Move both lightning nodes later in the tree.** They are currently right after the black
   background (2nd child), BEFORE `Sprite2D - Clouds`. The flash shader lights only what is
   drawn BEFORE it (screen_texture), so right now it only lights the black background — the
   clouds/logo/menu never get lit. Drag `ColorRect - LightningFlash` and `Node2D - LightningEffect`
   to near the END of `Main_Menu`'s children (e.g. just after `Control - Menu and Logic`) so the
   whole screen (menu included) flashes. Bolts-on-top-of-flash = put `Node2D - LightningEffect`
   AFTER `ColorRect - LightningFlash`.
2. **Vignette ColorRect** — user creates a fullscreen ColorRect child of `Main_Menu`, assigns a
   ShaderMaterial with Vignette.gdshader. It must sit ABOVE the nodes it should darken (topmost
   = over the whole menu). NOT wired into the scene yet.
3. User tweak targets: lightning frequency feels good at 5-10s; bolt tips via `BoltTipTaper`.

Controls tab is FUNCTIONALLY COMPLETE and BUILD IS GREEN (`dotnet build` succeeds, 0 errors).
BLACK TEXT CHANGE IS DONE and the build was verified after it.
LANGUAGE TAB: all CODE + CSV + project.godot work is DONE and build is green. Only the
SCENE (editor) work is left — done by the user per the division of work (instructions below).

### Just finished (session 3) — Language tab, code side
- `Common/Resources/Localization/translations.csv` created (PT strings as keys, `en` column).
- `project.godot` -> `[internationalization]` registers the GENERATED translation file
  `translations.en.translation` (NOT the .csv — see bug #4 below) and sets `locale/fallback="pt"`.
- `Settings.cs`: added `SectionLanguage`, `_language` (default "pt"), `Language` property,
  `SetLanguage()` (validates pt/en, applies, saves, emits SettingsChanged) and `ApplyLanguage()`
  (`TranslationServer.SetLocale`). Language loads at boot via `LoadSettings`.
- `LanguagePage.cs` created (OptionButton "Português"/"English", SettingsChanged subscription
  + `_ExitTree` unsubscribe, mirrors ControlsPage pattern).
- `ControlsPage.cs`: `DisplayName`/`BindingText` now instance methods; `BindingText` translates
  "Botão {0}" and "Eixo {0} {1}" via `Tr()`; `RefreshList` also re-sets the name labels so rows
  follow a runtime language change. PT names remain the tr() keys (auto-translate shows EN).
- `OptionsMenu.cs`: dropdowns rebuild when the language actually changes (window-mode items).
- `dotnet build` verified: 0 errors.

### CRITICAL editor fixes found (main menu won't switch language without them)
- The 5 main menu buttons `Continue`, `Load_Game`, `Options`, `Credits`, `Quit` each have
  `language = "en"` set in the scene. That per-Control override FORCES English on them no
  matter what the game locale is -> must be cleared (reset to default/empty) in the editor.
- The `Credits` button text is `"Creditos"` + trailing newline, and the `Language` tab button
  text is `"Idioma"` + trailing newline. CSV keys are exact-match, so the newlines must be
  removed (retype the text) for those two strings to translate.
- These are the ONLY places the keys in translations.csv depend on.

### Just finished (session 2)
- BLACK TEXT on Controls rows: `nameLabel` / `bindButton` in `CreateRow()` + `ResetButton`
  now use `AddThemeColorOverride("font_color", Colors.Black)` (buttons also override
  `font_hover_color`, `font_pressed_color`, `font_focus_color`).
- `RESUME_NOTES.md` created in the project root so work can be resumed.
- Language tab: PLAN DOCUMENTED only (see "Language tab - IMPLEMENTATION PLAN" below).

## Language tab - current scene state (IMPORTANT)
- The tab button ALREADY exists: `OptionsPanel/TabBar/Language` (text "Idioma"). 
- An EMPTY page ALREADY exists: `OptionsPanel/Control - Content/LanguagePage` (a `Control`,
  `visible = false`, no children, no script). The `ConnectTabs()` in OptionsMenu.cs matches
  tabs to pages by child order, so this page is already wired to the Language button.
- Pages order under `Control - Content`: AudioPage(0), VideoPage(1), ControlsPage(2), LanguagePage(3).
- NO localization setup exists yet: `project.godot` has no locale/translation entries and there
  are no `.csv`/translation files in the project.

## Language tab - editor layout pattern to replicate (from VideoPage)
Each settings row is: `Box - XList` (VBox, anchors full rect, separation ~30)
  -> `Box - XRow` (VBox, centered) -> `Label` (label_settings, text "Resolução" etc.)
     + control (OptionButton/CheckBox, theme Base_Main_Menu_Theme, font springmarch size 20).
See `Control - Content/Control - VideoPage/Box - VideoList/Box - ResolutionRow` for reference.
The LanguagePage Control in the scene is sized like ControlsPage: offset_left=153, offset_top=134,
offset_right=470, offset_bottom=~971 (use ControlsPage size).

---

## Goal

Godot 4.6 + C# (Project Bounty Hunter). Building an Options menu with a working Controls tab
(keyboard + gamepad rebinding, persistence). Longer term: Language tab (pt/en) + layout cleanup.

## Division of work
- The USER does all editor changes themselves. The ASSISTANT writes C# code and gives
  step-by-step editor instructions.
- User CANNOT view screenshots — text descriptions only.
- User communicates in English mostly; UI strings are Portuguese ("Resolução", "Modo de
  Janela", "Restaurar Padrões", "Pressione uma tecla...").

---

## Editor / Godot 4.6 naming gotchas (IMPORTANT for future instructions)
- Size flags are called **Container Sizing** (under `Layout`) and only appear on children of a
  Container.
- Offsets / `anchor_*` are engine properties NOT shown in the inspector when anchors are
  Top Left. Instead the inspector shows `Transform -> Position / Size`.
- Use `Layout -> Anchors Preset`, `Layout -> Container Sizing`, NOT "offsets"/"size flags".

## Bugs found & fixed (learnings)
1. **Dangling event subscription** -> `ObjectDisposedException: Cannot access a disposed
   object. Godot.BoxContainer` after scene change. Fixed by adding `_ExitTree()` overrides
   that unsubscribe from `Settings.Instance.SettingsChanged` in `ControlsPage.cs` and
   `OptionsMenu.cs`.
2. **Keyboard capture swallowed on Linux/X11**: focused button eats key events. Fix: use
   `_Input` (runs before GUI) instead of `_UnhandledInput`, and call `ReleaseFocus()` in
   `StartListening` instead of `GrabFocus()`.
3. **Button text didn't update after rebind**: `RefreshAllBindings` is suppressed while
   listening, so `FinishListening()` sets the button text directly.
4. **`Failed loading resource: .../translations.csv` (Condition "found" is true)** at boot:
   Godot imports the CSV fine (generates `translations.<locale>.translation`) but CANNOT load
   the `.csv` itself as a resource when it is listed under `locale/translations`. Fix: list the
   GENERATED `.translation` file in `project.godot` instead, e.g.
   `locale/translations=PackedStringArray("res://Common/Resources/Localization/translations.en.translation")`.
   (See godotengine/godot issue #71816.) Re-import happens automatically whenever the csv changes;
   a new locale column = a new `translations.<locale>.translation` file that must be added to the list.

---

## Current Controls tab architecture (in Main_Menu.tscn)
`Control - ControlsPage` (script ControlsPage.cs)
`-> ScrollContainer - Bindings`
`   -> BoxContainer - Bindings` (vertical, horizontal fill = Container Sizing 3)
`      -> Label - Section Divider - Keyboard` (uses LabelSettings_npx8l, already black w/ outline)
`      -> KeyboardList` (BoxContainer)
`      -> Label - Section Divider - GamePad`
`      -> GamepadList` (BoxContainer)
`-> ResetButton` (outside scroll)

## ControlsPage.cs key pieces
- Exports: `KeyboardList`, `GamepadList` (BoxContainer), `ResetButton` (Button),
  `LabelFont`, `LabelFontSize` (8-72), `ButtonFont`, `ButtonFontSize` (8-72).
- `CreateRow(action)`: HBox with nameLabel (ExpandFill, black) + bindButton
  (ShrinkEnd, min 110x28, black text). Row meta stores "action".
- `StartListening` -> `_Input` captures key (keyboard actions) or joypad button/axis
  (gamepad actions); Escape cancels. `SetBinding` + `FinishListening`.
- `BindingText(action)`: reads first event of `InputMap.ActionGetEvents` ->
  keycode string / "Botão N" / "Eixo N +/-" / "-".
- `DisplayName(action)`: returns PT strings, which are the tr() keys (auto-translate -> EN).
- Keyboard actions: Keyboard_Up/Down/Left/Right/Evade/Run/Interact/UseItem/Light_Attack/
  Heavy_Attack + Dialogue_Interact + Menu (NOT rebindable: toggle_console, Heal, Damage,
  TempSave, TempLoad).
- Gamepad actions: Game_Pad_Up/Down/Left/Right/Evade/Run/Interact/UseItem/Light_Attack/Heavy_Attack.

## Settings.cs key pieces
- Autoload `Settings`. Controls region: `SetBinding(action, event)`, `ResetBinding`,
  `ResetAllBindings` (restores project-default snapshot), `ApplyControlsBindings`
  (writes InputMap from user data), `EncodeBinding`/`DecodeBinding`.
- Emits `SettingsChanged`; persists to `user://settings.cfg`.

## Relevant files
- `Common/Resources/General Scritps/Options/ControlsPage.cs`
- `Common/Resources/General Scritps/Options/Settings.cs`
- `Common/Resources/General Scritps/Options/OptionsMenu.cs` (has _ExitTree unsubscribe)
- `Common/MainScenes/Main_Menu.tscn`
- NOTE: the folder is named "General Scritps" (typo) — keep it, do not rename.

---

## NEXT STEPS (pick one when resuming)
1. **Finish Main Menu effects scene work** (see "Main Menu effects - EDITOR INSTRUCTIONS
   (pending)" above): move LightningFlash/LightningEffect later in the tree; create + place the
   Vignette ColorRect; then judge the bolts — user said they may want to disable bolts
   (`DrawBolts=false`) and keep flash-only if the bolts look odd.
2. **OptionsPanel layout cleanup** (deferred).
2. Translate the rest of the game's scenes as text is added — see "WORKFLOW: adding
   translatable text" below.
3. Add more languages later (add a CSV column + register the new `.translation`).

---

## Language tab - EDITOR INSTRUCTIONS (session 3, scene work — USER)

### A. Fixes in Main_Menu.tscn (REQUIRED first, else main menu won't switch languages)
1. Select each of these buttons one at a time and CLEAR the `language` override (Inspector,
   the "Language" dropdown/field near the top of Control properties -> reset it to default/empty):
   `Continue`, `Load_Game`, `Options`, `Credits`, `Quit`.
2. Select the `Credits` button -> its Text currently reads "Creditos" with a line break after it
   -> retype it to just `Creditos` (no trailing newline).
3. Select `OptionsPanel/TabBar/Language` -> Text currently "Idioma" + newline -> retype to just
   `Idioma` (no trailing newline).

### B. Build the LanguagePage content (empty page exists: OptionsPanel/Control - Content/LanguagePage)
Replicate the VideoPage row layout (see "editor layout pattern" note above).
Use inspector naming: with Top Left anchors the inspector shows Transform -> Position/Size;
the Layout -> Offset Left/Top/Right/Bottom fields only appear while anchors are NOT Top Left.
1. Select `LanguagePage` (visible=false currently). Layout -> Anchors Preset -> Top Left.
   Transform -> Position (X=153, Y=134); Transform -> Size (W=317, H=837)  [same as ControlsPage].
2. Add child `Box - LanguageList` (VBoxContainer). Layout -> Anchors Preset -> Full Rect.
   Now the Layout section shows Offset Left/Top/Right/Bottom fields -> set Left=43, Top=24,
   Right=-48, Bottom=0. Theme Override Constants -> Separation = 30. Alignment = Center.
3. Add child `Box - LanguageRow` (VBoxContainer) under it. Alignment = Center.
4. Under `Box - LanguageRow` add a Label named `Language`: Text = `Idioma`. Set its
   "Label Settings" to the same sub-resource used by the `Resolução` label (any of the
   26px black/outline ones).
5. Under `Box - LanguageRow` add an OptionButton named `LanguageOption`:
   Custom Minimum Size (X=0, Y=30); Theme = Base_Main_Menu_Theme; Theme Override Fonts ->
   Font = springmarch.roman.otf, Font Size = 20; Alignment = Center.
6. Select `LanguagePage` -> Attach Script -> pick `LanguagePage.cs` (Options folder).
7. With `LanguagePage` selected, in Inspector under "Script Variables" set `LanguageOption`
   to the `LanguageOption` node (use the node picker).

### C. Verify
- Run the game. Language dropdown shows "Português"/"English". Picking "English" switches the
  whole menu/options/controls UI to English; picking "Português" switches back.
- The choice persists to user://settings.cfg and applies at boot.
- NOTE: first editor open after these changes will import the .csv and generate LanguagePage.cs.uid.

---

## Language tab - IMPLEMENTATION PLAN (DONE for code/CSV/project.godot; scene pending — see above)

PURPOSE (user's words): "I want this tab to be an option for the player to select the
language of the ENTIRE game." Not just the Options menu — the whole game. This session
only documented the plan; implementation is a future session.

APPROACH (chosen): Godot built-in CSV + `tr()` system. Standard pipeline, auto-translates
scene labels, future-proof for more languages.

### Key strategy (IMPORTANT DESIGN CHOICE)
Use the EXISTING Portuguese strings as the CSV keys (e.g. key `Resolução`, en `Resolution`).
Because Godot looks up a label's text in the translation table when the locale is not the
default, this means EVERY scene label (main menu buttons, tabs, labels) gets translated with
ZERO scene edits. When locale = `pt` there is no `pt` column, so `tr("Resolução")` returns
the key unchanged (i.e. Portuguese). When locale = `en` it returns the English value.
Caveat: keys contain diacritics/accents — ugly but functional. Alternative (cleaner keys like
`LBL_RESOLUTION`) requires editing every label's text in the scene = more editor work.

### Step-by-step (DONE — kept for reference)
1. **translations.csv** — create `Common/Resources/Localization/translations.csv`.
   Header row `keys,en`; one row per string: `Resolução,Resolution`, `Modo de Janela,Window Mode`,
   `Janela,Windowed`, `Tela Cheia,Fullscreen`, `Sem Bordas,Borderless`, `Vsync,Vsync`,
   `Controles,Controls`, `Idioma,Language`, `Restaurar Padrões,Restore Defaults`,
   `Pressione uma tecla...,Press a key...`, tab texts (Audio/Video/...), code-generated strings
   (Cima/Up, Baixo/Down, Esquerda/Left, Direita/Right, Esquivar/Evade, Correr/Run,
   Interagir/Interact, Usar Item/Use Item, Ataque Leve/Light Attack, Ataque Pesado/Heavy Attack,
   Diálogo/Dialogue, Menu/Menu, `Botão {0}`/`Button {0}`, `Eixo {0} {1}`/`Axis {0} {1}`), and the
   main menu (Novo Jogo/New Game, Carregar/Load, Opções/Options, Creditos/Credits, Sair/Quit).
   NOTE: `Botão N`/`Eixo N +/-` keys use `{0}`/`{1}` format placeholders (not "N"), because they
   are built in code with `string.Format(Tr(...))`.
2. **project.godot** — under `[internationalization]` -> `locale/translations` list the GENERATED
   file `res://Common/Resources/Localization/translations.en.translation` (NOT the .csv — see
   bug #4). `locale/fallback="pt"`. Godot reimports the csv automatically on editor focus.
3. **Settings.cs** — add language support (mirror the video settings pattern):
   - `private const string SectionLanguage = "language";`
   - `private string _language = "pt";` + public `string Language => _language;`
   - load in `LoadSettings()`: `_language = (string)_config.GetValue(SectionLanguage, "language", "pt");`
   - `SetLanguage(string lang)` -> validate (pt/en), `_language = lang`, `ApplyLanguage()`,
     `SaveSettings()`, `EmitSignal(SettingsChanged)`.
   - `ApplyLanguage()` -> `TranslationServer.SetLocale(_language);`
   - save in `SaveSettings()`: `_config.SetValue(SectionLanguage, "language", _language);`
   - Call `ApplyLanguage()` once in `_Ready`/`LoadSettings` so the saved language applies at boot.
4. **LanguagePage.cs** (new script, pattern = ControlsPage) — exports `LanguageOption`
   (OptionButton). `_Ready`: populate with "Português" (pt) + "English" (en), subscribe to
   `Settings.Instance.SettingsChanged`, set `Selected` from `Settings.Instance.Language`.
   On `ItemSelected`: `Settings.Instance.SetLanguage(code)`. `_ExitTree`: unsubscribe
   (reuse the dangling-subscription fix pattern).
5. **Scene (editor)** — fill the existing empty `LanguagePage` under
   `Control - Content`: replicate the VideoPage row layout (see note above):
   `Box - LanguageList` (VBox, anchors full rect, separation 30) ->
   `Box - LanguageRow` (VBox centered) -> `Label` ("Idioma", label_settings) +
   `LanguageOption` (OptionButton, theme Base_Main_Menu_Theme, font springmarch size 20).
   Attach LanguagePage.cs, set the export to the OptionButton.
6. **Code strings to switch to `tr()`** — `ControlsPage.DisplayName`, `ControlsPage`
   "Pressione uma tecla...", `ControlsPage.BindingText` ("Botão N" / "Eixo N +/-"),
   `OptionsMenu.PopulateResolutionOptions` window mode items ("Janela"/"Tela Cheia"/"Sem Bordas").

### Runtime behavior notes
- `TranslationServer.SetLocale()` at runtime DOES update existing scene/Control text automatically
  (Godot 4 auto-translate). Code-generated rows are additionally re-applied on `SettingsChanged`
  (ControlsPage.RefreshList, OptionsMenu dropdown rebuild) as a safety net.
- Language applies game-wide via `tr()` — every scene that uses text will follow.

---

## WORKFLOW: adding translatable text during development (session 3+)
The engine translates at RUNTIME; the developer only maintains `translations.csv` (the dictionary).
- Scene text (Label/Button): type Portuguese as usual. For each NEW visible string, add a row
  `Portuguese,English` to the CSV. Key must match the scene text EXACTLY (accents, case, no
  trailing spaces/newlines). Missing rows = text just stays PT (safe, no crash).
- C# code strings: wrap in `Tr("...")`. Dynamic strings: `string.Format(Tr("Você tem {0} moedas"), n)`
  with the key `Você tem {0} moedas`.
- After editing the CSV, Godot reimports automatically on editor focus (regenerates
  `translations.en.translation`). No command needed. If stale: FileSystem -> select csv ->
  Import -> Reimport.
- Helper: Project -> Tools -> Localization -> POT Generation lists every string used in scenes/scripts
  (spot new strings needing rows). Run the game in EN and look for text still in PT to find gaps.
- New language later: add a column (e.g. `es`) to the CSV -> Godot generates
  `translations.es.translation` -> add it to `locale/translations` in project.godot.

(End of file)

---

## C# Inspector Tooltips (plugin)

Plugin: `addons/csharp_inspector_tooltips/` (godot-csharp-inspector-tooltips by Nikita-Myshkin).
Shows C# XML `<summary>` comments as hover tooltips in the Godot Inspector for exported properties.

### Setup (already done)
1. `Project Bounty Hunter.csproj` has `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.
2. Plugin enabled in **Project > Project Settings > Plugins > C# Inspector Tooltips**.

### Adding descriptions to new exports
Put `/// <summary>` **ABOVE** the `[ExportGroup]` (not between `[ExportGroup]` and `[Export]`).
The `[ExportGroup]` attribute eats the doc comment if it comes after it — the C# compiler
does not associate the summary with the field when `[ExportGroup]` is between them.

CORRECT:
```csharp
/// <summary>Description of this field.</summary>
[ExportGroup("My Group")]
[Export(PropertyHint.Range, "0,100,1")]
public float MyField = 50f;
```

WRONG (summary gets eaten by ExportGroup):
```csharp
[ExportGroup("My Group")]
/// <summary>This will NOT appear in the inspector.</summary>
[Export(PropertyHint.Range, "0,100,1")]
public float MyField = 50f;
```

Fields that are NOT the first after an `[ExportGroup]` can have the summary anywhere above
the `[Export]` — only the first field in a group has this issue.

After editing summaries, **rebuild** (`dotnet build` or Build button in editor) — the plugin
reads the generated XML file next to the DLL. Hover over the property in the inspector to see
the tooltip.
