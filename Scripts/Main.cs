using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

public partial class Main : Node3D
{
    [Export]
    public Node serverNode;

    public Pool<Foo> pool = new Pool<Foo>(5, p => new Foo(p), PoolLoadingMode.Lazy, PoolAccessMode.Circular);

    public override void _Ready()
    {
        using (Foo _fooA = pool.Acquire())
        {
            _fooA.Test();
        }
        Foo _foo = pool.Acquire();
        Foo _foo2 = pool.Acquire();
        _foo.Test();
        _foo2.Test();
        _foo.Dispose();
        _foo2.Dispose();
    }

    public override void _Process(double delta)
    {
        GD.Print("test");
    }

    public async Task LoadServerLayout(string path)
    {
        var server = await File.LoadJson<ServerSerializedLayout>(path);
    }

    public void SaveServerLayout(string path)
    {
        List<NodeSerializedLayout> nodes = new List<NodeSerializedLayout>();
        List<LinkSerializedLayout> links = new List<LinkSerializedLayout>();
        foreach (var node in serverNode.Get("node_instances").AsGodotArray<Node>())
        {
            nodes.Add(new NodeSerializedLayout()
            {
                ID = node.Get("ID").AsInt16(),
                pos = node.Get("position").AsVector3(),
                Flags = 0
            });
        }
        foreach (var link in serverNode.Get("link_list").AsGodotArray<int[]>())
        {
            links.Add(new LinkSerializedLayout()
            {
                NodeA = link[0],
                NodeB = link[1],
                Flags = 0
            });
        }
        ServerSerializedLayout server = new ServerSerializedLayout()
        {
            Nodes = nodes.ToArray(),
            Links = links.ToArray()
        };
        Task.Run(async () => await File.SaveJson(path, server));
    }

    public struct ServerSerializedLayout
    {
        public NodeSerializedLayout[] Nodes;
        public LinkSerializedLayout[] Links;
    }

    public struct NodeSerializedLayout
    {
        public int ID;
        public Vector3 pos;
        public int Flags;
    }
    public struct LinkSerializedLayout
    {
        public int NodeA, NodeB;
        public int Flags;
    }
}


//https://stackoverflow.com/questions/2510975/c-sharp-object-pooling-pattern-implementation
public enum PoolAccessMode { FIFO, LIFO, Circular }
public enum PoolLoadingMode { Eager, Lazy, LazyExpanding }
public interface IDisposablePoolResource : IDisposable
{
    public IDisposablePool Pool { get; }
}

public class Foo : IDisposablePoolResource
{
    private static int count = 0;
    private int num;
    private IDisposablePool pool;
    public IDisposablePool Pool => pool;
    public Foo(IDisposablePool _pool)
    {
        num = Interlocked.Increment(ref count);
        pool = _pool;
    }
    public void Dispose()
    {
        if (Pool == null)
            throw new ArgumentException("Pool has not been assigned!");

        if (Pool.IsDisposed)
        {
            Console.WriteLine("DISPOSE FOO: " + num);
        }
        else
        {
            Pool.Release(this);
        }
    }
    public void Test()
    {
        Console.WriteLine("TEST FOO: " + num);
    }
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

    public bool IsDisposed => isDisposed;

    public Pool(int _size, Func<Pool<T>, T> _factory, PoolLoadingMode _loadingMode, PoolAccessMode _accessMode)
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
        switch (loadingMode)
        {
            case PoolLoadingMode.Eager:
                return AcquireEager();
            case PoolLoadingMode.Lazy:
                return AcquireLazy();
            default:
                return AcquireLazyExpanding();
        }
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
            Console.WriteLine("STORING " + item);
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