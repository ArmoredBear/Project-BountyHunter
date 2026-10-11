extends Node
# BH_PERF measurement HUD. Prints one self-contained perf line every 60 frames
# when the BH_PERF env var is set, e.g.:
#   PERF fps=60 frame_ms=16.67 process_ms=0.00 physics_ms=0.00
#        render_cpu_ms=... render_gpu_ms=... max_cpu_ms=... max_gpu_ms=...
#        draw_calls=... objects=... frames=...
# Disabled entirely unless BH_PERF is present, so normal gameplay is untouched.
# Timing source: RenderingServer viewport measured render times (ms).

var _frames := 0
var _max_cpu := 0.0
var _max_gpu := 0.0
var _enabled := OS.has_environment("BH_PERF")
var _vp : RID

func _ready() -> void:
	if not _enabled:
		return
	_vp = get_viewport().get_viewport_rid()
	RenderingServer.viewport_set_measure_render_time(_vp, true)

func _process(_delta: float) -> void:
	if not _enabled:
		return
	_frames += 1
	if _frames % 60 != 0:
		return
	var fps := Engine.get_frames_per_second()
	var frame_ms := 0.0 if fps <= 0.0 else 1000.0 / fps
	var proc_ms := Performance.get_monitor(Performance.TIME_PROCESS) * 1000.0
	var phys_ms := Performance.get_monitor(Performance.TIME_PHYSICS_PROCESS) * 1000.0
	var cpu_ms := RenderingServer.viewport_get_measured_render_time_cpu(_vp)
	var gpu_ms := RenderingServer.viewport_get_measured_render_time_gpu(_vp)
	_max_cpu = max(_max_cpu, cpu_ms)
	_max_gpu = max(_max_gpu, gpu_ms)
	print("PERF fps=%d frame_ms=%.2f process_ms=%.2f physics_ms=%.2f render_cpu_ms=%.2f render_gpu_ms=%.2f max_cpu_ms=%.2f max_gpu_ms=%.2f draw_calls=%.1f objects=%.1f frames=%d" % [
		fps, frame_ms, proc_ms, phys_ms, cpu_ms, gpu_ms, _max_cpu, _max_gpu,
		Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME),
		Performance.get_monitor(Performance.RENDER_TOTAL_OBJECTS_IN_FRAME),
		_frames])
