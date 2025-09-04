using Godot;
using System;

public interface IUse
{
	public void Use(bool _usable);
	public void Use(bool _usable, int _value);
	public void Use(bool _usable, int _value, int _modifier);
	public void Use(bool _usable, int _value, bool _modifier);	
}

