using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

//https://stackoverflow.com/questions/2510975/c-sharp-object-pooling-pattern-implementation
public enum PoolAccessMode { FIFO, LIFO, Circular }
public enum PoolLoadingMode { Eager, Lazy, LazyExpanding }
public interface IDisposablePoolResource : IDisposable
{
    public IDisposablePool Pool { get; set; }
    public void Acquire();
}
public interface IDisposablePool : IDisposable
{
    public bool IsDisposed { get; }
    public void Release(IDisposablePoolResource resource);
}

public class Pool<T> : IDisposablePool where T : IDisposablePoolResource
{
    private bool isDisposed;
    private Func<Pool<T>, T> factory;
    private PoolLoadingMode loadingMode;
    private IItemStore itemStore;
    private int size;
    private int count;
    private System.Threading.Semaphore sync;

    public int Count => count;
    public int Max => size;

    public bool IsDisposed => isDisposed;

    public Pool(int _size, Func<Pool<T>, T> _factory, PoolLoadingMode _loadingMode = PoolLoadingMode.LazyExpanding, PoolAccessMode _accessMode = PoolAccessMode.Circular)
    {
        if (_size <= 0)
        {
            throw new ArgumentOutOfRangeException("_size", size, "Pool - Argument 'size' must be greater than zero.");
        }
        if (_factory == null)
            throw new ArgumentNullException("_factory");

        size = _size;
        factory = _factory;
        sync = new System.Threading.Semaphore(size, size);
        loadingMode = _loadingMode;
        itemStore = CreateItemStore(_accessMode, size);
        if (loadingMode == PoolLoadingMode.Eager)
        {
            PreloadItems();
        }
    }

    public T Acquire()
    {
        sync.WaitOne();
        T item;
        switch (loadingMode)
        {
            case PoolLoadingMode.Eager:
                item = AcquireEager();
                break;
            case PoolLoadingMode.Lazy:
                item = AcquireLazy();
                break;
            default:
                item = AcquireLazyExpanding();
                break;
        }
        item.Acquire();
        return item;
    }

    public void Release(IDisposablePoolResource resource)
    {
        if (resource.GetType() != typeof(T))
        {
            throw new ArgumentException("Pool.Release - resource is not of pool's type!");
        }
        Release((T)resource);
    }
    public void Release(T item)
    {
        lock (itemStore)
        {
            itemStore.Store(item);
        }
        sync.Release();
    }

    public void Dispose()
    {
        if (isDisposed)
            return;
        isDisposed = true;
        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
        {
            lock (itemStore)
            {
                while (itemStore.Count > 0)
                {
                    ((IDisposable)itemStore.Fetch()).Dispose();
                }
            }
        }
        sync.Close();
    }

    private void PreloadItems()
    {
        for (int i = 0; i < size; i++)
        {
            T item = factory(this);
            itemStore.Store(item);
        }
        count = size;
    }

    public void AddToPool(T item)
    {
        item.Pool = this;
    }
    private T AcquireEager()
    {
        lock (itemStore)
        {
            return itemStore.Fetch();
        }
    }
    private T AcquireLazy()
    {
        lock (itemStore)
        {
            if (itemStore.Count > 0)
            {
                return itemStore.Fetch();
            }
        }
        Interlocked.Increment(ref count);
        return factory(this);
    }
    private T AcquireLazyExpanding()
    {
        bool shouldExpand = false;
        if (count < size)
        {
            int newCount = Interlocked.Increment(ref count);
            if (newCount <= size)
            {
                shouldExpand = true;
            }
            else
            {
                Interlocked.Decrement(ref count);
            }
        }
        if (shouldExpand)
        {
            return factory(this);
        }
        else
        {
            lock (itemStore)
            {
                return itemStore.Fetch();
            }
        }
    }

    private IItemStore CreateItemStore(PoolAccessMode mode, int capacity)
    {
        switch (mode)
        {
            case PoolAccessMode.FIFO:
                return new QueueStore(capacity);
            case PoolAccessMode.LIFO:
                return new StackStore(capacity);
            default:
                Debug.Assert(mode == PoolAccessMode.Circular,
                                "Invalid AccessMode in CreateItemStore");
                return new CircularStore(capacity);
        }
    }
    interface IItemStore
    {
        T Fetch();
        void Store(T item);
        int Count { get; }
    }

    class QueueStore : Queue<T>, IItemStore
    {
        public QueueStore(int capacity) : base(capacity)
        {
        }
        public T Fetch()
        {
            return Dequeue();
        }
        public void Store(T item)
        {
            Enqueue(item);
        }
    }

    class StackStore : Stack<T>, IItemStore
    {
        public StackStore(int capacity) : base(capacity)
        {
        }
        public T Fetch()
        {
            return Pop();
        }
        public void Store(T item)
        {
            Push(item);
        }
    }

    class CircularStore : IItemStore
    {
        private List<Slot> slots;
        private int freeSlotCount;
        private int position = -1;

        public CircularStore(int capacity)
        {
            slots = new List<Slot>(capacity);
        }

        public T Fetch()
        {
            if (Count == 0)
                throw new InvalidOperationException("The buffer is empty.");

            int startPosition = position;
            do
            {
                Advance();
                Slot slot = slots[position];
                if (!slot.IsInUse)
                {
                    slot.IsInUse = true;
                    --freeSlotCount;
                    return slot.Item;
                }
            } while (startPosition != position);
            throw new InvalidOperationException("No free slots.");
        }

        public void Store(T item)
        {
            Slot slot = slots.Find(s => object.Equals(s.Item, item));
            if (slot == null)
            {
                slot = new Slot(item);
                slots.Add(slot);
            }
            slot.IsInUse = false;
            ++freeSlotCount;
        }

        public int Count
        {
            get { return freeSlotCount; }
        }

        private void Advance()
        {
            position = (position + 1) % slots.Count;
        }

        class Slot
        {
            public Slot(T item)
            {
                this.Item = item;
            }

            public T Item { get; private set; }
            public bool IsInUse { get; set; }
        }
    }

}