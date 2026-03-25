using UnityEngine;
using static UnityEditor.PlayerSettings;

public class CameraMove : MonoBehaviour
{
	private Camera cam;

	float mapMinX = -138f;    // 맵 왼쪽 끝
    float mapMaxX = 16f;  // 맵 오른쪽 끝

	float mapMinY = -8f;    // 맵 아래 끝
	float mapMaxY = 136;   // 맵 위 끝

	float minZoom = 3.6f;
	float maxZoom = 43.31f;

	private float zoomSpeed = 5f;

	Vector3 dragOrigin;

	private void Awake()
	{
		cam = Camera.main;
	}

	private void LateUpdate()
	{
		ClampCamera();
	}

	private void Update()
	{
		CamZoominZoomOut();
		CamMove();
	}

	private void CamZoominZoomOut()
	{
		float scroll = Input.GetAxis("Mouse ScrollWheel");

		if (scroll != 0f)
		{
			cam.orthographicSize -= scroll * zoomSpeed;
			cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
		}
	}

	private void CamMove()
	{
		if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
		{
			dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
		}

		if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
		{
			Vector3 current = cam.ScreenToWorldPoint(Input.mousePosition);
			Vector3 diff = dragOrigin - current;

			transform.position += diff;
		}
	}

	private void ClampCamera()
	{
		Vector3 pos = transform.position;

		float halfH = cam.orthographicSize;
		float halfW = halfH * cam.aspect;

		float x = Mathf.Clamp(pos.x, mapMinX + halfW, mapMaxX - halfW);
		float y = Mathf.Clamp(pos.y, mapMinY + halfH, mapMaxY - halfH);

		transform.position = new Vector3(x, y, pos.z);
	}
}

