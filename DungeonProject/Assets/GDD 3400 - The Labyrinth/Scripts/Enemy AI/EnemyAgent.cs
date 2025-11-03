using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

using UnityEngine.SceneManagement;
namespace GDD3400.Labyrinth
{
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAgent : MonoBehaviour
    {
        [SerializeField] private LevelManager _levelManager;
        bool isMovingToTarget = false;
        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }
        [SerializeField] private float _TurnRate = 10f;
        [SerializeField] private float _MaxSpeed = 2f;
        [SerializeField] private float _SightDistance = 25f;

        [SerializeField] private float _StoppingDistance = 0.4f;

        float _currentSpeed = 2f;

        float _MaxHostileSpeed = 5f;

        //change of speed every second
        float _speedChangeRate = 0.2f;


        [Tooltip("The distance to the destination before we start leaving the path")]
        [SerializeField] private float _LeavingPathDistance = 2f; // This should not be less than 1

        [Tooltip("The minimum distance to the destination before we start using the pathfinder")]
        [SerializeField] private float _MinimumPathDistance = 6f;

        bool valuableMissing = false;


        [SerializeField]
        List<GameObject> patrolPathObjects;

        int currentPathIndex = 0;


        public enum EnemSubState
        {
            patrol,
            valuable,
            chase,
            seek,

        }


        public enum EnemyStateM
        {
            passive,
            suspicious,
            hostile
        }

        //the speed at which the suspicion is increased
        public float suspicionTime = 0.8f;
        [SerializeField] private float _visionRange = 20f;
        [SerializeField] private float _visionAngle = 120f; // Field of view angle

        float smoothSpeed = 20f;
        private float _suspicionTimer = 0;
        private Transform _player;
        Collider[] targets = new Collider[10];
        public LayerMask targetLayer;

        //because why not
        float susValue = 0;

        //the needed suspision to enter suspicious state 
        float neededSus = 5;

        //the needed suspicion to enter hostile state. The timer will increase every 10 seconds when it is in suspicious state.
        float neededHostile = 25;

        float speedupTime = 1f;
        float elapsedSpeedupTIme = 0f;

        bool canSeePlayer;
        private int _currentNodeIndex;
        Vector3 lastKnownLocation = Vector3.zero;

        private Vector3 _velocity;
        private Vector3 _floatingTarget;
        private Vector3 _destinationTarget;
        List<PathNode> _path;

        private Rigidbody _rb;

        private LayerMask _wallLayer;

        private bool DEBUG_SHOW_PATH = true;

        float _repathCooldown = 0.5f;
        float _nextRepathTime = 0f;
        bool playerInRange = false;

        List<Vector3> valuableLocations = new List<Vector3>();

        //has the ai become aware of the player's prescence
        bool hasSeenPlayer;

        float elapsedSeenTime = 0f;

        //every 30 seconds if the ai has seen the player at all, the ai will go to where it last saw the player. If the last seen location is vector3.zero, the ai will go to where the ai first saw you
        float searchTimer = 30f;

        //the first location the player is seen at
        Vector3 firstSeenLocation;

        Vector3 targetDirection;
        EnemyStateM _currentMainState = EnemyStateM.passive;
        bool _pathActive= false;
        [SerializeField] 
        private float obstacleAvoidanceRadius;

        EnemSubState _currentSubState = EnemSubState.patrol;


        [SerializeField]
        private float _obstacleCheckDistance;

        private RaycastHit[] hits;
        public void Awake()
        {
            // Grab and store the rigidbody component
            _rb = GetComponent<Rigidbody>();

            // Grab and store the wall layer
            _wallLayer = LayerMask.GetMask("Walls");
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            hits = new RaycastHit[10];
        }

        public void Start()
        {
            // If we didn't manually set the level manager, find it
            if (_levelManager == null) _levelManager = FindAnyObjectByType<LevelManager>();

            // If we still don't have a level manager, throw an error
            if (_levelManager == null) Debug.LogError("Unable To Find Level Manager");

            FindValuableLocInScene();
            StartCoroutine(FovRoutine());
        }

        public void Update()
        {
            if (!_isActive) return;
           
            
            DecisionMaking();


        }

