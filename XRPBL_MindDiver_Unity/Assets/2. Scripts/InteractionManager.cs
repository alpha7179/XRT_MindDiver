using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

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

    private void Update()
    {
        // 프로토타입용: 마우스 클릭을 터치로 간주
        if (Input.GetMouseButtonDown(0))
        {
            ProcessInput(Input.mousePosition);
        }
    }

    private void ProcessInput(Vector2 inputPos)
    {
        // 1. 입력이 어느 화면(Screen ID)인지 판별
        // 화면 전체 너비의 1/3씩 나눔
        float screenWidth = Screen.width;
        int screenID = -1; // 0:Front, 1:Left, 2:Right

        if (inputPos.x < screenWidth * 0.333f)
            screenID = 1; // Left
        else if (inputPos.x > screenWidth * 0.666f)
            screenID = 2; // Right
        else
            screenID = 0; // Front

        GameManager.GameState state = GameManager.Instance.currentState;
        GameManager.Instance.Log($"[Interaction] Touch on Screen {screenID} at {inputPos}");

        // 2. 상태별 로직 분기
        // MainMenu, CharacterSelect, Result -> 정면(0)만 터치 가능
        if (state == GameManager.GameState.MainMenu ||
            state == GameManager.GameState.CharacterSelect ||
            state == GameManager.GameState.Result)
        {
            if (screenID == 0)
            {
                HandleRaycast(DisplayManager.Instance.cam_Front, inputPos);
            }
        }
        // GameStage -> 좌(1), 우(2)만 터치 가능 (정면은 파일럿 뷰)
        else if (state == GameManager.GameState.GameStage)
        {
            if (screenID == 1) HandleRaycast(DisplayManager.Instance.cam_Left, inputPos);
            else if (screenID == 2) HandleRaycast(DisplayManager.Instance.cam_Right, inputPos);
        }
    }

    private void HandleRaycast(Camera cam, Vector2 screenPos)
    {
        // 카메라의 Viewport에 맞게 Ray를 쏴야 함
        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            // 타겟(적, UI 버튼 등) 상호작용
            // Target 컴포넌트가 있다고 가정 (별도 구현 필요)
            // var target = hit.collider.GetComponent<Target>();
            // if (target != null) target.OnHit();

            GameManager.Instance.Log($"[Interaction] Hit Object: {hit.collider.name}");

            // TODO: 여기서 실제 Target.cs의 OnHit 호출 연결
            hit.collider.SendMessage("OnHit", SendMessageOptions.DontRequireReceiver);
        }
    }
}