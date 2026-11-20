// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IFunctionalFieldCodec.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IFunctionalFieldCodec type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583
{
    public interface IFunctionalFieldCodec<in TEnParam, out TEnRet, in TDeParam, out TDeRet>
    {
        TEnRet Encode(TEnParam data);

        TDeRet Decode(TDeParam data);
    }
}
