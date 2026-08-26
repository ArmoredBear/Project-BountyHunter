using Godot;

public partial class FlowerPatch_RandomStart : AnimatedSprite2D
{
	public override void _Ready()
	{
		if (SpriteFrames == null) return;

		string anim = Animation;
		int frameCount = SpriteFrames.GetFrameCount(anim);

		if (frameCount > 0)
		{
			Frame = (int)(GD.Randi() % (uint)frameCount);
		}
	}
}
