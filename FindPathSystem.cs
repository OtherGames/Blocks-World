using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class FindPathSystem : MonoBehaviour//, IUpdateble
{
    public List<PathData> Pool = new List<PathData>();
    public List<Vector3> path = new();
    public PathData startNode = new PathData();
    public PathData goalNode = new PathData();
    public PathData node = new PathData();
    public List<PathData> storage = new List<PathData>();

    public List<PathData> reachable = new List<PathData>();
    public List<PathData> explored = new List<PathData>();
    public List<PathData> adjacents = new List<PathData>();
    public List<FindPathTask> findQueue = new List<FindPathTask>();

    public float delay = 5;
    public static FindPathSystem Instance;
    float timer;

    public Action<PathDataResult> onPathComplete;

    private void Awake()
    {
        Instance = this;
    }

    bool ebos = false;
    public void Update()
    {
        timer += Time.deltaTime;

        if (timer < delay)
            return;

        timer = 0;
        if (reachable.Count > 0)
        {
            ebos = true;
            //print(goal_node.point);
            // Choose some node we know how to reach.
            node = choose_node(reachable, goalNode);

            //WorldGenerator.Inst.SetBlockAndUpdateChunck(node.pos, 12);

            // If we just got to the goal node, build and return the path.
            if (node.pos == goalNode.pos)
            {
                Debug.Log($"!!!!!!  Путь найден  !!!!! {PathData.allCreated}");
                BuildPath(node);
                Pool.AddRange(storage);
                reachable.Clear();
                explored.Clear();
                ebos = false;
                onPathComplete?.Invoke
                (
                    new PathDataResult
                    (
                        startNode.pos,
                        goalNode.pos,
                        path
                    )
                );
            }
            else
            {
                // Don't repeat ourselves.
                //var removable = reachable.Find(n => n.pos == node.pos);
                //reachable.Remove(removable);
                reachable.Remove(node);
                explored.Add(node);
                //print(reachable.Count);


                // Where can we get from here that we haven't explored before?
                Get_adjacent_nodes(node);//.FindAll(n => explored.Find(p => p.pos == n.pos) == null); //get_adjacent_nodes(node) - explored
                List<PathData> new_reachable = new List<PathData>();
                foreach (var adj in adjacents)
                {
                    var length = explored.Count;
                    PathData found = null;
                    for (int i = 0; i < length; i++)
                    {
                        var item = explored[i];
                        if (item.pos == adj.pos)
                        {
                            found = item;
                            break;
                        }
                    }

                    if (found == null)
                    {
                        new_reachable.Add(adj);
                    }
                }

                foreach (var adjacent in new_reachable)
                {
                    // First time we see this node?
                    if (!reachable.Any(n => n.pos == adjacent.pos))
                    {
                        reachable.Add(adjacent);
                    }


                    // If this is a new path, or a shorter path than what we have, keep it.
                    if (node.cost + 1 < adjacent.cost)
                    {
                        adjacent.previous = node;
                        adjacent.cost = node.cost + 1;
                    }
                }

            }
        }
        else
        {
            if (path.Count == 0 && ebos)
            {
                Debug.Log($"пути нет {PathData.allCreated}");
                ebos = false;

                onPathComplete?.Invoke
                    (
                        new PathDataResult
                        (
                            startNode.pos,
                            goalNode.pos,
                            explored
                        )
                    );

                Pool.AddRange(storage);
                //adjacents.ForEach(a => PathData.ToPool(a));
                adjacents.Clear();
                explored.Clear();
            }

            if (findQueue.Count > 0)
            {
                InitTask();
            }
        }
    }

    List<PathData> Get_adjacent_nodes(PathData node)
    {
        adjacents.Clear();

        var generator = WorldGenerator.Inst;

        foreach (var offset in PathData.offsets)
        {
            var newPos = node.pos + offset;
            var blockID = generator.GetBlockID(newPos);

            if (blockID != 0)
            {
                var dist = Vector3.Distance(goalNode.pos, newPos);

                var found = storage.Find(n => n.pos == newPos);
                if (found == null)
                {
                    if (Pool.Count > 0)
                    {
                        var adjacent = Pool[0];
                        Pool.Remove(adjacent);
                        adjacent.Init(newPos, dist);
                        adjacents.Add(adjacent);
                        storage.Add(adjacent);
                    }
                    else
                    {
                        var newNode = new PathData(newPos, dist);
                        adjacents.Add(newNode);
                        storage.Add(newNode);
                    }
                }
                else
                {
                    found.distance = dist;
                    adjacents.Add(found);
                }
            }
        }

        return adjacents;
    }

    void BuildPath(PathData node)
    {
        while (node.previous != null)
        {
            WorldGenerator.Inst.SetBlockAndUpdateChunck(node.pos, 14);

            path.Add(node.pos);
            node = node.previous;
        }
    }

    public void Find(Vector3 globalBlockStartPos, Vector3 globalBlockEndPos)
    {
        var found = findQueue.Find(t => t.goal == globalBlockEndPos && t.start == globalBlockStartPos);
        if (found == null)
        {
            var newTask = new FindPathTask()
            {
                start = globalBlockStartPos,
                goal = globalBlockEndPos
            };

            findQueue.Add(newTask);

        }
        else
        {
            print("уже эст");
        }

    }

    void InitTask()
    {
        var nextTask = findQueue[0];
        findQueue.Remove(nextTask);
        var globalBlockStartPos = nextTask.start;
        var globalBlockEndPos = nextTask.goal;

        foreach (var data in Pool)
        {
            data.cost = PathData.initCost;
            data.previous = null;
            //WorldGenerator.Inst.SetBlockAndUpdateChunck(data.pos, 1);
            data.pos = default;
        }

        startNode.pos = globalBlockStartPos;
        startNode.distance = Vector3.Distance(globalBlockStartPos, globalBlockEndPos);
        startNode.cost = 0;
        startNode.previous = null;

        goalNode.pos = globalBlockEndPos;
        goalNode.distance = 0;
        goalNode.cost = PathData.initCost;
        goalNode.previous = null;

        reachable.Clear();
        explored.Clear();
        path.Clear();

        reachable.Add(startNode);

        storage.Clear();
    }

    PathData choose_node(List<PathData> reachable, PathData goal_node)
    {
        //PathPoint nearest = null;
        //float minDist = float.MaxValue;
        //foreach (var node in nodes)
        //{
        //    if(node.distance < minDist)
        //    {
        //        minDist = node.distance;
        //        nearest = node;
        //    }
        //}

        //return nearest;

        float min_cost = float.MaxValue;
        PathData best_node = null;

        foreach (var node in reachable)
        {
            var cost_start_to_node = node.cost;
            var cost_node_to_goal = Vector3.Distance(node.pos, goal_node.pos);
            var total_cost = cost_start_to_node + cost_node_to_goal;

            if (min_cost > total_cost)
            {
                min_cost = total_cost;
                best_node = node;
            }
        }

        return best_node;
    }

    [Serializable]
    public class FindPathTask
    {
        public Vector3 start;
        public Vector3 goal;
    }

    [Serializable]
    public class PathData
    {
        public Vector3 pos;
        public PathData previous;
        public float distance;
        public int cost = initCost;

        public static int allCreated = 0;

        public const int initCost = 88888888;

        //public static List<PathData> Pool = new List<PathData>();

        public static List<Vector3> offsets = new List<Vector3>()
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down
        };

        public PathData()
        {
            //allCreated++;
        }

        public PathData(Vector3 pos, float distance)
        {
            this.pos = pos;
            this.distance = distance;
            allCreated++;
        }

        public void Init(Vector3 pos, float distance)
        {
            this.pos = pos;
            this.distance = distance;
            this.cost = initCost;
            this.previous = null;
        }

        //public static void ToPool(PathData value)
        //{
        //    Pool.Add(value);
        //}

        //public static PathData Get(Vector3 pos, float dist)
        //{
        //    if (Pool.Count > 0)
        //    {
        //        var pathData = Pool[0];
        //        Pool.Remove(pathData);
        //        pathData.pos = pos;
        //        pathData.distance = dist;
        //        return pathData;
        //    }
        //    else
        //    {
        //        return new PathData(pos, dist);
        //    }
        //}
    }

    [Serializable]
    public class PathDataResult
    {
        public Vector3 start;
        public Vector3 goal;
        public List<Vector3> path;
        public bool found;
        public List<Vector3> explored;

        public PathDataResult(Vector3 start, Vector3 goal, List<Vector3> path)
        {
            this.start = start;
            this.goal = goal;
            this.path = path;
            this.found = true;
        }

        public PathDataResult(Vector3 start, Vector3 goal, List<PathData> explored)
        {
            this.start = start;
            this.goal = goal;
            this.found = false;
            this.explored = explored.Select(n => n.pos).ToList();
        }
    }
}
