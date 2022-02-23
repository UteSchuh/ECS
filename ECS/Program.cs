using ECS;
using System.Diagnostics;
using System.Runtime.CompilerServices;

var registry = new Registry();
for(int i = 0; i < 1000; i++)
{
    var entity = registry.CreateEntity();
    ref var position = ref registry.AddComponent<Position>(entity);
    position.x = i;
    position.y = i * 2;

    ref var sprite = ref registry.AddComponent<Sprite>(entity);
    sprite.Path = i.ToString();
    sprite.Width = i * 5;
    sprite.Height = i * 6;

    ref var sound = ref registry.AddComponent<Sound>(entity);
    sound.Path = "MyFile.txt";
    sound.Volume = i / 10.0f;
}

var results = new List<double>();
for(var i = 0; i < 1000; i++)
{
    var stopwatch = Stopwatch.StartNew();
    registry.ForEach((ref Position position, ref Sprite sprite, ref Sound sound) =>
    {
        if (position.x % 20 != 0)
            return;
        if(sprite.Width > sprite.Height)
            return;
        if (sound.Volume > 10.0f)
            return;
    });
    results.Add(stopwatch.Elapsed.TotalMilliseconds);
}
Console.WriteLine(results.Sum() / results.Count);

public struct Position
{
    public int x;
    public int y;
}

public struct Sprite
{
    public string Path;
    public int Width;
    public int Height;
}

public struct Sound
{
    public string Path;
    public float Volume;
}

public class Registry
{
    public delegate void RefAction<T>(ref T arg1);
    public delegate void RefAction<TA, TB>(ref TA arg1, ref TB arg2);
    public delegate void RefAction<TA, TB, TC>(ref TA arg1, ref TB arg2, ref TC arg3);

    internal static int ComponentCount { get; set; }

    private Guid[] _entities;
    private int _entityCount;
    private IComponentPool[] _componentPools;

    public Registry()
    {
        _entities = new Guid[256];
        _entityCount = 0;
        _componentPools = new IComponentPool[16];
    }

    public Guid CreateEntity()
    {
        var index = _entityCount;
        if(index >= _entities.Length)
        {
            Array.Resize(ref _entities, index << 1);
        }

        _entities[index] = Guid.NewGuid();
        _entityCount++;
        return _entities[index];
    }

    public void DestroyEntity(Guid entity)
    {
        var indexToDelete = Array.IndexOf(_entities, entity);
        if(indexToDelete == -1)
        {
            return;
        }

        _entities[indexToDelete] = _entities[_entityCount - 1];
        _entityCount--;
    }

    public Span<Guid> GetAllEntites() => new Span<Guid>(_entities, 0, _entityCount);
    public ref TComponent AddComponent<TComponent>(Guid entity) where TComponent : struct => ref GetComponentPool<TComponent>().CreateComponent(entity);
    public void RemoveComponent<TComponent>(Guid entity) where TComponent : struct => GetComponentPool<TComponent>().DestroyComponent(entity);
    public ref TComponent GetComponent<TComponent>(Guid entity) where TComponent : struct => ref GetComponentPool<TComponent>().GetComponent(entity);

    public void ForEach<TComponent>(RefAction<TComponent> action) 
        where TComponent : struct
    {
        var componentPool = GetComponentPool<TComponent>();

        for (var i = 0; i < _entityCount; i++)
        {
            if (componentPool.ContainsEntity(_entities[i]))
            {
                action(ref componentPool.GetComponent(_entities[i]));
            }
        }
    }

    public void ForEach<TComponentA, TComponentB>(RefAction<TComponentA, TComponentB> action)
        where TComponentA : struct 
        where TComponentB : struct
    {
        var componentPoolA = GetComponentPool<TComponentA>();
        var componentPoolB = GetComponentPool<TComponentB>();

        for (var i = 0; i < _entityCount; i++)
        {
            if (componentPoolA.ContainsEntity(_entities[i]) 
                && componentPoolB.ContainsEntity(_entities[i]))
            {
                action(ref componentPoolA.GetComponent(_entities[i]), 
                    ref componentPoolB.GetComponent(_entities[i]));
            }
        }
    }

    public void ForEach<TComponentA, TComponentB, TComponentC>(RefAction<TComponentA, TComponentB, TComponentC> action) 
        where TComponentA : struct 
        where TComponentB : struct 
        where TComponentC : struct
    {
        var componentPoolA = GetComponentPool<TComponentA>();
        var componentPoolB = GetComponentPool<TComponentB>();
        var componentPoolC = GetComponentPool<TComponentC>();

        for (var i = 0; i < _entityCount; i++)
        {
            if (componentPoolA.ContainsEntity(_entities[i]) 
                && componentPoolB.ContainsEntity(_entities[i]) 
                && componentPoolC.ContainsEntity(_entities[i]))
            {
                action(ref componentPoolA.GetComponent(_entities[i]), 
                    ref componentPoolB.GetComponent(_entities[i]), 
                    ref componentPoolC.GetComponent(_entities[i]));
            }
        }
    }

    private ComponentPool<TComponent> GetComponentPool<TComponent>() where TComponent : struct
    {
        var index = ComponentInfo<TComponent>.Id;
        if(index >= _componentPools.Length)
        {
            var newLength = _componentPools.Length << 1;
            while(newLength <= index)
            {
                newLength <<= 1;
            }

            Array.Resize(ref _componentPools, newLength);
        }
        
        if(_componentPools[index] is null)
        {
            _componentPools[index] = new ComponentPool<TComponent>();
        }
        return (ComponentPool<TComponent>)_componentPools[index];
    }
}