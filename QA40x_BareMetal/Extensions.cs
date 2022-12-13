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

        public static double[] Multiply(this double[] data, double scaler)
        {
            for (int i=0; i<data.Length; i++)
            {
                data[i] = data[i] * scaler;
            }

            return data;
        }
    }
}
