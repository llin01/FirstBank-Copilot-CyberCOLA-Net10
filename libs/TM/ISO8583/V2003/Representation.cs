namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    public enum Representation
    {
        /// <summary>
        /// Alphabetical Character
        /// </summary>
        A,

        /// <summary>
        /// Alphabetical Character and Number
        /// </summary>
        AN,

        /// <summary>
        /// Alphabetical Character, Number, Padding Character
        /// </summary>
        ANP,

        /// <summary>
        /// Alphabetical Character, Number, Space
        /// </summary>
        ANS,

        /// <summary>
        /// Alphabetical Character, Number, Binary
        /// </summary>
        ANB,

        /// <summary>
        /// Alphabetical Character, Number, Space, Binary
        /// </summary>
        ANSB,

        /// <summary>
        /// Alphabetical Character, Space
        /// </summary>
        AS,

        /// <summary>
        /// Binary
        /// </summary>
        B,

        /// <summary>
        /// Number, Hex represent
        /// </summary>
        N,

        /// <summary>
        /// Number and Space
        /// </summary>
        NS,

        /// <summary>
        /// Padding Character
        /// </summary>
        P,

        /// <summary>
        /// Space
        /// </summary>
        S,

        /// <summary>
        /// Start with <code>C</code> for credit, or <code>D</code> for debit
        /// </summary>
        XN,

        /// <summary>
        /// Track 2, 3
        /// </summary>
        Z
    }
}
