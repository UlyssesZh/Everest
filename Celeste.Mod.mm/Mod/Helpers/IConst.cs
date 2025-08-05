namespace Celeste.Mod.Helpers;

/// <summary>
/// Represents a constant value, to be used for generic methods by structs implementing this interface.
/// </summary>
public interface IConst<out T>
{
    /// <summary>
    /// The value of this constant.
    /// </summary>
    public static abstract T Value { get; }
}