namespace DotNetty.Common.Utilities
{
    /// <summary>
    ///     A singleton which is safe to compare via the <c>==</c> operator. Created and managed by
    ///     <see cref="ConstantPool" />.
    /// </summary>
    public interface IConstant
    {
        int Id { get; }
        string Name { get; }
    }
}