using System;
using UnityEngine;

public enum VisionHand { Left, Right }

public struct VisionHandState
{
    public bool Tracked;
    public Vector3 IndexTip;
    public Vector3 ThumbTip;
    public Vector3 PinchPoint => (IndexTip + ThumbTip) * 0.5f;
    public float PinchDistance => Vector3.Distance(IndexTip, ThumbTip);
    public bool IsPinching;
}

// The simple, Unity-style way to read the headset's controls from C#.
// No setup and no Input System: call these from Update() like UnityEngine.Input.
//
//   if (VisionControls.GetPinchDown(VisionHand.Right)) Fire();
//   transform.position = VisionControls.Hand(VisionHand.Right).PinchPoint;
//
// Per-object interaction (tap, grab, touch) lives in VisionInteractable.
// All positions are Unity world coordinates in meters.
public static class VisionControls
{
    // Hysteresis avoids flicker: pinch starts below Start and ends above End.
    public static float PinchStartDistance = 0.025f;
    public static float PinchEndDistance = 0.04f;

    public static event Action<VisionHand> PinchStarted;
    public static event Action<VisionHand> PinchEnded;

    private static VisionHandState left, right;
    private static int leftDownFrame = -1, leftUpFrame = -1, rightDownFrame = -1, rightUpFrame = -1;

    static VisionControls() { VisionInput.HandUpdated += OnHand; }

    public static VisionHandState Hand(VisionHand hand) => hand == VisionHand.Left ? left : right;
    public static bool IsTracked(VisionHand hand) => Hand(hand).Tracked;
    public static bool GetPinch(VisionHand hand) => Hand(hand).IsPinching;
    // True only during the frame in which the pinch started / ended.
    public static bool GetPinchDown(VisionHand hand) =>
        (hand == VisionHand.Left ? leftDownFrame : rightDownFrame) == Time.frameCount;
    public static bool GetPinchUp(VisionHand hand) =>
        (hand == VisionHand.Left ? leftUpFrame : rightUpFrame) == Time.frameCount;

    // Touching the property once is enough to start listening (static constructor).
    public static void Init() { }

    private static void OnHand(VisionInput.HandSample sample)
    {
        VisionHand which = sample.Hand == "left" ? VisionHand.Left : VisionHand.Right;
        VisionHandState state = Hand(which);
        bool was = state.IsPinching;
        state.Tracked = sample.Tracked;
        state.IndexTip = sample.IndexTip;
        state.ThumbTip = sample.ThumbTip;
        float distance = state.PinchDistance;
        state.IsPinching = sample.Tracked &&
            (was ? distance < PinchEndDistance : distance < PinchStartDistance);
        if (which == VisionHand.Left) left = state; else right = state;
        if (state.IsPinching == was) return;
        if (state.IsPinching)
        {
            if (which == VisionHand.Left) leftDownFrame = Time.frameCount; else rightDownFrame = Time.frameCount;
            PinchStarted?.Invoke(which);
        }
        else
        {
            if (which == VisionHand.Left) leftUpFrame = Time.frameCount; else rightUpFrame = Time.frameCount;
            PinchEnded?.Invoke(which);
        }
    }
}
