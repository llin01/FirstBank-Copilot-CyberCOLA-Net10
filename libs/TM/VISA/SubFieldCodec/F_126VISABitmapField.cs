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

    public class F_126VISABitmapField : VISABitmapField
    {
        private static readonly IDictionary<FIELD, VISAField> DataElementSpecs = new Dictionary<FIELD, VISAField>();

        private static readonly Packer Packers = new DefaultPacker();

        static F_126VISABitmapField()
        {
            DataElementSpecs.Add(FIELD.F_1, new VISAField(FIELD.F_1, 25, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_2, new VISAField(FIELD.F_2, 57, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_3, new VISAField(FIELD.F_3, 57, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_4, new VISAField(FIELD.F_4, 18, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_5, new VISAField(FIELD.F_5, 8, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_6, new VISAField(FIELD.F_6, 17, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_7, new VISAField(FIELD.F_7, 17, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_8, new VISAField(FIELD.F_8, 20, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_9, new VISAField(FIELD.F_9, 20, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_10, new VISAField(FIELD.F_10, 6, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_12, new VISAField(FIELD.F_12, 3, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_13, new VISAField(FIELD.F_13, 1, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_14, new VISAField(FIELD.F_14, 1, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_15, new VISAField(FIELD.F_15, 1, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_16, new VISAField(FIELD.F_16, 33, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_18, new VISAField(FIELD.F_18, 12, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_19, new VISAField(FIELD.F_19, 1, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_20, new VISAField(FIELD.F_20, 1, Format.Fix, Representation.ANS, Packers));
        }

        public F_126VISABitmapField()
            : base(8)
        {
        }

        public F_126VISABitmapField(byte[] fieldBytes)
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
