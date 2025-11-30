🏃 Sprint 2: 상호작용 심화 및 게임 루프 완성

기간: Day 7 ~ Day 10
목표: Phase 4(기둥 잡기), Phase 5(구조물 오르기), Phase 6(탈출)을 구현하고, 인트로->게임->엔딩으로 이어지는 전체 루프를 완성한다.

1. 핵심 기능 구현 계획 (Core Features)

A. Phase 4: 기둥 잡기 (Hold Pillar)

목표: 인파에 밀려 넘어지지 않도록 기둥을 잡고 버티기.

메카닉:

플레이어의 손이 기둥(Pillar) 오브젝트의 Collider 안에 있어야 함.

컨트롤러의 Grip 버튼을 누르고 있어야 함.

위 두 조건이 3초 이상 유지되면 성공.

연출: 화면이 심하게 흔들리는 Camera Shake 효과를 주어 위기감을 조성하고, 성공 시 흔들림이 멈추며 안도감을 줍니다.

B. Phase 5: 구조물 오르기 (Climb Up)

목표: 바닥의 압사 위험을 피해 높은 곳으로 대피하기.

메카닉:

벽면에 부착된 손잡이(ClimbInteractable)를 Grip 버튼으로 잡음.

손을 아래로 당겨 플레이어의 몸(XR Origin)을 위로 끌어올림 (Gorilla Tag 방식 또는 사다리 방식).

플레이어의 Y축 높이가 지면보다 0.5m 이상 상승하고, 특정 손잡이를 2초 이상 잡고 있으면 성공.

구현 팁: XRI의 Climb Provider를 사용하는 것이 가장 안정적입니다. 하지만 이번에는 '특정 시간 유지'라는 조건이 있으므로, 이를 감지하는 커스텀 로직이 필요합니다.

C. Phase 6: 탈출 (Escape)

목표: 구조대에게 이동하여 생존 확인.

메카닉:

전방에 밝은 빛과 함께 경찰차/구조대 오브젝트 활성화.

플레이어가 EscapeZone 트리거에 진입하면 게임 클리어.

연출: 사이렌 소리, 구조대의 외침("여기로 오세요!"), 환호성 등으로 극적인 안도감을 연출합니다.

2. 상세 개발 명세서 (Implementation Spec)

📜 ClimbInteractable.cs (신규 작성)

기능: 잡을 수 있는 손잡이 객체에 부착.

역할: 플레이어가 잡았는지 여부(IsGrabbed)를 GameStepManager에게 전달.

설정: XR Grab Interactable을 상속받거나 컴포넌트로 사용하여 물리적 잡기 기능을 구현.

📜 GameStepManager3.cs (업데이트)

Phase 4 로직 추가:

ControllerInputManager의 Grip 상태와 HandTrigger의 충돌 여부를 체크.

CameraShake 코루틴 실행/중지 제어.

Phase 5 로직 추가:

플레이어의 transform.position.y 값을 실시간 체크.

ClimbInteractable이 잡힌 상태에서 타이머 증가 로직 구현.

3. UI/UX 및 데이터 연동

HUD 업데이트: Phase 4, 5 진행 시 상단 지시사항 텍스트 변경.

게이지 바: 기둥 잡기 및 구조물 오르기 시에도 원형 게이지가 차오르도록 연동.

Outtro 연결: Phase 6 완료 시 GameManager.TriggerGameClear() 호출 -> OuttroUIManager 활성화 -> 점수 및 별점 표시 확인.

4. Sprint 2 체크리스트 (Definition of Done)

[ ] 기둥 잡기: 기둥 근처에서 그립을 잡으면 화면 흔들림이 멈추고 게이지가 차오르는가?

[ ] 오르기: 손잡이를 잡고 몸을 위로 올릴 수 있는가? (Climbing Locomotion)

[ ] 높이 판정: 일정 높이 이상 올라가서 버티면 다음 단계로 넘어가는가?

[ ] 탈출 엔딩: 탈출구 진입 시 '성공' 메시지와 함께 결과창이 뜨는가?

[ ] 데이터 저장: 재시작했을 때 이전 플레이 기록(성공 횟수 등)이 반영되는가?