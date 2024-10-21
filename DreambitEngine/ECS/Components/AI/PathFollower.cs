using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[Require(typeof(AStarPathfinder), typeof(Mover))]
public class PathFollower : Component
{
    private Mover _mover;
    private Queue<Node> _path = [];
    private AStarPathfinder _pathfinder;
    public bool IsSeeking;
    public float SeekSpeed { get; set; }
    public int PathLength => _path.Count;

    public override void OnAddedToEntity()
    {
        _pathfinder = Entity.GetComponent<AStarPathfinder>();
        _mover = Entity.GetComponent<Mover>();
    }

    public override void OnUpdate()
    {
        // Exit if we are not seeking
        if (!IsSeeking || _path.Count == 0) return;


        // peek the next node
        var nextNode = _path.Peek();

        //check distance to it
        if (ArrivedToNextNode(nextNode))
        {
            _path.Dequeue();

            if (!_path.TryPeek(out nextNode)) //try to get the next node
            {
                Stop();
                return; // return if no more nodes in path
            }
        }

        var currentPos = Transform.WorldPosition;
        var nextPos = new Vector3(nextNode.X, nextNode.Y, 0);
        var directionToNextNode = nextPos - currentPos;

        if (directionToNextNode.Length() > 0)
            directionToNextNode.Normalize();

        _mover.Velocity = directionToNextNode * SeekSpeed;
    }

    private bool ArrivedToNextNode(Node nextNode)
    {
        var currentPos = Transform.WorldPosition;
        var nextPos = new Vector3(nextNode.X, nextNode.Y, 0);

        var distanceToNextNode = (nextPos - currentPos).Length();

        return distanceToNextNode <= SeekSpeed * Time.DeltaTime;
    }

    public void Seek(Vector2 targetPosition)
    {
        _path = _pathfinder.FindPath(Transform.WorldPosToVec2, targetPosition);
        IsSeeking = true;
    }

    public void Stop()
    {
        IsSeeking = false;
        _path.Clear();
        _mover.Velocity = Vector3.Zero;
    }

    public void Pause()
    {
        IsSeeking = false;
        _mover.Velocity = Vector3.Zero;
    }

    public override void OnDebugDraw()
    {
        var pathList = _path.ToArray();
        for (var i = 0; i < pathList.Length - 1; i++)
        {
            var startNode = pathList[i];
            var endNode = pathList[i + 1];

            Core.SpriteBatch.DrawLine(startNode.ToVec2(), endNode.ToVec2(), Color.Wheat);
        }
    }
}