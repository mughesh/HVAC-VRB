using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VRTrainingKit.Utilities
{
    /// <summary>
    /// Repositions objects to their target socket positions at start.
    /// Useful for initializing valve caps or other objects that should start snapped in sockets.
    /// </summary>
    public class SnapObjectsAtStart : MonoBehaviour
    {
        [System.Serializable]
        public class SnapPair
        {
            [Tooltip("The object to reposition (e.g., valve cap)")]
            public GameObject objectToSnap;

            [Tooltip("The target socket position")]
            public GameObject targetSocket;
        }

        [Header("Snap Configuration")]
        [Tooltip("List of objects and their target sockets")]
        public List<SnapPair> snapPairs = new List<SnapPair>();

        [Header("Timing Settings")]
        [Tooltip("Delay in seconds before repositioning (allows physics to settle)")]
        [Range(0f, 2f)]
        public float delayBeforeSnap = 0.1f;

        [Header("Position Settings")]
        [Tooltip("Offset from socket position (useful for fine-tuning)")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("Match socket rotation as well")]
        public bool matchRotation = true;

        private void Start()
        {
            StartCoroutine(RepositionObjectsAfterDelay());
        }

        private IEnumerator RepositionObjectsAfterDelay()
        {
            // Wait for specified delay to let physics settle
            yield return new WaitForSeconds(delayBeforeSnap);

            // Reposition each snap pair
            foreach (var pair in snapPairs)
            {
                if (pair.objectToSnap != null && pair.targetSocket != null)
                {
                    RepositionObject(pair.objectToSnap, pair.targetSocket);
                }
                else
                {
                    Debug.LogWarning($"[SnapObjectsAtStart] Missing reference in snap pair on {gameObject.name}");
                }
            }
        }

        private void RepositionObject(GameObject obj, GameObject socket)
        {
            // Get the socket's position
            Vector3 targetPosition = socket.transform.position + positionOffset;

            // Disable physics temporarily if there's a Rigidbody
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            bool hadRigidbody = rb != null;
            bool wasKinematic = false;

            if (hadRigidbody)
            {
                wasKinematic = rb.isKinematic;
                rb.isKinematic = true; // Temporarily make kinematic to avoid physics interference
            }

            // Reposition the object
            obj.transform.position = targetPosition;

            if (matchRotation)
            {
                obj.transform.rotation = socket.transform.rotation;
            }

            // Re-enable physics after a short delay
            if (hadRigidbody)
            {
                StartCoroutine(RestorePhysics(rb, wasKinematic));
            }

            Debug.Log($"[SnapObjectsAtStart] Repositioned {obj.name} to {socket.name}");
        }

        private IEnumerator RestorePhysics(Rigidbody rb, bool originalKinematicState)
        {
            yield return new WaitForFixedUpdate();

            if (rb != null)
            {
                rb.isKinematic = originalKinematicState;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure we have at least one snap pair in the inspector
            if (snapPairs.Count == 0)
            {
                snapPairs.Add(new SnapPair());
            }
        }

        private void OnDrawGizmos()
        {
            // Draw lines showing which objects will snap to which sockets
            if (snapPairs == null) return;

            foreach (var pair in snapPairs)
            {
                if (pair.objectToSnap != null && pair.targetSocket != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pair.objectToSnap.transform.position, pair.targetSocket.transform.position);

                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(pair.targetSocket.transform.position + positionOffset, 0.02f);
                }
            }
        }
        #endif
    }
}
