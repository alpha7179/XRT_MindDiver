using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    [Header("Video Players (3 Screens)")]
    public VideoPlayer vp_Front;
    public VideoPlayer vp_Left;
    public VideoPlayer vp_Right;

    [Header("Video Clips")]
    public VideoClip introClip;
    public VideoClip outroClip;

    private void Start()
    {
        // 1. DataManager에서 어떤 비디오를 틀어야 하는지 확인
        DataManager.VideoType type = DataManager.Instance.currentVideoType;
        VideoClip clipToPlay = (type == DataManager.VideoType.Intro) ? introClip : outroClip;

        if (clipToPlay == null)
        {
            GameManager.Instance.Log("[VideoManager] Video Clip is missing!");
            OnVideoFinished(vp_Front); // 비디오가 없으면 바로 다음 단계로
            return;
        }

        GameManager.Instance.Log($"[VideoManager] Playing {type} Video...");

        // 2. 3면 스크린에 비디오 할당 및 재생
        PlayVideoOnAllScreens(clipToPlay);

        // 3. 종료 이벤트 연결 (정면 스크린 기준)
        vp_Front.loopPointReached += OnVideoFinished;
    }

    private void PlayVideoOnAllScreens(VideoClip clip)
    {
        // 정면
        vp_Front.clip = clip;
        vp_Front.Play();

        // 좌측
        if (vp_Left != null)
        {
            vp_Left.clip = clip;
            vp_Left.Play();
        }

        // 우측
        if (vp_Right != null)
        {
            vp_Right.clip = clip;
            vp_Right.Play();
        }
    }

    // 비디오 재생이 끝났을 때 호출됨
    private void OnVideoFinished(VideoPlayer vp)
    {
        GameManager.Instance.Log("[VideoManager] Video Finished.");

        // 현재 비디오 타입에 따라 다음 씬 결정
        DataManager.VideoType type = DataManager.Instance.currentVideoType;

        if (type == DataManager.VideoType.Intro)
        {
            // 인트로 끝 -> 캐릭터 선택
            GameManager.Instance.ChangeState(GameManager.GameState.CharacterSelect);
        }
        else if (type == DataManager.VideoType.Outro)
        {
            // 아웃트로 끝 -> 결과 화면
            GameManager.Instance.ChangeState(GameManager.GameState.Result);
        }
    }
}