using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ETouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace RhythmArchive
{
    /// <summary>
    /// 터치(멀티터치), 마우스, 키보드(D F J K) 입력을 레인 단위로 읽는다.
    /// 새 Input System과 기존 Input Manager 둘 다 지원한다.
    /// </summary>
    public class InputReader
    {
        public const int LaneCount = 4;

        /// <summary>이번 프레임에 새로 눌린 레인.</summary>
        public readonly bool[] Pressed = new bool[LaneCount];

        /// <summary>현재 누르고 있는 레인(롱노트 판정용).</summary>
        public readonly bool[] Held = new bool[LaneCount];

        public bool AnyPressed { get; private set; }
        public bool BackPressed { get; private set; }

#if ENABLE_INPUT_SYSTEM
        static readonly Key[] LaneKeys = { Key.D, Key.F, Key.J, Key.K };
#else
        static readonly KeyCode[] LaneKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };
#endif

        public InputReader()
        {
#if ENABLE_INPUT_SYSTEM
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
#else
            Input.simulateMouseWithTouches = false;
            Input.multiTouchEnabled = true;
#endif
        }

        /// <param name="laneFromScreen">화면 좌표 → 레인 번호. 무시할 영역(버튼 등)이면 -1.</param>
        public void Poll(Func<Vector2, int> laneFromScreen)
        {
            Array.Clear(Pressed, 0, LaneCount);
            Array.Clear(Held, 0, LaneCount);
            AnyPressed = false;
            BackPressed = false;

#if ENABLE_INPUT_SYSTEM
            int touchCount = 0;
            foreach (var t in ETouch.activeTouches)
            {
                touchCount++;
                int lane = laneFromScreen(t.screenPosition);
                if (lane < 0 || lane >= LaneCount) continue;
                if (t.phase == ETouchPhase.Began) Pressed[lane] = true;
                if (t.phase != ETouchPhase.Ended && t.phase != ETouchPhase.Canceled) Held[lane] = true;
            }

            var mouse = Mouse.current;
            if (mouse != null && touchCount == 0 && !Application.isMobilePlatform)
            {
                var btn = mouse.leftButton;
                if (btn.isPressed || btn.wasPressedThisFrame)
                {
                    int lane = laneFromScreen(mouse.position.ReadValue());
                    if (lane >= 0 && lane < LaneCount)
                    {
                        if (btn.wasPressedThisFrame) Pressed[lane] = true;
                        if (btn.isPressed) Held[lane] = true;
                    }
                }
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                for (int i = 0; i < LaneCount; i++)
                {
                    var key = kb[LaneKeys[i]];
                    if (key.wasPressedThisFrame) Pressed[i] = true;
                    if (key.isPressed) Held[i] = true;
                }
                if (kb.spaceKey.wasPressedThisFrame) AnyPressed = true;
                // 안드로이드 뒤로가기 버튼은 Escape 키로 들어온다.
                BackPressed = kb.escapeKey.wasPressedThisFrame;
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                int lane = laneFromScreen(t.position);
                if (lane < 0 || lane >= LaneCount) continue;
                if (t.phase == TouchPhase.Began) Pressed[lane] = true;
                if (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled) Held[lane] = true;
            }

            if (Input.touchCount == 0 && !Application.isMobilePlatform && Input.mousePresent)
            {
                bool down = Input.GetMouseButtonDown(0);
                bool held = Input.GetMouseButton(0);
                if (down || held)
                {
                    int lane = laneFromScreen(Input.mousePosition);
                    if (lane >= 0 && lane < LaneCount)
                    {
                        if (down) Pressed[lane] = true;
                        if (held) Held[lane] = true;
                    }
                }
            }

            for (int i = 0; i < LaneCount; i++)
            {
                if (Input.GetKeyDown(LaneKeys[i])) Pressed[i] = true;
                if (Input.GetKey(LaneKeys[i])) Held[i] = true;
            }
            if (Input.GetKeyDown(KeyCode.Space)) AnyPressed = true;
            BackPressed = Input.GetKeyDown(KeyCode.Escape);
#endif
            for (int i = 0; i < LaneCount; i++)
                if (Pressed[i]) AnyPressed = true;
        }
    }
}
