using System;
using System.Collections.Generic;

namespace PixelariaEngine;

public class PriorityQueue<T> where T : IComparable<T>
{
    private List<T> heap;
    private HashSet<T> itemsSet;

    public PriorityQueue(int capacity)
    {
        heap = new List<T>(capacity);
        itemsSet = new HashSet<T>();
    }

    public void Enqueue(T item)
    {
        heap.Add(item);
        itemsSet.Add(item);
        HeapifyUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Cannot dequeue from an empty heap.");

        T frontItem = heap[0];
        int lastIndex = heap.Count - 1;

        if (lastIndex > 0)
        {
            heap[0] = heap[lastIndex];
            heap.RemoveAt(lastIndex);
            itemsSet.Remove(frontItem);
            HeapifyDown(0);
        }
        else
        {
            // Only one item in the heap; remove it
            heap.RemoveAt(0);
            itemsSet.Remove(frontItem);
        }

        return frontItem;
    }

    public bool Contains(T item)
    {
        return itemsSet.Contains(item);
    }

    public int Count => heap.Count;

    private void HeapifyUp(int index)
    {
        T item = heap[index];
        while (index > 0)
        {
            int parentIndex = (index - 1) >> 1;
            T parentItem = heap[parentIndex];
            if (item.CompareTo(parentItem) >= 0)
                break;
            heap[index] = parentItem;
            index = parentIndex;
        }
        heap[index] = item;
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = heap.Count - 1;
        T item = heap[index];

        while (true)
        {
            int childIndex = (index << 1) + 1; // Left child index
            if (childIndex > lastIndex)
                break;

            int rightChild = childIndex + 1; // Right child index
            if (rightChild <= lastIndex && heap[rightChild].CompareTo(heap[childIndex]) < 0)
                childIndex = rightChild;

            if (heap[childIndex].CompareTo(item) >= 0)
                break;

            heap[index] = heap[childIndex];
            index = childIndex;
        }

        heap[index] = item;
    }
}