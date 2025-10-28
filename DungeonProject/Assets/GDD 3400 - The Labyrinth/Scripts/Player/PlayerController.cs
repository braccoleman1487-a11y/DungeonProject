using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace  GDD3400.Labyrinth
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private float _MoveSpeed = 10;
        [SerializeField] private float _DashDistance = 2.5f;
        [SerializeField] private float _DashCooldown = 1.5f;


        [Tooltip("The minimum distance to the destination before we start using the pathfinder")]
        [SerializeField] private float _MinimumPathDistance = 6f;

        [Header("Connections")]
        [SerializeField] private Transform _GraphicsRoot;
        private Rigidbody _rigidbody;
        private InputAction _moveAction;
        private InputAction _dashAction;
        private Vector3 _moveVector;

        private bool _performDash;
        private bool _isDashing;

        List<PathNode> _path;

        [SerializeField]LevelManager _levManager;

        float minDistance = 6f;

        private Vector3 _targetLocation;
        Vector3 _floatingTarget;

        Vector3 _velocity;

        float __MaxSpeed = 5f;

        float _stoppingDistance = 1.5f;

        float __turnRate = 10f;

   
        private void Awake()
        {
            // Assign member variables
            _rigidbody = GetComponent<Rigidbody>();

            _moveAction = InputSystem.actions.FindAction("Click");
         
        }

        void PathFollowing()
        {
            int closestNodeIndex = GetClosestNode();
            if (closestNodeIndex != -1)
            {
                int nextNodeIndex = closestNodeIndex + 1;

                PathNode targetNode = null;

                if (nextNodeIndex < _path.Count)
                {
                    targetNode = _path[nextNodeIndex];
                }

                else
                {
                    targetNode = _path[closestNodeIndex];
                }

                _floatingTarget = targetNode.transform.position;
            }
           
        }




        // Get the closest node to the player's current position
        private int GetClosestNode()
        {
            int closestNodeIndex = 0;
            float closestDistance = float.MaxValue;
            if (_path != null)
            {
                for (int i = 0; i < _path.Count; i++)
                {
                    float distance = Vector3.Distance(transform.position, _path[i].transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNodeIndex = i;
                    }
                }
                return closestNodeIndex;
            }
            return -1;
        }
        private void Update()
        {
            PathFollowing();
        }
        private void Start()
        {
            // If we didn't manually set the level manager, find it
            if (_levManager == null) _levManager = FindAnyObjectByType<LevelManager>();
        }

        private void FixedUpdate()
        {
            if (_floatingTarget != Vector3.zero && Vector3.Distance(transform.position, _floatingTarget) > _stoppingDistance)
            {
                Vector3 direction = (_floatingTarget-transform.position).normalized;
                _velocity = direction * __MaxSpeed;
            }
            else
            {
                //othervise slow down
                _velocity *= 0.95f;
            }
            if(_velocity!= Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_velocity);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, __turnRate);
            }
            _rigidbody.linearVelocity = _velocity;
        }

        public void SetDestinationTarget(Vector3 targetLocation)
        {
            _floatingTarget = targetLocation;
            if (Vector3.Distance(transform.position, targetLocation) > minDistance)
            {
                PathNode startNode = _levManager.GetNode(transform.position);
                PathNode endNode = _levManager.GetNode(targetLocation);
                if (startNode == null || endNode == null)
                {
                    return;
                }
                _path = Pathfinder.FindPath(startNode, endNode);
                StartCoroutine(DrawPathDebugLines(_path));
            }
            else
            {
                _floatingTarget = targetLocation;
            }
        }

        private void PerformDash()
        {
            _performDash = true;
            _isDashing = true;

            // Call reset after the cooldown
            Invoke("ResetDash", _DashCooldown);
        }

        private void IsDashing()
        {
            // Make invulnurable when dashing
        }

        private void ResetDash()
        {
            _isDashing = false;
            _rigidbody.linearVelocity = _moveVector * _MoveSpeed;
        }


        /// <summary>
        /// Draws a visual representation of the path to the screen
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private IEnumerator DrawPathDebugLines(List<PathNode> path)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i].transform.position, path[i + 1].transform.position, Color.red, 3.5f);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
