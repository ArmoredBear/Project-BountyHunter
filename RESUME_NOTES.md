# RESUME NOTES

---

## SESSION RULES (CURRENT — read first)
1. **Read files before planning/editing** — read the relevant code/scene/shader files immediately before thinking about a change.
2. **Ask permission before every edit** — state what will be done and get the OK first.
3. **Ask questions for decisions & methods** — do NOT decide alone on approach; the user picks the method (they find the real problems).
4. **One thing at a time** — do not overdo or overthink; make the smallest change that addresses the reported issue.
5. **Build & check after every edit batch** — run `dotnet build` (0 errors) and headless resource checks before finishing.
6. **Research online before thinking of a solution** — when proposing a fix, look up the relevant technique/docs first.
7. **NEVER TOUCH GITHUB. EVER.** — never `git push`, never `git commit`, never `git pull`, never open or
   close PRs/issues, never use `gh`, and never run any other command that talks to GitHub. Local `git status`
   / `git diff` / `git log` read-only inspection is fine. All work stays local and uncommitted unless the
   user explicitly asks otherwise in the moment.

---

## Session Rules
1. **Build after every edit batch** — run `dotnet build` before finishing. 0 errors required.
2. **Every `[Export]` gets a `/// <summary>`** — add descriptions to all new exported variables.
   Remember: summary must go ABOVE `[ExportGroup]`, not between `[ExportGroup]` and `[Export]`.
3. **No ternary operators** — use simple if/else blocks instead. Ternaries (`? :`) are hard to
   read, especially for someone learning the codebase.
4. **NEVER TOUCH GITHUB. EVER.** — no `git push`, `git commit`, `git pull`, `gh`, PRs or issues.
   Local read-only git inspection is fine; everything stays local and uncommitted.

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

---

## C# Vision (image viewing — plugin)

Lets the agent see images (art, sprites, screenshots) even though `big-pickle` is a
text-only model. Works via an MCP server that analyzes the image externally and returns
text.

### Setup (already done)
1. `opencode-vision` (Python package) installed in `~/.config/opencode/vision-venv`.
2. MCP server `vision` registered in `~/.config/opencode/opencode.json` using **Ollama**:
   - `VISION_PROVIDER=ollama`
   - `OLLAMA_BASE_URL=http://localhost:11434`
   - `OLLAMA_MODEL=moondream` (local, free, NO API key — Google API could not be created).
3. Ollama installed via Homebrew, runs in background and auto-starts at login
   (`~/.config/autostart/ollama.desktop`). If not running: `ollama serve`.
4. Model pulled: `ollama pull moondream` (~1.7 GB, CPU-only, no GPU needed).

### Tools
- `vision_describe(image_path, prompt?)` — describe an image (colors, layout, style)
- `vision_ocr(image_path)` — extract text (moondream is weak at this)
- `vision_analyze(image_path)` — metadata + description + OCR

### Usage notes
- Pass the **absolute path** as `image_path`. Example: `vision_describe("res://... tree.png")`
  → actually use the full filesystem path, e.g.
  `/mnt/STORAGE/GodotProjects/Project-BountyHunter/...png`.
- After changing MCP config, **quit and restart opencode** — config only loads at startup.
- moondream is fast/decent on art but weak on text. To upgrade descriptions, swap
  `OLLAMA_MODEL` in `opencode.json` (e.g. `qwen2.5vl:3b` or `llava`) and `ollama pull <model>`.
- Tool descriptions may include unreliable details (hallucinated shapes/text) — take them
  as a rough read, not ground truth, especially for color/palette work.

---

Project root: `/mnt/STORAGE/GodotProjects/Project-BountyHunter`

---

## CURRENT WORK: Snow system simplified to falling snow only (2026-10-13)

**Status: rolling back the snow-accumulation system at user request; keeping only the falling snow.**

- User removed all non-falling snow from `Forest.tscn` themselves (SnowController node, SnowMoundController,
  mounds polygon + material, frozen-overlay material, baker node are gone; only `Snow` > `Snow_Fall` sprite
  with `ShaderMaterial_snfall` + `Snowfall_World.gdshader` remains).
- Deleted the whole snow system so it can't be resurrected accidentally:
  shaders `Snow_Ground_Mounds.gdshader`, `Snow_Mound.gdshader`, `Snow_Overlay.gdshader`,
  `Frozen_Ground_Overlay.gdshader` (all + `.uid`); scripts `SnowMoundController.cs`, `SnowMoundWorldBaker.cs`
  (the bake system), `SnowController.cs` (all + `.uid`); removed `snow_amount` from `project.godot`
  `[shader_globals]`.
- KEPT (falling snow): `Snowfall_World.gdshader` + `.uid` (Forest `Snow_Fall`), `Snowfall.gdshader` + `.uid`
  and `SnowfallMaterial.tres` (used by `Main_Menu.tscn`).
- The CPU-bake work from 2026-10-12 is void — do not attempt to restore it unless the user asks.
- Next when user returns: verify build/headless are clean; optionally resume the snow-fall tuning that existed
  before the accumulation system (SnowController ramped flake density via the `intensity` uniform).