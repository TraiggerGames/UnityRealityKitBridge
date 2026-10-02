using System;
using UnityEngine;
using UnityEngine.Events;

// Add next to a VisionObject to react to the headset's controls. Use the C#
// events from code or the UnityEvents in the Inspector; no Swift needed.
//
//   Tapped      look at it and pinch (the system tap)
//   GrabBegan / GrabMoved / GrabEnded   look, pinch and move your hand
//   TouchBegan / TouchEnded             poke it with a fingertip (no pinch)
//
// Tick Grabbable to have the object follow your hand while grabbed.
[RequireComponent(typeof(VisionObject))]
public sealed class VisionInteractable : MonoBehaviour
{
    [SerializeField] private bool grabbable = true;
    [SerializeField] private bool touchable = false;
    [Tooltip("If the object is anchored, grabbing it releases it from the anchor so it can move.")]
    [SerializeField] private bool detachAnchorOnGrab = true;

    public UnityEvent onTapped;
    public UnityEvent onGrabBegan;
    public UnityEvent onGrabEnded;
    public UnityEvent onTouchBegan;
    public UnityEvent onTouchEnded;

    public event Action Tapped;
    public event Action GrabBegan;
    public event Action<Vector3> GrabMoved; // new world position of the object
    public event Action GrabEnded;
    public event Action<VisionHand> TouchBegan;
    public event Action<VisionHand> TouchEnded;

    public bool Grabbable { get => grabbable; set => grabbable = value; }
    public bool IsGrabbed { get; private set; }
    public VisionObject Object { get; private set; }

    private VisionTouchable touch;
    private Vector3 grabOffset;

    private void Awake() { Object = GetComponent<VisionObject>(); }

    private void OnEnable()
    {
        Object.SelectionChanged += OnSelection;
        VisionInput.ObjectDragged += OnDrag;
        if (touchable)
        {
            touch = GetComponent<VisionTouchable>();
            if (touch == null) touch = gameObject.AddComponent<VisionTouchable>();
            touch.SelectOnTouch = false;
            touch.TouchBegan += OnTouchBegan;
            touch.TouchEnded += OnTouchEnded;
        }
    }

    private void OnDisable()
    {
        Object.SelectionChanged -= OnSelection;
        VisionInput.ObjectDragged -= OnDrag;
        if (touch != null)
        {
            touch.TouchBegan -= OnTouchBegan;
            touch.TouchEnded -= OnTouchEnded;
        }
        IsGrabbed = false;
    }

    private void OnSelection(bool selected) { Tapped?.Invoke(); onTapped.Invoke(); }

    private void OnTouchBegan(string hand) { TouchBegan?.Invoke(ToHand(hand)); onTouchBegan.Invoke(); }
    private void OnTouchEnded(string hand) { TouchEnded?.Invoke(ToHand(hand)); onTouchEnded.Invoke(); }
    private static VisionHand ToHand(string hand) => hand == "left" ? VisionHand.Left : VisionHand.Right;

    private void OnDrag(VisionInput.DragSample drag)
    {
        if (drag.ObjectId != Object.Id) return;
        if (drag.Phase == "changed")
        {
            if (!IsGrabbed)
            {
                IsGrabbed = true;
                if (grabbable && detachAnchorOnGrab) Object.DetachFromAnchor();
                grabOffset = transform.position - drag.Position;
                GrabBegan?.Invoke();
                onGrabBegan.Invoke();
            }
            if (grabbable) transform.position = drag.Position + grabOffset;
            GrabMoved?.Invoke(transform.position);
        }
        else if (drag.Phase == "ended" && IsGrabbed)
        {
            IsGrabbed = false;
            GrabEnded?.Invoke();
            onGrabEnded.Invoke();
        }
    }
}
