using System;

namespace Course_work
{
    public sealed class GF
    {
        private int[] expTable;
        private int[] logTable;
        private GFP zero;
        private GFP one;
        private readonly int size;
        private readonly int primitive;
        private readonly int generatorBase;

        public GF(int primitive, int size, int genBase)
        {
            this.primitive = primitive;
            this.size = size;
            this.generatorBase = genBase;

            expTable = new int[size];
            logTable = new int[size];
            int x = 1;
            for (int i = 0; i < size; i++)
            {
                expTable[i] = x;
                x <<= 1;
                if (x >= size)
                {
                    x ^= primitive;
                    x &= size - 1;
                }
            }
            for (int i = 0; i < size - 1; i++)
            {
                logTable[expTable[i]] = i;
            }
            // logTable[0] == 0 but this should never be used
            zero = new GFP(this, new int[] { 0 });
            one = new GFP(this, new int[] { 1 });
        }

        internal GFP Zero
        {
            get
            {
                return zero;
            }
        }

        internal GFP One
        {
            get
            {
                return one;
            }
        }

        internal GFP buildMonomial(int degree, int coefficient)
        {
            if (degree < 0)
            {
                throw new ArgumentException();
            }
            if (coefficient == 0)
            {
                return zero;
            }
            int[] coefficients = new int[degree + 1];
            coefficients[0] = coefficient;
            return new GFP(this, coefficients);
        }

        static internal int addOrSubtract(int a, int b)
        {
            return a ^ b;
        }

        internal int exp(int a)
        {
            return expTable[a];
        }

        internal int log(int a)
        {
            if (a == 0)
            {
                throw new ArgumentException();
            }
            return logTable[a];
        }

        internal int inverse(int a)
        {
            if (a == 0)
            {
                throw new ArithmeticException();
            }
            return expTable[size - logTable[a] - 1];
        }

        internal int multiply(int a, int b)
        {
            if (a == 0 || b == 0)
            {
                return 0;
            }
            return expTable[(logTable[a] + logTable[b]) % (size - 1)];
        }

        public int Size
        {
            get { return size; }
        }


        public int GeneratorBase
        {
            get { return generatorBase; }
        }

    }
}
