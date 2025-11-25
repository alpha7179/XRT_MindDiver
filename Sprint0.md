🚀 마인드 다이버 - Sprint 0: 개발 시작을 위한 첫 번째 작업

본 문서는 v0.4 (PC-First) 프로토타입 개발 계획서를 기반으로, 개발을 시작하기 위해 가장 먼저 수행해야 할 작업들을 체크리스트 형태로 정리한 것입니다.

Task 1: Unity 프로젝트 설정 및 씬 생성

모든 것이 담길 그릇을 만드는 단계입니다.

[ ] Unity 프로젝트 생성:

Unity Hub를 열어 새 프로젝트를 생성합니다.

템플릿은 3D (URP - Universal Render Pipeline) 를 추천합니다. (사이버펑크 스타일의 '빛' 효과(Bloom)에 유리합니다.)

프로젝트 이름을 MindDiver_Prototype 등으로 정합니다.

[ ] 5개 씬(Scene) 파일 생성:

Project 창 > Assets > Scenes 폴더를 생성합니다.

Scenes 폴더 안에 5개의 씬을 생성합니다. (File > New Scene)

MainMenuScene

VideoScene

CharacterSelectScene

GameStageScene

ResultScene

[ ] 빌드 세팅(Build Settings)에 씬 등록:

File > Build Settings (Ctrl+Shift+B)를 엽니다.

Scenes 폴더의 5개 씬을 모두 드래그 앤 드롭하여 Scenes In Build 목록에 추가합니다.

MainMenuScene을 0번 인덱스(가장 위)에 둡니다.

Task 2: 싱글톤(Singleton) 매니저 기본 구조 생성

게임의 모든 상태를 관리할 '뇌'를 준비하는 단계입니다. 이 매니저들은 씬이 바뀌어도 파괴되지 않고(DontDestroyOnLoad) 유지되어야 합니다.

[ ] MainMenuScene 열기:

MainMenuScene은 씬 0번으로, 모든 매니저를 로드하는 시작점이 됩니다.

[ ] 5개의 싱글톤 매니저 오브젝트 생성:

Hierarchy 창에 5개의 빈 게임 오브젝트(Empty GameObject)를 생성합니다.

@GameManager

@InputManager

@DataManager

@AudioManager

@CAVE_Display

Task 3: C# 스크립트 파일 생성

기술 계획서에 명시된 모든 스크립트의 '빈 껍데기' 파일을 미리 만들어 둡니다.

[ ] Scripts 폴더 생성:

Assets > Scripts 폴더를 생성합니다. (하위 폴더로 Managers, Gameplay 등을 만들어도 좋습니다.)

[ ] 12개의 C# 스크립트 파일 생성:

Scripts 폴더 내에 다음 12개의 C# 스크립트 파일을 생성합니다. (내용은 비워둡니다.)

GameManager.cs

GunnerInputManager.cs

PlayerShipController.cs

StageManager.cs

VideoPlayerManager.cs

CharacterSelectManager.cs

Target.cs

EnemyManager.cs

DataManager.cs

UIManager_MainMenu.cs

UIManager_GameStage.cs

UIManager_Result.cs

CAVE_DisplayManager.cs

AudioManager.cs

BossAI.cs (Phase 3용)

Task 4: 싱글톤 스크립트 연결 및 Awake() 구현

Task 2의 오브젝트와 Task 3의 스크립트를 연결합니다.

[ ] 5개 싱글톤 스크립트 연결:

MainMenuScene의 @GameManager 오브젝트에 GameManager.cs 스크립트를 드래그 앤 드롭합니다.

@InputManager → GunnerInputManager.cs

@DataManager → DataManager.cs

@AudioManager → AudioManager.cs

@CAVE_Display → CAVE_DisplayManager.cs

[ ] 싱글톤 Awake() 코드 구현:

방금 연결한 5개의 스크립트(GameManager.cs, GunnerInputManager.cs 등)를 엽니다.

각 스크립트에 static Instance 프로퍼티와 Awake() 함수를 추가하여 싱글톤 및 DontDestroyOnLoad를 구현합니다.

(복사-붙여넣기용 템플릿 - GameManager.cs 예시):

using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 추가

public class GameManager : MonoBehaviour
{
    // 1. 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    // 2. Awake() 함수 구현
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 있다면 이 오브젝트는 파괴
        }
    }

    // (이후 여기에 ChangeState, QuitGame 등 기술서의 함수들을 구현합니다)
}


[ ] DataManager.cs 에도 위 템플릿을 적용합니다.

[ ] GunnerInputManager.cs 에도 위 템플릿을 적용합니다.

[ ] AudioManager.cs 에도 위 템플릿을 적용합니다.

[ ] CAVE_DisplayManager.cs 에도 위 템플릿을 적용합니다.

Task 5: 3-카메라 및 디스플레이 설정

Sprint 0 - Task 2를 완료하여 3면 뷰의 기반을 다집니다.

[ ] 3-카메라 생성:

MainMenuScene의 @CAVE_Display 오브젝트의 자식(Child)으로 3대의 카메라(Camera)를 생성합니다.

cam_Front

cam_Left

cam_Right

[ ] CAVE_DisplayManager.cs 구현:

CAVE_DisplayManager.cs 스크립트를 엽니다.

기술서(2.11)대로 3대의 카메라를 연결할 public 변수와 Start() 함수를 구현합니다.

(구현 코드):

using UnityEngine;

public class CAVE_DisplayManager : MonoBehaviour
{
    // (싱글톤 Awake() 코드는 이미 Task 4에서 추가됨)

    [Header("CAVE Cameras")]
    public Camera cam_Front;
    public Camera cam_Left;
    public Camera cam_Right;

    private void Start()
    {
        // 3대의 카메라가 3개의 모니터(디스플레이)에 각각 출력되도록 설정
        // (개발 중에는 1개 모니터에서만 보일 수 있음)
        if (Display.displays.Length > 0)
            cam_Front.targetDisplay = 0; // 1번 디스플레이 (정면)

        if (Display.displays.Length > 1)
            cam_Left.targetDisplay = 1; // 2번 디스플레이 (좌측)

        if (Display.displays.Length > 2)
            cam_Right.targetDisplay = 2; // 3번 디스플레이 (우측)

        // (필요 시) 좌/우 카메라 각도 조절
        // cam_Left.transform.localRotation = Quaternion.Euler(0, -90, 0);
        // cam_Right.transform.localRotation = Quaternion.Euler(0, 90, 0);
    }
}


[ ] 카메라 연결:

@CAVE_Display 오브젝트의 인스펙터(Inspector) 창에서 cam_Front, cam_Left, cam_Right 슬롯에 3대의 카메라 오브젝트를 각각 드래그 앤 드롭합니다.