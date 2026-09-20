# RESUME NOTES

---

## SESSION RULES (CURRENT — read first)
1. **Read files before planning/editing** — read the relevant code/scene/shader files immediately before thinking about a change.
2. **Ask permission before every edit** — state what will be done and get the OK first.
3. **Ask questions for decisions & methods** — do NOT decide alone on approach; the user picks the method (they find the real problems).
4. **One thing at a time** — do not overdo or overthink; make the smallest change that addresses the reported issue.
5. **Build & check after every edit batch** — run `dotnet build` (0 errors) and headless resource checks before finishing.
6. **Research online before thinking of a solution** — when proposing a fix, look up the relevant technique/docs first.

---

## Session Rules
1. **Build after every edit batch** — run `dotnet build` before finishing. 0 errors required.
2. **Every `[Export]` gets a `/// <summary>`** — add descriptions to all new exported variables.
   Remember: summary must go ABOVE `[ExportGroup]`, not between `[ExportGroup]` and `[Export]`.
3. **No ternary operators** — use simple if/else blocks instead. Ternaries (`? :`) are hard to
   read, especially for someone learning the codebase.

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

Project root: `/mnt/STORAGE/GodotProjects/Project-BountyHunter`