using System;

namespace PISmartcardClient
{
    public interface IUIDispatcher
    {
        public void Invoke(Action action);
    }

    public class UIDispatcher : IUIDispatcher
    {
        public void Invoke(Action action)
        {
            App.Current.Dispatcher.Invoke(action);
        }
    }
}
