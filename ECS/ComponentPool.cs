namespace ECS;

public class ComponentPool<TComponent> : IComponentPool where TComponent : struct
{
    public Type ComponentType { get; }

    private TComponent[] _components;
    private int _componentCount;
    private readonly Dictionary<Guid, int> _componentIndexByEntity;

    public ComponentPool()
    {
        ComponentType = typeof(TComponent);
        _components = new TComponent[256];
        _componentIndexByEntity = new(256);
        _componentCount = 0;
    }

    public bool ContainsEntity(Guid entity)
    {
        return _componentIndexByEntity.ContainsKey(entity);
    }

    public ref TComponent CreateComponent(Guid entity)
    {
        //If the entity already has a component, return that component
        if(_componentIndexByEntity.TryGetValue(entity, out var componentIndex))
        {
            return ref _components[componentIndex];
        }

        //If not, create a new component and add that index of the component in the lookup
        //If the array is too small, resize it to twice the current size
        int index = _componentCount;
        if (index >= _components.Length)
        {
            Array.Resize(ref _components, index << 1);
        }

        _components[index] = new();
        _componentIndexByEntity[entity] = index;
        _componentCount++;
        return ref _components[index];
    }

    public ref TComponent GetComponent(Guid entity)
    {
        //Get the component id from the lookup table and return the component
        return ref _components[_componentIndexByEntity[entity]];
    }

    public void DestroyComponent(Guid entity)
    {
        //Store the old index in a variable and remove the entity from the lookup table
        var indexToDelete = _componentIndexByEntity[entity];
        _componentIndexByEntity.Remove(entity);

        //If there was only one entity, don't replace the old component with the last component
        var lastIndex = _componentCount - 1;
        if(lastIndex > 0)
        {
            //Find the last entity by the last index
            var lastEntity = GetEntityByComponentIndex(lastIndex);

            //Copy the component from the last index in the deleted component's index
            //Update the lookup table for the last entity
            _components[indexToDelete] = _components[lastIndex];
            _componentIndexByEntity[lastEntity] = indexToDelete;
        }
        //Decrement the component count, the next create index will be the previous last index
        _componentCount--;
    }

    private Guid GetEntityByComponentIndex(int index)
    {
        var entity = Guid.Empty;
        foreach(var componentEntityPair in _componentIndexByEntity)
        {
            if(componentEntityPair.Value == index)
            {
                entity = componentEntityPair.Key;
                break;
            }
        }
        return entity;
    }
}