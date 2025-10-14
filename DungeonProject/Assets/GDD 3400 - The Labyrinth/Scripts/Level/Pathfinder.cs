using System.Collections.Generic;
using UnityEngine;

namespace GDD3400.Labyrinth
{
    public class Pathfinder
    {
        public static List<PathNode> FindPath(PathNode startNode, PathNode endNode)
        {
            //The list of the nodes we might want to take 
             List<PathNode> openSet = new List<PathNode>();

            //nodes we have already looked at
            List<PathNode> closedSet = new List<PathNode>();

            //saves path information back to start
            Dictionary<PathNode,PathNode> cameFromNode = new Dictionary<PathNode,PathNode>();   


            //keeping track of our costs as we go
            Dictionary<PathNode,float> costSoFar = new Dictionary<PathNode,float>();
            Dictionary<PathNode, float> costToEnd = new Dictionary<PathNode, float>();


            //initialize starting info
            openSet.Add(startNode);
            costSoFar.Add(startNode, 0);
            costToEnd.Add(startNode, Heuristic(startNode,endNode));

            while(openSet.Count > 0){
                //current gets the lowest cost node to the end
                PathNode current = GetLowestCost(openSet, costToEnd);
                
                //we have found the goal, break out and return our path
                if(current == endNode)
                {
                    return ReconstructPath(cameFromNode, current);
                }
                
                //move current node from open to closedSet
                openSet.Remove(current); 
                closedSet.Add(current);

                //evaluate each neighbor of the current node 
                foreach(var connection in current.Connections)
                {
                    PathNode neighbor = connection.Key;
                    //if we've already evaluated this neighbor, skip it
                    if (closedSet.Contains(neighbor)) continue;

                    
                    float tentativeCostFromStart = costSoFar[current] + connection.Value;

                    //add the neighbor if we haven't already looked at it
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);  
                    }
                    //otherwise if the cost from to start is greater (longer path), skip this neighbor
                    else if (tentativeCostFromStart >= costSoFar[neighbor]) continue;

                    //record best path and update costs
                    cameFromNode[neighbor] = current;
                    costSoFar[neighbor] = tentativeCostFromStart;
                    costToEnd[neighbor] = costSoFar[neighbor]+Heuristic(neighbor,endNode);

                }






            }






            return new List<PathNode>(); // Return an empty path if no path is found
        }

        // Calculate the heuristic cost from the start node to the end node, manhattan distance
        private static float Heuristic(PathNode startNode, PathNode endNode)
        {
            return Vector3.Distance(startNode.transform.position, endNode.transform.position);
        }

        // Get the node in the provided open set with the lowest cost (eg closest to the end node)
        private static PathNode GetLowestCost(List<PathNode> openSet, Dictionary<PathNode, float> costs)
        {
            PathNode lowest = openSet[0];
            float lowestCost = costs[lowest];

            foreach (var node in openSet)
            {
                float cost = costs[node];
                if (cost < lowestCost)
                {
                    lowestCost = cost;
                    lowest = node;
                }
            }

            return lowest;
        }

        // Reconstruct the path from the cameFrom map
        private static List<PathNode> ReconstructPath(Dictionary<PathNode, PathNode> cameFrom, PathNode current)
        {
            List<PathNode> totalPath = new List<PathNode> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Insert(0, current);
            }
            return totalPath;
        }
    }
}
