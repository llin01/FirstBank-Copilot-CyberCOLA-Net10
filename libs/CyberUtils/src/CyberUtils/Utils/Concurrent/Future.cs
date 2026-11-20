namespace Com.Cybersoft4u.CyberUtils.Utils.Concurrent
{
    public interface Future
    {
        bool Cancel();

        object Get();

        object Get(long millisecond);

        bool Canceled();

        bool Done();
    }
}
