namespace CGame
{
    public interface ICallbackContainer<TCallback>
    {
        void AddCallbacks(TCallback callbacks);
        void RemoveCallbacks(TCallback callbacks);
    }
}
