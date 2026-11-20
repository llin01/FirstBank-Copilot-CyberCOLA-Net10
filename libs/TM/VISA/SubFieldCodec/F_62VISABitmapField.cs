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

    public class F_62VISABitmapField : VISABitmapField
    {
        private static readonly IDictionary<FIELD, VISAField> DataElementSpecs = new Dictionary<FIELD, VISAField>();

        private static readonly Packer Packers = new DefaultPacker();

        static F_62VISABitmapField()
        {
            DataElementSpecs.Add(FIELD.F_1, new VISAField(FIELD.F_1, 1, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_2, new VISAField(FIELD.F_2, 8, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_3, new VISAField(FIELD.F_3, 4, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_4, new VISAField(FIELD.F_4, 1, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_5, new VISAField(FIELD.F_5, 1, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_6, new VISAField(FIELD.F_6, 1, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_17, new VISAField(FIELD.F_17, 15, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_20, new VISAField(FIELD.F_20, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_21, new VISAField(FIELD.F_21, 4, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_22, new VISAField(FIELD.F_22, 6, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_23, new VISAField(FIELD.F_23, 2, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_24, new VISAField(FIELD.F_24, 6, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_25, new VISAField(FIELD.F_25, 1, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_26, new VISAField(FIELD.F_26, 1, Format.Fix, Representation.AN, Packers));
        }

        public F_62VISABitmapField()
            : base(8)
        {
        }

        public F_62VISABitmapField(byte[] fieldBytes)
            : base(8, fieldBytes)
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
