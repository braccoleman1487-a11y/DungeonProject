using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GDD3400.Labyrinth
{
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAgent : MonoBehaviour
    {
        [SerializeField] private LevelManager _levelManager;

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }
        [SerializeField] private float _TurnRate = 10f;
        [SerializeField] private float _MaxSpeed = 5f;
        [SerializeField] private float _SightDistance = 25f;

        [SerializeField] private float _StoppingDistance = 1.5f;
        
        [Tooltip("The distance to the destination before we start leaving the path")]
        [SerializeField] private float _LeavingPathDistance = 2f; // This should not be less than 1

        [Tooltip("The minimum distance to the destination before we start using the pathfinder")]
        [SerializeField] private float _MinimumPathDistance = 6f;

       

        private Vector3 _velocity;
        private Vector3 _floatingTarget;
        private Vector3 _destinationTarget;
        List<PathNode> _path;

        private Rigidbody _rb;

        private LayerMask _wallLayer;

        private bool DEBUG_SHOW_PATH = true;


        public void Awake()
        {
            // Grab and store the rigidbody component
            _rb = GetComponent<Rigidbody>();

            // Grab and store the wall layer
            _wallLayer = LayerMask.GetMask("Walls");
        }

        public void Start()
        {
            // If we didn't manually set the level manager, find it
            if (_levelManager == null) _levelManager = FindAnyObjectByType<LevelManager>();

            // If we still don't have a level manager, throw an error
            if (_levelManager == null) Debug.LogError("Unable To Find Level Manager");
        }

        public void Update()
        {
            if (!_isActive) return;

            Perception();
            DecisionMaking();
        }

        private void Perception()
        {
            // TODO: Implement perception            
        }

        private void DecisionMaking()
        {
            
           
            if(_path!=null && _path.Count > 0)
            {
                if (Vector3.Distance(transform.position, _destinationTarget) < _LeavingPathDistance)
                {
                    _path = null;
                    _floatingTarget = _destinationTarget;
                }
                else
                {
                    PathFollowing();
                }
            }
        }

        #region Path Following

        // Perform path following
        private void PathFollowing()
        {
            int closestNodeIndex = GetClosestNode();
            int nextNodeIndex = closestNodeIndex + 1;

            PathNode targetNode = null;

            if(nextNodeIndex<_path.Count)
            {
                targetNode = _path[nextNodeIndex];
            }

            else
            {
                targetNode = _path[closestNodeIndex];
            }

            _floatingTarget = targetNode.transform.position;


        }

        // Public method to set the destination target
        public void SetDestinationTarget(Vector3 destination)
        {
           //storing the destination in a member variable
           _destinationTarget = destination;

            //if the straight line distance is greater than the minumum , lets do pathfinding
            if (Vector3.Distance(transform.position, destination) > _MinimumPathDistance)
            {
                PathNode startNode = _levelManager.GetNode(transform.position);
                PathNode endNode = _levelManager.GetNode(destination);


                Debug.Log("pathfinding");
                //we couln't find a node close enough
                if (startNode == null || endNode==null) return;

                _path = Pathfinder.FindPath(startNode, endNode);

                StartCoroutine(DrawPathDebugLines(_path));
            }
            // othewise move directly to destination
            else
            {
                _floatingTarget = destination;
            }
            

        }

        // Get the closest node to the player's current position
        private int GetClosestNode()
        {
            int closestNodeIndex = 0;
            float closestDistance = float.MaxValue;

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

        #endregion

        #region Action
        private void FixedUpdate()
        {
            if (!_isActive) return;


            Debug.DrawLine(this.transform.position, _floatingTarget, Color.green);

            // If we have a floating target and we are not close enough to it, move towards it
            if (_floatingTarget != Vector3.zero && Vector3.Distance(transform.position, _floatingTarget) > _StoppingDistance)
            {
                // Calculate the direction to the target position
                Vector3 direction = (_floatingTarget - transform.position).normalized;

                // Calculate the movement vector
                _velocity = direction * _MaxSpeed;                
            }

            // If we are close enough to the floating target, slow down
            else
            {
                _velocity *= .95f;
            }

            // Calculate the desired rotation towards the movement vector
            if (_velocity != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_velocity);

                // Smoothly rotate towards the target rotation based on the turn rate
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _TurnRate);
            }

            _rb.linearVelocity = _velocity;
        }
        #endregion

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
