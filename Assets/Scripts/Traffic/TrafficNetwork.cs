using System;
using System.Collections.Generic;
using UnityEngine;

namespace FriendOfOurs.Traffic
{
    public enum TrafficTurnType { Straight, Left, Right }

    [Serializable]
    public struct TrafficEdge
    {
        [SerializeField] private int index;
        [SerializeField] private TrafficNode source;
        [SerializeField] private TrafficNode target;
        [SerializeField] private TrafficPort sourcePort;

        public int Index => index;
        public TrafficNode Source => source;
        public TrafficNode Target => target;
        public TrafficPort SourcePort => sourcePort;
        public bool IsValid => source != null && target != null;

        public TrafficEdge(int index, TrafficNode source, TrafficNode target, TrafficPort sourcePort)
        {
            this.index = index;
            this.source = source;
            this.target = target;
            this.sourcePort = sourcePort;
        }
    }

    public struct TrafficPath
    {
        private readonly Vector3 start;
        private readonly Vector3 control;
        private readonly Vector3 end;
        private readonly bool curved;

        private TrafficPath(Vector3 start, Vector3 control, Vector3 end, bool curved)
        {
            this.start = start;
            this.control = control;
            this.end = end;
            this.curved = curved;
        }

        public static TrafficPath Line(Vector3 start, Vector3 end) => new TrafficPath(start, Vector3.zero, end, false);
        public static TrafficPath Turn(Vector3 start, Vector3 center, Vector3 end) => new TrafficPath(start, center, end, true);
        public float Length => ApproximateLength(16);

        public Vector3 Evaluate(float t)
        {
            t = Mathf.Clamp01(t);
            if (!curved)
            {
                return Vector3.Lerp(start, end, t);
            }

            float inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

        public Vector3 GetDirection(float t)
        {
            Vector3 tangent = curved
                ? 2f * (1f - Mathf.Clamp01(t)) * (control - start) + 2f * Mathf.Clamp01(t) * (end - control)
                : end - start;
            tangent.y = 0f;
            return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector3.forward;
        }

        public float FindClosestT(Vector3 worldPosition, int samples = 24)
        {
            samples = Mathf.Max(2, samples);
            float bestT = 0f;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (samples - 1f);
                float distance = (Evaluate(t) - worldPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestT = t;
                }
            }

            return bestT;
        }

        private float ApproximateLength(int samples)
        {
            float length = 0f;
            Vector3 previous = Evaluate(0f);
            for (int i = 1; i < samples; i++)
            {
                Vector3 next = Evaluate(i / (samples - 1f));
                length += Vector3.Distance(previous, next);
                previous = next;
            }

            return length;
        }
    }

    /// <summary>Builds a directed road graph from invisible TrafficNode objects. No road meshes or splines are required.</summary>
    public sealed class TrafficNetwork : MonoBehaviour
    {
        [Header("Nodes")]
        [SerializeField] private TrafficNode[] nodes = Array.Empty<TrafficNode>();
        [SerializeField] private bool findNodesInChildren = true;

        [Header("Bake")]
        [SerializeField, Min(1f)] private float maxConnectionDistance = 85f;
        [SerializeField, Range(1f, 45f)] private float maxPortAngle = 14f;

        [Header("Lane shape")]
        [SerializeField, Min(0.1f)] private float laneOffset = 1.45f;
        [SerializeField, Min(0.1f)] private float intersectionRadius = 5f;
        [SerializeField, Min(0.1f)] private float speedLimit = 8f;

        [Header("Turn selection weights")]
        [SerializeField, Min(0f)] private float straightWeight = 0.6f;
        [SerializeField, Min(0f)] private float rightWeight = 0.25f;
        [SerializeField, Min(0f)] private float leftWeight = 0.15f;
        [SerializeField] private bool drawGraphGizmos = true;

        private readonly List<TrafficEdge> edges = new List<TrafficEdge>();
        public static TrafficNetwork Active { get; private set; }
        public int EdgeCount => edges.Count;
        public float SpeedLimit => speedLimit;

        private void Awake()
        {
            RebuildGraph();
        }

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
        }

