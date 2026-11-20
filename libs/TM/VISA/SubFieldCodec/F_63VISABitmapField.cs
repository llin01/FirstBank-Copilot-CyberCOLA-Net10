// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_62VISABitmapField.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_62VISABitmapField type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec
{
    #region DEPENDENCY

    using System.Collections.Generic;

    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class F_63VISABitmapField : VISABitmapField
    {
        private static readonly IDictionary<FIELD, VISAField> DataElementSpecs = new Dictionary<FIELD, VISAField>();

        private static readonly Packer Packers = new DefaultPacker();

        static F_63VISABitmapField()
        {

            DataElementSpecs.Add(FIELD.F_1, new VISAField(FIELD.F_1, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_2, new VISAField(FIELD.F_2, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_3, new VISAField(FIELD.F_3, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_4, new VISAField(FIELD.F_4, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_19, new VISAField(FIELD.F_19, 3, Format.Fix, Representation.AN, Packers));
        }

        public F_63VISABitmapField()
            : base(3)
        {
        }

        public F_63VISABitmapField(byte[] fieldBytes)
            : base(3, fieldBytes)
        {
        }

        public override DataElementSpec Spec(FIELD field)
        {
            VISAField spec;
            DataElementSpecs.TryGetValue(field, out spec);
            return spec;
        }
    }
}