        private IEnumerator FovRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.2f);
            while (true)
            {
                yield return wait;
                FieldOfViewCheck();
            }
            }

        private void FieldOfViewCheck()
        {
            Collider[] rangeChecks = Physics.OverlapSphere(transform.position, _SightDistance, targetLayer);
            if (rangeChecks.Length > 0)
            {
                Transform target = rangeChecks[0].transform;
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, directionToTarget) < _visionAngle / 2)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, target.position);
                    if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, _wallLayer))
                    {
                        Debug.DrawLine(transform.position, _player.transform.position,Color.blue,2f);
                        canSeePlayer = true;
                    }
                    else
                    {
                        canSeePlayer = false;
                        
                    }
                }
                else
                {
                    canSeePlayer = false;
                }
            }
            else
            {
                if (canSeePlayer)
                {
                    canSeePlayer = false;
                }

            }
        }
            private void FindValuableLocInScene()
        {
            List<GameObject> valuables = new List<GameObject>(GameObject.FindGameObjectsWithTag("Collectable"));
            if (valuables.Count == 0)
            {
                Debug.Log("No valuables found. Critical error");
            }
            else
            {
                foreach (GameObject valuable in valuables)
                {
                    //add the location of the valuable to the array
                    valuableLocations.Add(valuable.transform.position);       
                }

            }
        }

        /// <summary>
        /// calculate the vector to avoid the obstacles
        /// </summary>
        void ObstacleAvoidance()
        {

        }
      

        private Vector3 HandleAvoidance()
        {
          Vector3 avoidance = Vector3.zero;
            float rayDistance = _obstacleCheckDistance > 0 ? _obstacleCheckDistance : 2f;
            float avoidStrength = 1.5f;

            Vector3 forwardDir = transform.forward;
            Vector3 rightDir = Quaternion.Euler(0,45f,0f) * forwardDir;
            Vector3 leftDir = Quaternion.Euler(0, -45f, 0f) * forwardDir;

            avoidance += CheckAvoidanceRay(forwardDir, rayDistance, avoidStrength);
            avoidance += CheckAvoidanceRay(rightDir, rayDistance, avoidStrength);
            avoidance += CheckAvoidanceRay(leftDir, rayDistance, avoidStrength);
            if(avoidance.sqrMagnitude > 0.001f)
            {
                return avoidance.normalized;
            }
            return Vector3.zero;
        }


        Vector3 CheckAvoidanceRay(Vector3 dir, float rayDistance, float strength)
        {
            Vector3 result = Vector3.zero;
            Debug.DrawRay(transform.position, dir.normalized * rayDistance, Color.yellow);
            if(Physics.Raycast(transform.position,dir.normalized,out RaycastHit hit, rayDistance))
            {
                float closeNess = 1f - (hit.distance / rayDistance);
                float weight = closeNess* strength;

                Vector3 push = hit.normal*weight;

                Vector3 tangent = Vector3.Cross(hit.normal,Vector3.up).normalized;

                if (Vector3.Dot(tangent, transform.forward) < 0f)
                {
                    tangent = -tangent;
                }
                push += tangent * (weight * 0.3f);
                result += push;
            }
            return result;
        }

        private void Perception()
        {
            canSeePlayer = false;
            int numTargets = Physics.OverlapSphereNonAlloc(transform.position, 25, targets, LayerMask.GetMask("Player"), QueryTriggerInteraction.Collide);
           
            if (numTargets > 0 && targets.Length > 0)
            {
                foreach (Collider target in targets)
                {
                    if (target.gameObject.CompareTag("Player"))
                    {
                        canSeePlayer = true;
                        return;
                    }
                }
            }



        }

        private void DecisionMaking()
        {
            switch (_currentMainState)
            {
                case EnemyStateM.passive:
                    PassiveBehavior();
                    break;
                case EnemyStateM.suspicious:
                    SuspiciousBehavior();
                    break;

                case EnemyStateM.hostile:
                    HostileBehavior();

                    break;
            }
            PathFollowing();


        }

        private void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.tag == "Player")
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void HostileBehavior()
        {
            //the ai will always know where the player is now and if the ai "sees" the player, it will gradually increase in speed. 
            if (canSeePlayer)
            {
                if (_currentSpeed < _MaxHostileSpeed)
                {
                    if (elapsedSpeedupTIme < speedupTime)
                    {
                        elapsedSpeedupTIme += Time.deltaTime;
                    }
                    else
                    {
                        _currentSpeed += _speedChangeRate;
                        elapsedSpeedupTIme = 0;
                    }
                }
            }
            else
            {
                if (_currentSpeed > _MaxSpeed)
                {
                    if (elapsedSpeedupTIme < speedupTime)
                    {
                        elapsedSpeedupTIme += Time.deltaTime;
                    }
                    else
                    {
                        _currentSpeed -= _speedChangeRate;
                    }
                }
                else
                {
                    _currentSpeed = _MaxSpeed;
                    elapsedSpeedupTIme = 0;
                }
                
            }

            //follow the player no matter whether or not they can see them or not
            if (!_pathActive || Time.deltaTime >= _nextRepathTime || Vector3.Distance(_destinationTarget, _player.position) > 2f)
            {
                _pathActive = true;
                _nextRepathTime = Time.deltaTime + _repathCooldown;
                SetDestinationTarget(_player.transform.position);
            }
          
        }
        /// <summary>
        /// unlike passive, the suspicion value will never reset once he enters this state.
        /// </summary>
        private void SuspiciousBehavior()
        {
            //if we are far enough from the player and we can still see them, stop a bit away from the player.
            if (canSeePlayer)
            {
                lastKnownLocation = _player.transform.position;
                if (!_pathActive || Time.deltaTime >= _nextRepathTime || Vector3.Distance(_destinationTarget, _player.position) > 2f)
                {
                   
                    SetDestinationTarget(_player.transform.position);
                    _pathActive = true;
                    _nextRepathTime = Time.deltaTime + _repathCooldown;
                }
                if (susValue <= neededHostile)
                {
                    susValue += Time.deltaTime * suspicionTime;
                }
                else
                {
                    susValue = neededHostile;
                    isMovingToTarget = false;
                    _currentMainState = EnemyStateM.hostile;
                    return;
                }
            }
            else
            {
                //we can't see the player, so we go back to patroling. The route will take the AI to all the valuables. Regardless if they exist or not.
                //if the AI gets to the location of the valuable and it is not there,
                //the AI will permanently enter the hostile phase.
                if (!isMovingToTarget)
                {
                    DiscoverMissingValuables();
                    if (valuableMissing)
                    {
                        Debug.Log("enemy became hostile because there was a valuable missing!");
                        _currentMainState = EnemyStateM.hostile;
                        return;
                    }
                    else
                    {
                        if (lastKnownLocation != Vector3.zero)
                        {
                            Debug.Log("going to last known location!");
                            SetDestinationTarget(lastKnownLocation);
                            isMovingToTarget = false;
                        }
                        else
                        {
                            Debug.Log("Going to patrol valuable locations");
                            Vector3 randomPatrol = valuableLocations[UnityEngine.Random.Range(0,valuableLocations.Count-1)];
                            SetDestinationTarget(randomPatrol);
                            isMovingToTarget = false;
                        }
                        
                      
                    }

                }
              
              


            }


        }

        private void PassiveBehavior()
        {

            //if we have seen the player at all, then we start a timer
            if (hasSeenPlayer)
            {
                if (elapsedSeenTime < searchTimer)
                {
                    Debug.Log(elapsedSeenTime.ToString());
                    elapsedSeenTime += Time.deltaTime;
                }
                else
                {
                    //if we have a last known location, then we go to it, otherwise it goes to the first seen location
                    if (lastKnownLocation != Vector3.zero)
                    {
                        SetDestinationTarget(lastKnownLocation);
                    }
                    else
                    {
                        SetDestinationTarget(firstSeenLocation);
                    }
                    isMovingToTarget = false;
                    elapsedSeenTime = 0;
                }
            }else if (hasSeenPlayer && isMovingToTarget)
            {
                if (elapsedSeenTime > 0)
                {
                    elapsedSeenTime = 0f;
                }
            }

            if (canSeePlayer)
            {
                lastKnownLocation = _player.transform.position;
                if (!hasSeenPlayer)
                {
                    //if the player has been seen at all
                    hasSeenPlayer = true;
                    Debug.Log("Seen player for the first time"); 
                    firstSeenLocation = _player.transform.position;
                   
                }
                // Increase suspicion over time
                susValue += Time.deltaTime *suspicionTime;
                
                if (susValue >= neededSus)
                {
                    Debug.Log("Enemy has become suspicious of the player");
                    _currentMainState = EnemyStateM.suspicious;
                    susValue = neededSus;
                    return;
                }
            }
            else
            {
                //slowly decrease suspicion if player not seen
                susValue -= Time.deltaTime / (suspicionTime * 2f);
                susValue = Mathf.Clamp(susValue, 0, neededSus);

                if (currentPathIndex < patrolPathObjects.Count)
                {
                    if (!isMovingToTarget)
                    {
                        SetDestinationTarget(patrolPathObjects[currentPathIndex].transform.position);
                        currentPathIndex++;
                        isMovingToTarget= true;
                    }
                }
                else if(!isMovingToTarget)
                {
                    currentPathIndex = 0;
                    
                }
            }
        
        }

        private GameObject ChooseRandomNode()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 100);
            List<GameObject> nodesInRange = new List<GameObject>();
            foreach (Collider hit in hits)
            {
                if (hit.gameObject.name == "PathNode" || hit.gameObject.name == "ExitNode")
                {
                    nodesInRange.Add(hit.gameObject);
                }
            }
            if (nodesInRange.Count == 0) return null;
            Debug.Log("found at least one node in range");
            return nodesInRange[UnityEngine.Random.Range(0, nodesInRange.Count - 1)];

        }

        #region Path Following

        private void PathFollowing()
        {
            if (_path == null || _path.Count == 0) return;

            // Make sure _currentNodeIndex is valid
            _currentNodeIndex = Mathf.Clamp(_currentNodeIndex, 0, _path.Count - 1);

            PathNode targetNode = _path[_currentNodeIndex];
            float distanceToNode = Vector3.Distance(transform.position, targetNode.transform.position);

            // If close enough, move to next node
            if (distanceToNode < _StoppingDistance)
            {
                _currentNodeIndex++;

                if (_currentNodeIndex >= _path.Count)
                {
                    // Done
                    _path = null;
                    _pathActive = false;
                    lastKnownLocation = Vector3.zero;
                    isMovingToTarget = false;
                 
                    return;
                }

                targetNode = _path[_currentNodeIndex];
            }

            // Keep updating the floating target
            _floatingTarget = targetNode.transform.position;
        }

        private void DiscoverMissingValuables()
        {
            GameObject firstFoundValuable = CheckIfValuablesInRange();
            if (firstFoundValuable == null)
            {
                Debug.Log("the valuable is missing!!!!");
                valuableMissing = true;
                return;
            }

        }

        GameObject CheckIfValuablesInRange()
        {
            Collider[] valuables = new Collider[10];
            int valuablesCount = Physics.OverlapSphereNonAlloc(transform.position, _SightDistance, valuables);

            if (valuablesCount > 0)
            {
                return valuables[0].gameObject;
            }
            return null;
        }

        // Public method to set the destination target
        public void SetDestinationTarget(Vector3 destination)
        {
           //storing the destination in a member variable
           _destinationTarget = destination;

            //if the straight line distance is greater than the minumum , lets do pathfinding
            if (Vector3.Distance(transform.position, _destinationTarget) > _MinimumPathDistance)
            {
                PathNode startNode = _levelManager.GetNode(transform.position);
                PathNode endNode = _levelManager.GetNode(destination);


              
                //we couln't find a node close enough
                if (startNode == null || endNode==null) return;

                _path = Pathfinder.FindPath(startNode, endNode);

                StartCoroutine(DrawPathDebugLines(_path));
            }
            // othewise move directly to destination
            else
            {
                _floatingTarget  = destination;
                
            }
            

        }

        // Get the closest node to the player's current position
        private int GetClosestNode()
        {
            int closestNodeIndex = 0;
            float closestDistance = float.MaxValue;
            if(_path!= null)
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

        #endregion

        #region Action
        private void FixedUpdate()
        {
            Vector3 avoidDir = HandleAvoidance();

            Vector3 pathDir = (transform.position-_destinationTarget).normalized;
            //flatening the direction in the y so the ai does not sink into the ground
            Vector3 flatDir = new Vector3(pathDir.x,0,pathDir.z);

            if (avoidDir != Vector3.zero)
            {
                flatDir = Vector3.Lerp(flatDir, avoidDir, Time.fixedDeltaTime * 3.0f);
            }

            _velocity = flatDir * _currentSpeed;
            _rb.linearVelocity = _velocity;
            // Smooth rotation toward movement direction (only rotate if actually moving)
            if (_velocity.sqrMagnitude > 0.05f)
            {
                // Flatten rotation direction to prevent tilting
                Vector3 flatDirection = _velocity;
                flatDirection.y = 0f;

                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
                }
            }
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
