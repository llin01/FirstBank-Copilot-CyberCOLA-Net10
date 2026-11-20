namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    public interface CoreWrapper
    {
        /// <summary>
        /// Decode Fields data and put into <code>Core</code> variable.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="field"></param>
        void Pack(Core core, FIELD field);

        /// <summary>
        /// Encode each <code>Field</code> from <code>Core</code> variables.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="field"></param>
        void UnPack(Core core, FIELD field);
    }
}
