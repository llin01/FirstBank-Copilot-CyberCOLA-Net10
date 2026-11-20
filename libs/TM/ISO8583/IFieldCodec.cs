// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IFieldCodec.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IFieldCodec type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583
{
    public interface IFieldCodec<out TEnRet, in TDeParam, out TDeRet>
    {
        TEnRet Encode();

        TDeRet Decode(TDeParam data);
    }
}
