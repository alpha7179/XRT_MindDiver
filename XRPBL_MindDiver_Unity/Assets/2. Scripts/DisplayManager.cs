using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3면 카메라 셋업 및 디스플레이 출력을 관리하는 매니저
/// </summary>
public class DisplayManager : MonoBehaviour
{
    public static DisplayManager Instance { get; private set; }

    [Header("Cameras (0:Front, 1:Left, 2:Right)")]
    public Camera cam_Front; // Main Cam 역할
    public Camera cam_Left;
    public Camera cam_Right;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 멀티 디스플레이 활성화 (실제 CAVE 환경용)
        // 에디터나 단일 모니터 테스트 시에는 1번 디스플레이에 분할 렌더링됨
        if (Display.displays.Length > 1) Display.displays[1].Activate();
        if (Display.displays.Length > 2) Display.displays[2].Activate();

        UpdateScreenLayout();
    }

    private void Update()
    {
        // 개발 중 해상도가 바뀔 수 있으므로 계속 업데이트 (빌드 후에는 최적화를 위해 뺄 수 있음)
        UpdateScreenLayout();
    }

    // ThreeScreenCamera.cs의 로직을 사용하여 3개 카메라의 Viewport Rect를 조정
    public void UpdateScreenLayout()
    {
        // 단일 모니터에서 32:9 비율 등으로 테스트할 때를 위한 분할 로직
        // 실제 CAVE 환경(3 모니터)에서는 각각 Target Display를 설정해야 하지만,
        // 여기서는 프로토타입(단일 와이드 화면) 기준으로 작성됨.

        // 카메라 3개 기본 Rect (왼쪽부터 순서대로 배치: Left -> Front -> Right)
        // 주의: 보내주신 코드는 0, 1, 2 순서인데 보통 왼쪽이 1번, 정면이 0번, 오른쪽이 2번이므로 순서 조정이 필요할 수 있음.
        // 여기서는 화면상 [Left][Front][Right] 순서로 배치한다고 가정.

        float aspect = (float)Screen.width / Screen.height;
        float targetAspect = 16f / 3f; // 16:9 화면 3개 가로 배치 시 (48:9 = 16:3)

        // Rect 계산 (uploaded code logic adaptation)
        float widthPerCam = 1f / 3f;

        // 카메라 Rect 설정 (0:Front가 가운데, 1:Left가 왼쪽, 2:Right가 오른쪽)
        cam_Left.rect = new Rect(0f, 0f, widthPerCam, 1f);
        cam_Front.rect = new Rect(widthPerCam, 0f, widthPerCam, 1f);
        cam_Right.rect = new Rect(widthPerCam * 2f, 0f, widthPerCam, 1f);

        // FOV 조정 (가로 FOV 90도 유지)
        float targetFOV = Camera.HorizontalToVerticalFieldOfView(90f, cam_Front.aspect);
        cam_Front.fieldOfView = targetFOV;
        cam_Left.fieldOfView = targetFOV;
        cam_Right.fieldOfView = targetFOV;
    }
}