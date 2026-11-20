// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IReviveService.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IReviveService type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.ServiceProvider.Utils
{
    public interface IReviveService<in T>
    {
        void Remove(T item);

        void Release(T item);
    }
}
