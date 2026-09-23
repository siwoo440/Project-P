# 2일차 — Unity 프로젝트 기본 구조

**날짜**: 2026-09-23
**단계**: 1단계 · 개발 기반 구축
**상태**: 완료

---

## 1. 목표

정식 Scene과 개발 전용 Scene을 생성하고, 빌드 목록과 폴더 구조를 1일차 기준선에 맞게 정리한다.

내용 없는 **빈 껍데기**를 만드는 날이다. 3~4일차 Scene 전환 코드를 작성할 때 이동 대상이 존재해야 하므로 미리 준비한다.

---

## 2. 작업 내용

### 2.1 Scene 생성

| 구분 | Scene | 경로 |
|---|---|---|
| 정식 | `00_Boot` | `Assets/_Project/Scenes/` |
| 정식 | `01_Title` | `Assets/_Project/Scenes/` |
| 정식 | `02_MainHub` | `Assets/_Project/Scenes/` |
| 정식 | `03_Gameplay` | `Assets/_Project/Scenes/` |
| 정식 | `90_Ending` | `Assets/_Project/Scenes/` |
| 개발 전용 | `Dev_PuzzleTest` | `Assets/_Project/Scenes/Dev/` |
| 개발 전용 | `Dev_BattleTest` | `Assets/_Project/Scenes/Dev/` |

모든 Scene은 2D URP 기본 구성(`Main Camera` + `Global Light 2D`)으로 통일했다. 각 Scene마다 고유 GUID를 발급한 `.meta` 파일을 함께 생성했다.

### 2.2 빌드 목록 등록

```
0  00_Boot      ← 게임 시작 Scene
1  01_Title
2  02_MainHub
3  03_Gameplay
4  90_Ending
```

- `00_Boot`을 인덱스 0에 둔다. 전역 시스템이 다른 모든 것보다 먼저 초기화되어야 하기 때문이다.
- `Dev_PuzzleTest`, `Dev_BattleTest`는 빌드 목록에서 제외했다. 에디터에서 직접 열어 테스트하는 데는 지장이 없다.

### 2.3 템플릿 정리

- Unity 템플릿 기본 `SampleScene`과 `Assets/Scenes` 폴더를 삭제했다.
- `Assets/Settings`, URP 전역 설정, `DefaultVolumeProfile`, `InputSystem_Actions`는 렌더링·입력 시스템이 참조하므로 유지했다.

### 2.4 폴더 구조

1일차에 선행 생성한 `Assets/_Project/` 구조를 그대로 사용한다. 우리 코드·에셋과 템플릿·외부 에셋을 분리하기 위함이다.

---

## 3. 검증

- Unity 에디터 임포트 로그에서 7개 Scene이 발급한 GUID 그대로 임포트된 것을 확인했다.
- 임포트 과정에서 Scene·스크립트 관련 에러는 발생하지 않았다.
- 빌드 목록에 정식 Scene 5개가 지정한 순서로 등록되어 있다.

---

## 4. 참고 사항

에디터 로그상 그래픽 장치가 **Microsoft Basic Render Driver**(소프트웨어 렌더링)로 동작하고 있다. D3D12 장치 생성에 실패한 뒤 D3D11 기본 드라이버로 폴백한 상태다.

프로젝트 파일 문제는 아니지만, 이 상태로는 에디터와 Play 모드가 느려진다. 그래픽 드라이버 설치·업데이트 여부를 점검할 필요가 있다.

---

## 5. 다음 단계

**3일차 — 00_Boot 및 전역 서비스**

`GameManager`, `SaveManager`, `AudioManager`, `DataManager`, `SceneFlow`를 구현하고 Scene이 바뀌어도 유지되는 전역 서비스 구조를 만든다. 초기화 성공 시 `01_Title`로 자동 전환되는 흐름까지 완성한다.
