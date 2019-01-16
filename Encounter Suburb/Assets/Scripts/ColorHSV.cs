using UnityEngine;

public struct ColorHSV
{
	public float hue;
	public float saturation;
	public float value;

	public ColorHSV(float hue, float saturation, float value)
	{
		this.hue = hue;
		this.saturation = saturation;
		this.value = value;
	}

	public static implicit operator Color(ColorHSV c)
	{
		return Color.HSVToRGB(c.hue, c.saturation, c.value);
	}

	public static explicit operator ColorHSV(Color c)
	{
		float hue, saturation, value;
		Color.RGBToHSV(c, out hue, out saturation, out value);
		return new ColorHSV(hue, saturation, value);
	}
}