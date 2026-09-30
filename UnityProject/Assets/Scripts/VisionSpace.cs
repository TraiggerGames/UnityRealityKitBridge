using System;
using System.Collections.Generic;
using UnityEngine;

// Persistent real-world anchors. Anchors live in ARKit, survive app restarts
// and are identified by a name you choose. Positions are Unity world
// coordinates, the same ones used by VisionObject and VisionInput.
public static class VisionAnchors
{
    public struct AnchorSample
    {
        public string Id;
        public string Phase; // added, updated, removed
        public bool Tracked; // false while ARKit cannot locate it
        public Vector3 Position;
        public Quaternion Rotation;
    }

    internal struct PendingCommand
    {
        public string Id, Op; // ensure, place, remove
        public int Sequence;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    // Raised for new, moved, lost and restored anchors. Anchors saved by an
    // earlier launch arrive here shortly after start ("added").
    public static event Action<AnchorSample> AnchorUpdated;

    private const int MaxPending = 64;
    private static readonly Dictionary<string, AnchorSample> known = new Dictionary<string, AnchorSample>();
    private static readonly List<PendingCommand> pending = new List<PendingCommand>();
    private static int sequence;

    internal static IReadOnlyList<PendingCommand> Pending => pending;

    public static IReadOnlyDictionary<string, AnchorSample> Known => known;
    public static bool TryGet(string id, out AnchorSample sample) => known.TryGetValue(id, out sample);

    // Creates the anchor at this pose only if no anchor with that name exists.
    // Use it for "place once, keep forever" content: on later launches the saved
    // anchor is kept and the pose passed here is ignored.
    public static void Ensure(string id, Vector3 position, Quaternion rotation) =>
        Enqueue("ensure", id, position, rotation);

    // Creates the anchor, replacing any existing one with the same name.
    public static void Place(string id, Vector3 position, Quaternion rotation) =>
        Enqueue("place", id, position, rotation);

    public static void Remove(string id) =>
        Enqueue("remove", id, Vector3.zero, Quaternion.identity);

    private static void Enqueue(string op, string id, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(id)) { Debug.LogWarning("Anchor ID must not be empty"); return; }
        pending.Add(new PendingCommand {
            Id = id, Op = op, Sequence = ++sequence, Position = position, Rotation = rotation
        });
        // Commands repeat in every frame until the host consumes them by sequence.
        if (pending.Count > MaxPending) pending.RemoveAt(0);
    }

    internal static void Receive(string id, string phase, bool tracked, Vector3 position, Quaternion rotation)
    {
        var sample = new AnchorSample {
            Id = id, Phase = phase, Tracked = tracked, Position = position, Rotation = rotation
        };
        if (phase == "removed") known.Remove(id); else known[id] = sample;
        AnchorUpdated?.Invoke(sample);
    }
}

public enum VisionMapMode
{
    Off,       // no room mesh (default, cheapest)
    Mesh,      // draw the room mesh as a wireframe
    Occlusion  // real surfaces hide virtual objects behind them
}

// Room mapping with ARKit scene reconstruction. The mesh stays in the host;
// Unity only chooses the mode. Use VisionInput.SurfaceUpdated for planes.
public static class VisionMap
{
    public static VisionMapMode Mode = VisionMapMode.Off;
    internal static string ModeName => Mode.ToString().ToLowerInvariant();
}
