using System;

namespace Contracts {
    public interface IMatch {
        Tuple<int, int> Match(byte[] file);
    }
}
