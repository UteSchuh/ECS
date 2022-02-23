namespace ECS;

public static class ComponentInfo<T> where T : struct
{
    public static int Id { get; }
    public static Type Type { get; }

    static ComponentInfo()
    {
        Id = Registry.ComponentCount++;
        Type = typeof(T);
    }
}