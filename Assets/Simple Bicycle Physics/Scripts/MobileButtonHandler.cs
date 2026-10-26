using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Every control owns its pointers: lifting one finger cannot release another control.
public class MobileButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public int buttonPressed;
    public int direction = 1;
    public UnityEvent onPress = new UnityEvent();
    public bool IsPressed => pointers.Count > 0;
    public int CancellationVersion { get; private set; }
    private readonly HashSet<int> pointers = new HashSet<int>();
    private Graphic graphic;
    private bool visualPressed;
    private int screenWidth, screenHeight;

    private void Awake() { graphic = GetComponent<Graphic>(); }
    private void OnEnable() { screenWidth = Screen.width; screenHeight = Screen.height; ReleaseAll(); }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || Time.timeScale == 0 || eventData.button != PointerEventData.InputButton.Left) return;
        bool wasPressed = IsPressed;
        pointers.Add(eventData.pointerId);
        Refresh();
        if (!wasPressed) onPress.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Unity sends pointer-up for both Ended and Canceled touch phases.
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId == eventData.pointerId && touch.phase == TouchPhase.Canceled)
            {
                OnPointerExit(eventData);
                return;
            }
        }
        Release(eventData.pointerId);
    }
    // Sliding off cancels the hold. Sliding back on requires a new press.
    public void OnPointerExit(PointerEventData eventData)
    {
        if (pointers.Contains(eventData.pointerId) && pointers.Count == 1) CancellationVersion++;
        Release(eventData.pointerId);
    }

    private void Release(int pointerId)
    {
        if (!pointers.Remove(pointerId)) return;
        Refresh();
    }

    public void ReleaseAll()
    {
        if (IsPressed) CancellationVersion++;
        pointers.Clear();
        Refresh();
    }

    private void Refresh()
    {
        buttonPressed = IsPressed ? Mathf.Clamp(direction, -1, 1) : 0;
        if (visualPressed != IsPressed && graphic != null) graphic.SetVerticesDirty();
        visualPressed = IsPressed;
    }

    private void Update()
    {
        bool resized = Screen.width != screenWidth || Screen.height != screenHeight;
        screenWidth = Screen.width; screenHeight = Screen.height;
        if (IsPressed && (Time.timeScale == 0 || resized || EventSystem.current == null || !EventSystem.current.isActiveAndEnabled)) ReleaseAll();
    }

    private void OnDisable() { ReleaseAll(); }
    private void OnApplicationFocus(bool focused) { if (!focused) ReleaseAll(); }
    private void OnApplicationPause(bool paused) { if (paused) ReleaseAll(); }
}