        [ContextMenu("Rebuild Traffic Graph")]
        public void RebuildGraph()
        {
            if (findNodesInChildren)
            {
                nodes = GetComponentsInChildren<TrafficNode>(true);
            }

            edges.Clear();
            if (nodes == null) return;

            for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                TrafficNode source = nodes[nodeIndex];
                if (source == null) continue;

                foreach (TrafficPort port in Enum.GetValues(typeof(TrafficPort)))
                {
                    if (!source.IsOutgoingEnabled(port)) continue;
                    TrafficNode target = FindNeighbour(source, port);
                    if (target != null)
                    {
                        edges.Add(new TrafficEdge(edges.Count, source, target, port));
                    }
                }
            }
        }

        public bool IsValidEdgeIndex(int edgeIndex) => edgeIndex >= 0 && edgeIndex < edges.Count && edges[edgeIndex].IsValid;
        public TrafficEdge GetEdge(int edgeIndex) => IsValidEdgeIndex(edgeIndex) ? edges[edgeIndex] : default;

        public TrafficPath GetRoadPath(int edgeIndex)
        {
            if (!IsValidEdgeIndex(edgeIndex)) return TrafficPath.Line(transform.position, transform.position);
            TrafficEdge edge = edges[edgeIndex];
            Vector3 direction = GetFlatDirection(edge.Source.transform.position, edge.Target.transform.position);
            Vector3 right = Vector3.Cross(Vector3.up, direction) * laneOffset;
            float radius = Mathf.Min(intersectionRadius, Vector3.Distance(edge.Source.transform.position, edge.Target.transform.position) * 0.35f);
            Vector3 start = edge.Source.transform.position + direction * radius + right;
            Vector3 end = edge.Target.transform.position - direction * radius + right;
            return TrafficPath.Line(start, end);
        }

        public TrafficPath GetTurnPath(int incomingEdgeIndex, int outgoingEdgeIndex)
        {
            TrafficPath incoming = GetRoadPath(incomingEdgeIndex);
            TrafficPath outgoingPath = GetRoadPath(outgoingEdgeIndex);
            TrafficEdge outgoingEdge = GetEdge(outgoingEdgeIndex);
            return TrafficPath.Turn(incoming.Evaluate(1f), outgoingEdge.Source.transform.position, outgoingPath.Evaluate(0f));
        }

        public bool TrySelectNextEdge(int incomingEdgeIndex, out int nextEdgeIndex)
        {
            nextEdgeIndex = -1;
            if (!IsValidEdgeIndex(incomingEdgeIndex)) return false;

            TrafficEdge incoming = edges[incomingEdgeIndex];
            List<int> choices = new List<int>();
            List<float> weights = new List<float>();
            float totalWeight = 0f;

            for (int i = 0; i < edges.Count; i++)
            {
                TrafficEdge outgoing = edges[i];
                if (outgoing.Source != incoming.Target || outgoing.Target == incoming.Source) continue;
                TrafficTurnType turn = GetTurn(incoming, outgoing);
                TrafficPort incomingPort = GetIncomingPort(incoming);
                if (!incoming.Target.AllowsTurn(incomingPort, turn)) continue;

                float weight = turn == TrafficTurnType.Straight ? straightWeight : turn == TrafficTurnType.Right ? rightWeight : leftWeight;
                if (weight <= 0f) continue;
                choices.Add(i);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (choices.Count == 0) return false;
            float roll = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < choices.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                {
                    nextEdgeIndex = choices[i];
                    return true;
                }
            }

            nextEdgeIndex = choices[choices.Count - 1];
            return true;
        }

        public bool TryFindClosestEdge(Vector3 worldPosition, Vector3 forward, float maxDistance, out int edgeIndex)
        {
            edgeIndex = -1;
            float bestDistance = maxDistance * maxDistance;
            forward.y = 0f;
            forward.Normalize();
            for (int i = 0; i < edges.Count; i++)
            {
                TrafficPath path = GetRoadPath(i);
                float t = path.FindClosestT(worldPosition);
                Vector3 point = path.Evaluate(t);
                float distance = (point - worldPosition).sqrMagnitude;
                if (distance > bestDistance || (forward.sqrMagnitude > 0.01f && Vector3.Dot(forward, path.GetDirection(t)) < 0.1f)) continue;
                bestDistance = distance;
                edgeIndex = i;
            }

            return edgeIndex >= 0;
        }

        public bool TryGetSpawnPose(Vector3 playerPosition, Camera camera, float minDistance, float maxDistance, float cameraMargin,
            int attempts, out Pose pose, out int edgeIndex)
        {
            pose = default;
            edgeIndex = -1;
            float minSqr = minDistance * minDistance;
            float maxSqr = maxDistance * maxDistance;
            for (int i = 0; i < attempts && edges.Count > 0; i++)
            {
                int candidate = UnityEngine.Random.Range(0, edges.Count);
                TrafficPath path = GetRoadPath(candidate);
                float t = UnityEngine.Random.Range(0.18f, 0.82f);
                Vector3 position = path.Evaluate(t);
                float distance = (position - playerPosition).sqrMagnitude;
                if (distance < minSqr || distance > maxSqr || IsVisible(camera, position, cameraMargin)) continue;
                pose = new Pose(position, Quaternion.LookRotation(path.GetDirection(t), Vector3.up));
                edgeIndex = candidate;
                return true;
            }

            return false;
        }

        private TrafficNode FindNeighbour(TrafficNode source, TrafficPort port)
        {
            Vector3 direction = source.GetWorldDirection(port);
            direction.y = 0f;
            direction.Normalize();
            float minimumDot = Mathf.Cos(maxPortAngle * Mathf.Deg2Rad);
            TrafficNode best = null;
            float bestDistance = maxConnectionDistance;
            for (int i = 0; i < nodes.Length; i++)
            {
                TrafficNode candidate = nodes[i];
                if (candidate == null || candidate == source) continue;
                Vector3 offset = candidate.transform.position - source.transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance <= 0.01f || distance >= bestDistance || Vector3.Dot(direction, offset / distance) < minimumDot) continue;
                best = candidate;
                bestDistance = distance;
            }

            return best;
        }

        private static TrafficTurnType GetTurn(TrafficEdge incoming, TrafficEdge outgoing)
        {
            Vector3 incomingDirection = GetFlatDirection(incoming.Source.transform.position, incoming.Target.transform.position);
            Vector3 outgoingDirection = GetFlatDirection(outgoing.Source.transform.position, outgoing.Target.transform.position);
            float signedAngle = Vector3.SignedAngle(incomingDirection, outgoingDirection, Vector3.up);
            if (Mathf.Abs(signedAngle) < 35f) return TrafficTurnType.Straight;
            return signedAngle > 0f ? TrafficTurnType.Right : TrafficTurnType.Left;
        }

        private static TrafficPort GetIncomingPort(TrafficEdge incoming)
        {
            TrafficNode node = incoming.Target;
            Vector3 towardSource = GetFlatDirection(node.transform.position, incoming.Source.transform.position);
            TrafficPort bestPort = TrafficPort.North;
            float bestDot = float.NegativeInfinity;
            foreach (TrafficPort port in Enum.GetValues(typeof(TrafficPort)))
            {
                float dot = Vector3.Dot(node.GetWorldDirection(port), towardSource);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestPort = port;
                }
            }

            return bestPort;
        }

        private static Vector3 GetFlatDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private static bool IsVisible(Camera camera, Vector3 position, float margin)
        {
            if (camera == null) return false;
            Vector3 viewport = camera.WorldToViewportPoint(position);
            return viewport.z > 0f && viewport.x >= -margin && viewport.x <= 1f + margin && viewport.y >= -margin && viewport.y <= 1f + margin;
        }

        private void OnDrawGizmos()
        {
            if (!drawGraphGizmos) return;
            if (!Application.isPlaying) RebuildGraph();
            for (int i = 0; i < edges.Count; i++)
            {
                TrafficPath path = GetRoadPath(i);
                Gizmos.color = Color.green;
                Vector3 previous = path.Evaluate(0f);
                for (int sample = 1; sample < 12; sample++)
                {
                    Vector3 next = path.Evaluate(sample / 11f);
                    Gizmos.DrawLine(previous + Vector3.up * 0.18f, next + Vector3.up * 0.18f);
                    previous = next;
                }
                Gizmos.DrawRay(path.Evaluate(0.5f) + Vector3.up * 0.18f, path.GetDirection(0.5f) * 1.2f);
            }
        }
    }
}
