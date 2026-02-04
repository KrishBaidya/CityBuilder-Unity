using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 2f;
    public float maxZoom = 20f;
    public float zoomSpeed = 2f;
    
    [Header("Pan Settings")]
    public float panSpeed = 10f;
    public Vector2 minPanLimit = new Vector2(-50, -50);
    public Vector2 maxPanLimit = new Vector2(50, 50);
    
    [Header("Touch Settings")]
    public float touchZoomSpeed = 0.1f;
    
    private Camera cam;
    private Vector3 dragOrigin;
    private bool isDragging = false;
    
    // For touch pinch zoom
    private float initialPinchDistance;
    private float initialZoom;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // Desktop controls
        HandleMouseZoom();
        HandleMousePan();
        
        // Mobile controls
        HandleTouchControls();
    }

    void HandleMouseZoom()
    {
        if (Input.touchCount == 0) // Only on desktop
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }
    }

    void HandleMousePan()
    {
        if (Input.touchCount == 0) // Only on desktop
        {
            // Start drag with middle mouse or right mouse
            if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1))
            {
                dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
                isDragging = true;
            }

            // End drag
            if (Input.GetMouseButtonUp(2) || Input.GetMouseButtonUp(1))
            {
                isDragging = false;
            }

            // Perform drag
            if (isDragging)
            {
                Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newPos = transform.position + difference;
                
                // Clamp position
                newPos.x = Mathf.Clamp(newPos.x, minPanLimit.x, maxPanLimit.x);
                newPos.y = Mathf.Clamp(newPos.y, minPanLimit.y, maxPanLimit.y);
                
                transform.position = newPos;
            }
        }
    }

    void HandleTouchControls()
    {
        if (Input.touchCount == 1)
        {
            // Single finger pan
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                dragOrigin = cam.ScreenToWorldPoint(touch.position);
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(touch.position);
                Vector3 newPos = transform.position + difference;
                
                newPos.x = Mathf.Clamp(newPos.x, minPanLimit.x, maxPanLimit.x);
                newPos.y = Mathf.Clamp(newPos.y, minPanLimit.y, maxPanLimit.y);
                
                transform.position = newPos;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
        else if (Input.touchCount == 2)
        {
            // Two finger pinch zoom
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                initialZoom = cam.orthographicSize;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                float deltaPinch = initialPinchDistance - currentPinchDistance;
                
                cam.orthographicSize = initialZoom + (deltaPinch * touchZoomSpeed);
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }
    }

    // Smooth zoom to specific position (useful for AI commands)
    public void ZoomToPosition(Vector3 position, float targetZoom = 5f, float duration = 0.5f)
    {
        StartCoroutine(SmoothZoomTo(position, targetZoom, duration));
    }

    System.Collections.IEnumerator SmoothZoomTo(Vector3 targetPos, float targetZoom, float duration)
    {
        Vector3 startPos = transform.position;
        float startZoom = cam.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Smooth interpolation
            t = t * t * (3f - 2f * t); // Smoothstep
            
            transform.position = Vector3.Lerp(startPos, new Vector3(targetPos.x, targetPos.y, startPos.z), t);
            cam.orthographicSize = Mathf.Lerp(startZoom, targetZoom, t);
            
            yield return null;
        }
    }
}