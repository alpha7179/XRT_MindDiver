using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    [Header("Video Players Settings (Order: 0=Front, 1=Left, 2=Right)")]
    // [변경됨] VideoPlayer도 배열로 묶어서 관리
    public VideoPlayer[] videoPlayers;

    [Header("Video Clips Settings (Order: 0=Front, 1=Left, 2=Right)")]
    // [변경됨] 개별 변수 대신 배열로 관리 (행렬/리스트 형식)
    public VideoClip[] introClips;
    public VideoClip[] outroClips;

    [Header("디버그 로그")]
    [SerializeField] private bool isDebugMode = true;

    private void Start()
    {
        // 플레이어 배열 유효성 검사
        if (!CheckArrayValid(videoPlayers, "Video Players")) return;

        // 1. DataManager에서 어떤 비디오 타입인지 확인
        DataManager.VideoType type = DataManager.Instance.currentVideoType;

        Log($"[VideoManager] Playing {type} Video (3-Screen Panoramic)...");

        // 2. 타입에 따라 알맞은 3개의 클립 세팅 및 재생
        // 배열 인덱스: 0=Front, 1=Left, 2=Right
        if (type == DataManager.VideoType.Intro)
        {
            if (CheckArrayValid(introClips, "Intro Clips"))
            {
                PlayPanoramicVideo(introClips);
            }
        }
        else // Outro
        {
            if (CheckArrayValid(outroClips, "Outro Clips"))
            {
                PlayPanoramicVideo(outroClips);
            }
        }

        // 3. 종료 이벤트 연결 (정면 스크린 [0] 기준)
        // 정면 화면이 존재할 때만 이벤트 연결
        if (videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached += OnVideoFinished;
        }
    }

    // 배열 유효성 검사 (최소 3개인지 확인)
    private bool CheckArrayValid(System.Array array, string arrayName)
    {
        if (array == null || array.Length < 3)
        {
            Log($"[VideoManager] Error: {arrayName} array must have at least 3 elements (Front, Left, Right).");

            // 안전 장치: 플레이어가 세팅되어 있다면 정면 화면 기준으로 종료 처리 시도
            if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
            {
                OnVideoFinished(videoPlayers[0]);
            }
            return false;
        }
        return true;
    }

    private void PlayPanoramicVideo(VideoClip[] clips)
    {
        // 3면 반복 처리 (0: Front, 1: Left, 2: Right)
        for (int i = 0; i < 3; i++)
        {
            // 플레이어와 클립이 모두 존재해야 재생
            if (videoPlayers[i] != null && clips[i] != null)
            {
                videoPlayers[i].clip = clips[i];

                // 사이드 화면(1, 2) 오디오 음소거 옵션 (필요시 주석 해제)
                // if (i > 0) videoPlayers[i].SetDirectAudioMute(0, true);

                videoPlayers[i].Play();
            }
        }

        // 정면(0) 플레이어가 없거나 클립이 없으면 강제 종료 (진행 막힘 방지)
        if (videoPlayers[0] == null || clips[0] == null)
        {
            OnVideoFinished(null);
        }
    }

    // 비디오 재생이 끝났을 때 호출됨
    private void OnVideoFinished(VideoPlayer vp)
    {
        Log("[VideoManager] Video Finished.");

        // 이벤트 중복 호출 방지를 위해 구독 해제 (정면 기준)
        if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached -= OnVideoFinished;
        }

        // 다음 씬 전환
        DataManager.VideoType type = DataManager.Instance.currentVideoType;

        if (type == DataManager.VideoType.Intro)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.CharacterSelect);
        }
        else if (type == DataManager.VideoType.Outro)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Result);
        }
    }

    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
}