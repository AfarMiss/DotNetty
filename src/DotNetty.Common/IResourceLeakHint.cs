namespace DotNetty.Common
{
    /// <summary>
    ///     A hint object that provides human-readable message for easier resource leak tracking.
    /// </summary>
    public interface IResourceLeakHint
    {
        /// <summary>
        ///     Returns a human-readable message that potentially enables easier resource leak tracking.
        /// </summary>
        /// <returns></returns>
        string ToHintString();
    }
}