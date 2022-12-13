using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40x_BareMetal
{
    static class Extensions
    {
        public static byte[] ToBigEndianBytes(this UInt32 x)
        {
            byte[] bytes = BitConverter.GetBytes(x);
            Array.Reverse(bytes);
            return bytes;
        }


        /// <summary>
        /// Multiplies (in place) an array of doubles by a scalar value
        /// </summary>
        /// <param name="data"></param>
        /// <param name="scaler"></param>
        /// <returns></returns>
        public static double[] ScalarMultiply(this double[] data, double scaler)
        {
            for (int i=0; i<data.Length; i++)
            {
                data[i] = data[i] * scaler;
            }

            return data;
        }
    }
}
