using DotNetty.Common.Utilities;

namespace DotNetty.Common.Internal
{
    public interface IAppendable
    {
        IAppendable Append(char c);
        IAppendable Append(ICharSequence sequence);
        IAppendable Append(ICharSequence sequence, int start, int end);
    }
}
