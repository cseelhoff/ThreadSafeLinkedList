using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ThreadSafeLinkedList
{
    public class ThreadSafeLinkedList<T> : List<T>
    {
#if DEBUG
        SpinLock spinLock = new SpinLock(true);
#else
        SpinLock spinLock = new SpinLock(false);
#endif

        public static bool warningIssuedIndex;
        public static bool warningIssuedRemove;
        private int itemCount;
        private ThreadSafeNode<T> firstNode;
        private ThreadSafeNode<T> lastNode;

        public ThreadSafeLinkedList(int capacity) : base(capacity)
        {
        }

        public ThreadSafeLinkedList()
        {
        }

        public new int Count
        {
            get
            {
                return itemCount;
            }
        }

        public bool IsReadOnly => false;

        [Obsolete("Using an index on ThreadSafeLinkedList will lead to errors")]
        public new T this[int index]
        {
            get
            {
                return getNodeAtIndex(index).value;
            }
            set
            {
                getNodeAtIndex(index).value = value;
            }
        }

        [Obsolete("Using an index on ThreadSafeLinkedList will lead to errors")]
        ThreadSafeNode<T> getNodeAtIndex(int index)
        {
            int i = 0;
            ThreadSafeNode<T> currentNode = firstNode;
            while (currentNode != null)
            {
                if (i == index)
                {
                    return currentNode;
                }
                i++;
                currentNode = currentNode.nextNode;
            }
            return lastNode;
            throw new ArgumentOutOfRangeException();
        }

        public void AddNode(ThreadSafeNode<T> threadSafeNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                if (firstNode == null)
                {
                    firstNode = threadSafeNode;
                }
                else
                {
                    threadSafeNode.previousNode = lastNode;
                    lastNode.nextNode = threadSafeNode;
                }
                lastNode = threadSafeNode;
                itemCount++;
            }
            finally
            {
                if (lockTaken) spinLock.Exit(false);
            }
        }
        public new void Add(T obj)
        {
            AddNode(new ThreadSafeNode<T>(obj));
        }
        public void InsertAfter(ThreadSafeNode<T> previousNode, ThreadSafeNode<T> newNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                newNode.nextNode = previousNode.nextNode;
                newNode.previousNode = previousNode;
                previousNode.nextNode = newNode;
                if (lastNode == previousNode)
                {
                    lastNode = newNode;
                }
                itemCount++;
            }
            finally
            {
                if (lockTaken) spinLock.Exit(false);
            }
        }
        public void InsertBefore(ThreadSafeNode<T> nextNode, ThreadSafeNode<T> newNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                newNode.nextNode = nextNode;
                newNode.previousNode = nextNode.previousNode;
                nextNode.previousNode = newNode;
                if (firstNode == nextNode)
                {
                    firstNode = newNode;
                }
                itemCount++;
            }
            finally
            {
                if (lockTaken) spinLock.Exit(false);
            }
        }
        [Obsolete("Using an index on ThreadSafeLinkedList will lead to errors")]
        public new void Insert(int index, T obj)
        {
            InsertNode(index, new ThreadSafeNode<T>(obj));
        }

        [Obsolete("Using an index on ThreadSafeLinkedList will lead to errors")]
        public void InsertNode(int index, ThreadSafeNode<T> newNode)
        {
            int i = 1; //not using 0, since it is doing insertAfter instead of insertBefore
            bool lockTaken = false;
            try
            {
                //ThreadSafeNode<T> insertAfterNode_nextNode = null;
                spinLock.Enter(ref lockTaken);
                ThreadSafeNode<T> insertAfterNode = firstNode;

                if (index <= 0)
                {
                    newNode.nextNode = firstNode;
                    firstNode = newNode;
                }
                else
                {
                    foreach (ThreadSafeNode<T> threadSafeNode in this)
                    {
                        insertAfterNode = threadSafeNode;
                        if (i == index)
                            break;
                        i++;
                    }
                    newNode.previousNode = insertAfterNode;
                    //if (insertAfterNode_nextNode == null)
                    //{
                    //    Error("Never null");
                    //}
                    newNode.nextNode = insertAfterNode.nextNode;
                    insertAfterNode.nextNode = newNode;

                }
                if (index >= itemCount)
                {
                    lastNode = newNode;
                }
                else
                {
                    newNode.nextNode.previousNode = newNode;
                }
                itemCount++;
            }
            finally
            {
                if (lockTaken) spinLock.Exit(false);
            }
        }
        public bool RemoveNode(ThreadSafeNode<T> threadSafeNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                itemCount--;
                if (threadSafeNode == firstNode)
                {
                    firstNode = threadSafeNode.nextNode;
                }
                if (threadSafeNode == lastNode)
                {
                    lastNode = threadSafeNode.previousNode;
                }
                if (threadSafeNode.previousNode != null)
                {
                    threadSafeNode.previousNode.nextNode = threadSafeNode.nextNode;
                }
                if (threadSafeNode.nextNode != null)
                {
                    threadSafeNode.nextNode.previousNode = threadSafeNode.previousNode;
                }
            }
            finally
            {
                if (lockTaken) spinLock.Exit(false);
            }
            return true;
        }

        [Obsolete("Using an index on ThreadSafeLinkedList will lead to errors")]
        public new int IndexOf(T item)
        {
            int i = 0;
            foreach (T threadSafeNodeValue in this)
            {
                if (threadSafeNodeValue.Equals(item))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        [Obsolete("Using an index on ThreadSafeLinkedList will lead to errors")]
        public new void RemoveAt(int index)
        {
            RemoveNode(getNodeAtIndex(index));
            return;
        }

        [Obsolete("Calling ThreadSafeLinkedList.Remove(T) is not optimal and will remove the first occurance. ThreadSafeLinkedList.RemoveNode(ThreadSafeNode<T>) is preferred.")]
        public new bool Remove(T item)
        {
            ThreadSafeNode<T> currentNode = firstNode;
            while (currentNode != null)
            {
                if (currentNode.value.Equals(item))
                {
                    itemCount--;
                    RemoveNode(currentNode);
                }
                currentNode = currentNode.nextNode;
            }
            return true;
        }

        public new int RemoveAll(Predicate<T> predicate)
        {
            int totalRemoved = 0;
            ThreadSafeNode<T> currentNode = firstNode;
            while (currentNode != null)
            {
                if (predicate(currentNode.value))
                {
                    RemoveNode(currentNode);
                    totalRemoved++;
                }
                currentNode = currentNode.nextNode;
            }
            return totalRemoved;
        }

        public new void Clear()
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                itemCount = 0;
                firstNode = null;
                lastNode = null;
            }
            finally
            {
                if (lockTaken) spinLock.Exit(false);
            }
        }

        public new bool Contains(T item)
        {
            foreach (T threadSafeNodeValue in this)
            {
                if (threadSafeNodeValue.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }
        public ThreadSafeNode<T> getFirstNodeContaining(T item)
        {
            ThreadSafeNode<T> currentNode = firstNode;
            while (currentNode != null)
            {
                if (currentNode.value.Equals(item))
                {
                    return currentNode;
                }
                currentNode = currentNode.nextNode;
            }
            return null;
        }

        public bool ContainsNode(ThreadSafeNode<T> node)
        {
            ThreadSafeNode<T> currentNode = firstNode;
            while (currentNode != null)
            {
                if (currentNode.Equals(node))
                {
                    return true;
                }
                currentNode = currentNode.nextNode;
            }
            return false;
        }

        public new void CopyTo(T[] array, int arrayIndex)
        {
            int i = 0;
            foreach (T threadSafeNodeValue in this)
            {
                if (i >= arrayIndex)
                    array[i - arrayIndex] = threadSafeNodeValue;
                i++;
            };
        }

        public List<T> ToList()
        {
            List<T> newList = new List<T>();
            foreach (T threadSafeNodeValue in this)
            {
                newList.Add(threadSafeNodeValue);
            }
            return newList;
        }

        public new IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public new class Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly ThreadSafeLinkedList<T> list;
            private ThreadSafeNode<T> currentNode;
            bool firstMove = false;

            public T Current
            {
                get
                {
                    if (currentNode == null) throw new InvalidOperationException("Enumerator Ended");

                    return currentNode.value;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (currentNode == null) throw new InvalidOperationException("Enumerator Ended");
                    return currentNode.value;
                }
            }

            public void Dispose()
            {
                currentNode = null;
            }

            public bool MoveNext()
            {
                if (firstMove)
                    currentNode = currentNode?.nextNode;
                firstMove = true;
                return currentNode != null;
            }

            public void Reset()
            {
                currentNode = list.firstNode;
                firstMove = false;
            }
            internal Enumerator(ThreadSafeLinkedList<T> threadSafeLinkedList)
            {
                list = threadSafeLinkedList;
                currentNode = threadSafeLinkedList.firstNode;
                firstMove = false;
            }
        }

    }

    public class ThreadSafeNode<T>
    {
        public ThreadSafeNode<T> previousNode;
        public ThreadSafeNode<T> nextNode;
        public T value;

        public T Obj { get; }
        public ThreadSafeNode<T> NextNode { get; }

        public ThreadSafeNode(T obj, ThreadSafeNode<T> previous)
        {
            value = obj;
            previousNode = previous;
        }
        public ThreadSafeNode(T obj)
        {
            value = obj;
        }

        public ThreadSafeNode(T obj, ThreadSafeNode<T> previous, ThreadSafeNode<T> next)
        {
            value = obj;
            previousNode = previous;
            nextNode = next;
        }
    }    
}
