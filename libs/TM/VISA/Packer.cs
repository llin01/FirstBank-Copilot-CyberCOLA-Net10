// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Packer.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Packer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public interface Packer : IPacker
    {
        ByteBuffer Pack(object data, VISAField spec);

        object Unpack(VISAField spec, ByteBuffer record);
    }
}
