using UnityEngine;

public class CameraMove : MonoBehaviour
{
	private Camera cam;

	[Header("Map Bounds")]
	[SerializeField] private float mapMinX = -142f;
	[SerializeField] private float mapMaxX = 20f;
	[SerializeField] private float mapMinY = -12f;
	[SerializeField] private float mapMaxY = 140f;

	[Header("Free Zoom")]
	[SerializeField] private float minZoom = 3.6f;
	[SerializeField] private float maxZoom = 43.31f;
	[SerializeField] private float zoomSpeed = 5f;

	[Header("Pixel Zoom")]
	[SerializeField] private bool useStepZoom = false;
	[SerializeField] private float[] zoomSteps = new float[] { 4f, 6f, 9f, 13f, 18f, 24f, 32f, 43f };
	[SerializeField] private bool snapCameraToPixelGrid = true;

	[Header("Drag Move")]
	[SerializeField] private bool useDragMove = true;
	[SerializeField] private int dragMouseButton = 1; // 0: 좌클릭, 1: 우클릭, 2: 휠클릭

	[Header("Edge Scroll")]
	[SerializeField] private bool useEdgeScroll = true;
	[SerializeField] private float edgeSize = 20f;          // 화면 끝 감지 범위(px)
	[SerializeField] private float edgeScrollSpeed = 25f;   // 이동 속도

	public bool move = false;

	private Vector3 dragOrigin;
	private int currentZoomStepIndex;

	private void Awake()
	{
		cam = Camera.main;
		currentZoomStepIndex = GetClosestZoomStepIndex(cam != null ? cam.orthographicSize : minZoom);
		ApplyCurrentZoomStep();
	}

	private void Update()
	{
		if (move == true)
		{
			CamZoominZoomOut();
			CamMove();
		}
	}

	private void LateUpdate()
	{
		ClampCamera();
		SnapCameraToPixelGrid();
	}

	private void CamZoominZoomOut()
	{
		if (cam == null) return;

		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll == 0f) return;

		if (useStepZoom && zoomSteps != null && zoomSteps.Length > 0)
		{
			if (scroll > 0f)
				currentZoomStepIndex = Mathf.Max(0, currentZoomStepIndex - 1);
			else if (scroll < 0f)
				currentZoomStepIndex = Mathf.Min(zoomSteps.Length - 1, currentZoomStepIndex + 1);

			ApplyCurrentZoomStep();
			return;
		}

		cam.orthographicSize -= scroll * zoomSpeed;
		cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
	}

	private void CamMove()
	{
		if (cam == null) return;

		// 1. 드래그 이동
		if (useDragMove)
		{
			if (Input.GetMouseButtonDown(dragMouseButton))
			{
				dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
			}

			if (Input.GetMouseButton(dragMouseButton))
			{
				Vector3 current = cam.ScreenToWorldPoint(Input.mousePosition);
				Vector3 diff = dragOrigin - current;
				diff.z = 0f;

				transform.position += diff;
			}
		}

		// 2. 화면 끝 이동
		if (useEdgeScroll)
		{
			Vector3 move = Vector3.zero;
			Vector3 mousePos = Input.mousePosition;

			if (mousePos.x <= edgeSize)
				move.x -= 1f;
			else if (mousePos.x >= Screen.width - edgeSize)
				move.x += 1f;

			if (mousePos.y <= edgeSize)
				move.y -= 1f;
			else if (mousePos.y >= Screen.height - edgeSize)
				move.y += 1f;

			if (move != Vector3.zero)
			{
				move.Normalize();
				transform.position += move * edgeScrollSpeed * Time.deltaTime;
			}
		}
	}

	private void ClampCamera()
	{
		if (cam == null) return;

		Vector3 pos = transform.position;
		float halfH = cam.orthographicSize;
		float halfW = halfH * cam.aspect;

		float x = Mathf.Clamp(pos.x, mapMinX + halfW, mapMaxX - halfW);
		float y = Mathf.Clamp(pos.y, mapMinY + halfH, mapMaxY - halfH);

		transform.position = new Vector3(x, y, pos.z);
	}

	private void SnapCameraToPixelGrid()
	{
		if (!snapCameraToPixelGrid || cam == null || !cam.orthographic)
			return;

		float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
		if (unitsPerPixel <= 0f)
			return;

		Vector3 pos = transform.position;
		pos.x = Mathf.Round(pos.x / unitsPerPixel) * unitsPerPixel;
		pos.y = Mathf.Round(pos.y / unitsPerPixel) * unitsPerPixel;
		transform.position = pos;
	}

	private int GetClosestZoomStepIndex(float currentZoom)
	{
		if (zoomSteps == null || zoomSteps.Length == 0)
			return 0;

		int closestIndex = 0;
		float closestDistance = Mathf.Abs(zoomSteps[0] - currentZoom);

		for (int i = 1; i < zoomSteps.Length; i++)
		{
			float distance = Mathf.Abs(zoomSteps[i] - currentZoom);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestIndex = i;
			}
		}

		return closestIndex;
	}

	private void ApplyCurrentZoomStep()
	{
		if (cam == null || zoomSteps == null || zoomSteps.Length == 0)
			return;

		currentZoomStepIndex = Mathf.Clamp(currentZoomStepIndex, 0, zoomSteps.Length - 1);
		cam.orthographicSize = zoomSteps[currentZoomStepIndex];
	}
}