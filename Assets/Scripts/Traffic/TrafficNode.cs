using UnityEngine;

namespace FriendOfOurs.Traffic
{
    public enum TrafficPort
    {
        North,
        East,
        South,
        West
    }

    [System.Serializable]
    public sealed class TrafficTurnPermissions
    {
        [SerializeField] private bool straight = true;
        [SerializeField] private bool left = true;
        [SerializeField] private bool right = true;

        public bool Allows(TrafficTurnType turn)
        {
            switch (turn)
            {
                case TrafficTurnType.Left: return left;
                case TrafficTurnType.Right: return right;
                default: return straight;
            }
        }
    }

    /// <summary>
    /// Invisible decision point placed once at each intersection, merge, forced corner or road end.
    /// Enabled ports are lanes that may leave this node; incoming traffic is inferred from a neighbour's enabled port.
    /// </summary>
    public sealed class TrafficNode : MonoBehaviour
    {
        [Header("Outgoing road arms")]
        [SerializeField] private bool north = true;
        [SerializeField] private bool east = true;
        [SerializeField] private bool south = true;
        [SerializeField] private bool west = true;

        [Header("Turn permissions by incoming arm")]
        [SerializeField] private TrafficTurnPermissions northInbound = new TrafficTurnPermissions();
        [SerializeField] private TrafficTurnPermissions eastInbound = new TrafficTurnPermissions();
        [SerializeField] private TrafficTurnPermissions southInbound = new TrafficTurnPermissions();
        [SerializeField] private TrafficTurnPermissions westInbound = new TrafficTurnPermissions();

        public bool AllowsTurn(TrafficPort incomingPort, TrafficTurnType turn)
        {
            switch (turn)
            {
                default:
                    switch (incomingPort)
                    {
                        case TrafficPort.North: return northInbound.Allows(turn);
                        case TrafficPort.East: return eastInbound.Allows(turn);
                        case TrafficPort.South: return southInbound.Allows(turn);
                        default: return westInbound.Allows(turn);
                    }
            }
        }

        public bool IsOutgoingEnabled(TrafficPort port)
        {
            switch (port)
            {
                case TrafficPort.North: return north;
                case TrafficPort.East: return east;
                case TrafficPort.South: return south;
                default: return west;
            }
        }

        public Vector3 GetWorldDirection(TrafficPort port)
        {
            switch (port)
            {
                case TrafficPort.North: return transform.forward;
                case TrafficPort.East: return transform.right;
                case TrafficPort.South: return -transform.forward;
                default: return -transform.right;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.25f, 0.65f);

            foreach (TrafficPort port in System.Enum.GetValues(typeof(TrafficPort)))
            {
                if (!IsOutgoingEnabled(port))
                {
                    continue;
                }

                Vector3 direction = GetWorldDirection(port);
                Vector3 start = transform.position + Vector3.up * 0.25f;
                Gizmos.color = Color.green;
                Gizmos.DrawRay(start, direction * 2f);
                Gizmos.DrawSphere(start + direction * 2f, 0.14f);
            }
        }
    }
}
