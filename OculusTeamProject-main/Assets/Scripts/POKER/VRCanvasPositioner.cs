using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRCanvasPositioner : MonoBehaviour
{
    private Canvas worldSpaceCanvas;
    private Camera xrCamera;

    void Start()
    {
        worldSpaceCanvas = GetComponent<Canvas>();

        // XR 카메라 찾기
        var xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
        {
            xrCamera = xrOrigin.Camera;
        }

        if (xrCamera != null)
        {
            PositionCanvasInFrontOfPlayer();
        }
        else
        {
            Debug.LogError("XR Camera not found!");
        }
    }

    void PositionCanvasInFrontOfPlayer()
    {
        if (worldSpaceCanvas != null)
        {
            // Canvas 위치 설정
            transform.position = xrCamera.transform.position +
                               xrCamera.transform.forward * 1f + // 1 meter forward
                               Vector3.up * -0.3f; // Slightly below eye level

            // Canvas가 카메라를 향하도록 회전 (수정된 부분)
            transform.rotation = xrCamera.transform.rotation;
            // Y축으로 180도 회전하여 텍스트가 올바른 방향을 향하도록
            transform.Rotate(0, 180, 0, Space.Self);

            // Canvas 크기 조정
            transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // RectTransform 크기 설정
            RectTransform rectTransform = worldSpaceCanvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(1000f, 800f);
            }
        }
    }
}