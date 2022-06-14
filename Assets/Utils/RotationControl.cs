using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationControl : MonoBehaviour
{
	public float radius = 35.0f;
	float xangle = Mathf.PI / 2;
	float yangle = -Mathf.PI / 2;

	float radiusSmooth;
	float xangleSmooth;
	float yangleSmooth;

	void Start()
	{
		radiusSmooth = radius;
		xangleSmooth = xangle;
		yangleSmooth = yangle;
	}

	void Update()
	{
		var mouseButton = Input.GetMouseButton(1);
		var shift = Input.GetKey(KeyCode.LeftShift);

		var xdelta = Input.GetAxis("Mouse X") * 0.04f * (shift ? 2.0f : 1.0f);
		var ydelta = Input.GetAxis("Mouse Y") * 0.04f * (shift ? 2.0f : 1.0f);
		radius += Input.mouseScrollDelta.y * -1.0f * (shift ? 3.0f : 1.0f);

		radius = Mathf.Clamp(radius, 0.3f, 50.0f);

		if (mouseButton)
		{
			xangle -= xdelta;
			yangle -= ydelta;
			yangle = Mathf.Clamp(yangle, Mathf.PI * -0.99f, Mathf.PI * -0.01f);
		}

		radiusSmooth = Mathf.Lerp(radiusSmooth, radius, 0.1f);
		xangleSmooth = Mathf.Lerp(xangleSmooth, xangle, 0.1f);
		yangleSmooth = Mathf.Lerp(yangleSmooth, yangle, 0.1f);

		var position = new Vector3
		{
			x = radiusSmooth * Mathf.Sin(yangleSmooth) * Mathf.Cos(xangleSmooth),
			z = radiusSmooth * Mathf.Sin(yangleSmooth) * Mathf.Sin(xangleSmooth),
			y = radiusSmooth * Mathf.Cos(yangleSmooth)
		};

		transform.position = position + Vector3.one * 15.5f;
		transform.LookAt(Vector3.one * 15.5f);
		
	}
}
