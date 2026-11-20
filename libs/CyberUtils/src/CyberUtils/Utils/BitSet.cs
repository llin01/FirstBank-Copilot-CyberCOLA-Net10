namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;
    using System.Collections;

    #endregion

    public class BitSet
    {
        private readonly BitArray data;

        public BitSet(byte[] byteArray)
        {
            this.data = new BitArray(byteArray);
        }

        public BitSet(Bytes bytes)
        {
            this.data = new BitArray(bytes.Val());
        }

        public int PreviousSetBit(int fromIndex)
        {
            if (fromIndex < 0)
            {
                if (fromIndex == -1)
                {
                    return -1;
                }

                throw new IndexOutOfRangeException("fromIndex < -1: " + fromIndex);
            }

            if (fromIndex >= this.data.Length)
            {
                return this.PreviousSetBit(this.data.Length - 1);
            }

            var currentIndex = fromIndex;

            while (true)
            {
                if (this.data[currentIndex])
                {
                    return currentIndex;
                }

                currentIndex--;
                if (currentIndex < 0)
                {
                    return -1;
                }
            }
        }
    }
}
