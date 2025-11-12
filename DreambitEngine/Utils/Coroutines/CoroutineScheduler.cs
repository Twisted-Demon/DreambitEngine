using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dreambit;

internal sealed class CoroutineScheduler : ICoroutineService
{
    private readonly Dictionary<int, Node> _byId = [];

    private readonly List<Node> _endOfFrameQueue = new(64);
    private readonly List<Node> _fixedQueue = new(64);
    private readonly Stack<Node> _pool = [];

    private Node _head;
    private int _idSeq = 1;

    private readonly ILogger _logger = new Logger<CoroutineScheduler>();

    public CoroutineHandle StartCoroutine(IEnumerator routine)
    {
        return StartCoroutineInternal(routine, null);
    }

    public void StopCoroutine(CoroutineHandle handle)
    {
        if (!handle.IsValid || !_byId.TryGetValue(handle.Id, out var node)) return;
        Remove(node);
    }

    public void StopAllCoroutines(object owner = null)
    {
        if (owner == null)
        {
            var cur = _head;
            while (cur != null)
            {
                var next = cur.Next;
                Recycle(cur);
                cur = next;
            }

            _head = null;
            _byId.Clear();
            _endOfFrameQueue.Clear();
            _fixedQueue.Clear();
        }
        else
        {
            var cur = _head;
            Node prev = null;
            while (cur != null)
            {
                var next = cur.Next;
                if (ReferenceEquals(cur.Owner, owner))
                {
                    Unlink(prev, cur);
                    Recycle(cur);
                }
                else
                {
                    prev = cur;
                }

                cur = next;
            }
        }
    }

    public bool IsRunning(CoroutineHandle h)
    {
        return h.IsValid && _byId.ContainsKey(h.Id);
    }

    public CoroutineHandle StartCoroutine(IEnumerator routine, object owner)
    {
        return StartCoroutineInternal(routine, owner);
    }

    public void Update()
    {
        var clock = CoroutineClock.Now();

        var cur = _head;
        Node prev = null;
        while (cur != null)
        {
            var next = cur.Next;
            if (cur.WaitingFixedUpdate)
            {
                prev = cur;
                cur = next;
                continue;
            }

            if (cur.WaitingEndOfFrame)
            {
                prev = cur;
                cur = next;
                continue;
            }

            if (!TickCoroutine(cur, clock))
            {
                Unlink(prev, cur);
                Recycle(cur);
            }
            else
            {
                prev = cur;
            }

            cur = next;
        }

        _endOfFrameQueue.Clear();
        cur = _head;
        while (cur != null)
        {
            if (cur.WaitingEndOfFrame) _endOfFrameQueue.Add(cur);
            cur = cur.Next;
        }

        foreach (var n in _endOfFrameQueue) n.WaitingEndOfFrame = false;
    }

    public void FixedUpdate()
    {
        var clock = CoroutineClock.NowFixed();

        _fixedQueue.Clear();
        var cur = _head;
        while (cur != null)
        {
            if (cur.WaitingFixedUpdate) _fixedQueue.Add(cur);
            cur = cur.Next;
        }

        foreach (var n in _fixedQueue)
        {
            n.WaitingFixedUpdate = false;
            TickCoroutine(n, clock);
        }
    }

    public void EndOfFrame()
    {
        var clock = CoroutineClock.Now(); // EoF usually shares the frame clock

        foreach (var n in _endOfFrameQueue) TickCoroutine(n, clock);
        _endOfFrameQueue.Clear();
    }

    private bool TickCoroutine(Node node, CoroutineClock clock)
    {
        try
        {
            if (node.CurrentYield != null)
            {
                if (node.CurrentYield is WaitForEndOfFrame eof)
                {
                    eof.queued = false;
                    node.WaitingEndOfFrame = true;
                    return true;
                }

                if (node.CurrentYield is WaitForFixedUpdate fx)
                {
                    fx.pending = false;
                    node.WaitingFixedUpdate = true;
                    return true;
                }

                if (node.CurrentYield.KeepWaiting(clock)) return true;
                node.CurrentYield = null;
            }

            var e = node.Enumerator ?? PopEnumerator(node);
            while (true)
            {
                if (e == null) return false;

                var moved = e.MoveNext();
                if (!moved)
                {
                    e = PopEnumerator(node);
                    node.Enumerator = e;
                    if (e == null) return false;
                    continue;
                }

                var yielded = e.Current;

                if (yielded == null)
                {
                    node.Enumerator = e;
                    return true;
                }

                if (yielded is IYieldInstruction yi)
                {
                    node.CurrentYield = yi;

                    if (yi is WaitForEndOfFrame eof2)
                    {
                        eof2.queued = true;
                        node.WaitingEndOfFrame = true;
                        return true;
                    }

                    if (yi is WaitForFixedUpdate fx2)
                    {
                        fx2.pending = true;
                        node.WaitingFixedUpdate = true;
                        return true;
                    }

                    return true;
                }

                if (yielded is IEnumerator nested)
                {
                    PushEnumerator(node, e);
                    e = nested;
                    continue;
                }

                if (yielded is Task task)
                {
                    node.CurrentYield = new WaitForTask(task);
                    return true;
                }

                node.Enumerator = e;
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message, node.ToString());
            return false;
        }
    }

    private CoroutineHandle StartCoroutineInternal(IEnumerator routine, object owner)
    {
        if (routine == null) return default;

        var node = _pool.Count > 0 ? _pool.Pop() : new Node();
        node.Reset();
        node.Id = ++_idSeq;
        node.Owner = owner;
        node.Enumerator = routine;

        node.Next = _head;
        _head = node;
        _byId[node.Id] = node;
        return new CoroutineHandle(node.Id);
    }

    private void Remove(Node node)
    {
        Node prev = null, cur = _head;
        while (cur != null)
        {
            if (ReferenceEquals(cur, node))
            {
                Unlink(prev, cur);
                break;
            }

            prev = cur;
            cur = cur.Next;
        }

        Recycle(node);
    }

    private void Unlink(Node prev, Node node)
    {
        if (prev == null) _head = node.Next;
        else prev.Next = node.Next;
        _byId.Remove(node.Id);
    }

    private void Recycle(Node node)
    {
        node.Reset();
        _pool.Push(node);
    }

    private static void PushEnumerator(Node n, IEnumerator e)
    {
        n.Stack.Push(e);
    }

    private static IEnumerator PopEnumerator(Node n)
    {
        return n.Stack.Count > 0 ? n.Stack.Pop() : null;
    }

    private sealed class Node
    {
        public readonly Stack<IEnumerator> Stack = new(8);
        public IYieldInstruction CurrentYield;
        public IEnumerator Enumerator;
        public int Id;
        public Node Next;
        public object Owner;
        public bool WaitingEndOfFrame;
        public bool WaitingFixedUpdate;

        public void Reset()
        {
            Id = 0;
            Owner = null;
            Enumerator = null;
            CurrentYield = null;
            Stack.Clear();
            WaitingEndOfFrame = false;
            WaitingFixedUpdate = false;
            Next = null;
        }
    }
}