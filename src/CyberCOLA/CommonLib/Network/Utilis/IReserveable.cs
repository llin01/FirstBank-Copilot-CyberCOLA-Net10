// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IReserveable.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   IReserveable.cs
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.CommonLib.Network.Utilis
{
    public interface IReserveable<in TItem>
    {
        bool Take(TItem item);

        void Release(TItem item);

        bool IsTake(TItem item);

        bool Remove(TItem item);
    }
}
