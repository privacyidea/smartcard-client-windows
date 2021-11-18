using PISmartcardClient;
using System;

namespace Tests.TestUtils
{
    public class TestDispatcher : IUIDispatcher
    {
        void IUIDispatcher.Invoke(Action action)
        {
            action.Invoke();
        }
    }
}
