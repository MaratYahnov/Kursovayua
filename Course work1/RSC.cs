using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Course_work
{
    public sealed class RSC
    {

        private static byte[] ERR_BLOCK = Encoding.UTF8.GetBytes("");
        private readonly GF field;
        private readonly int blockSize;
        private readonly int ecSize;
        private readonly int[] errorBlock;
        private readonly List<GFP> cachedGenerators;

        public RSC(GF field, int blockSize, int errorCorrectionSize)
        {
            if (blockSize < ERR_BLOCK.Length)
            {
                throw new ArgumentException("Размер блока не может быть меньше размера блока с сообщением об ошибке!");
            }
            this.field = field;
            this.blockSize = blockSize;
            this.ecSize = errorCorrectionSize;
            this.errorBlock = Enumerable.Repeat(32, this.blockSize).ToArray();
            Array.Copy(ERR_BLOCK.Select(x => (int)x).ToArray(), 0, this.errorBlock, 0, ERR_BLOCK.Length);
            this.cachedGenerators = new List<GFP>();
            cachedGenerators.Add(new GFP(field, new int[] { 1 }));
        }

        public byte[] encode(byte[] message)
        {
            int length = message.Length;
            int blocks_count = (length / this.blockSize + ((0 != (length % this.blockSize)) ? 1 : 0));
            int[] source = message.Select(x => (int)x).ToArray();
            int[] result = new int[blocks_count * (this.blockSize + this.ecSize)];
            for (int i = 0; i < blocks_count; i++)
            {
                int[] block = Enumerable.Repeat(0, this.blockSize + this.ecSize).ToArray();
                Array.Copy(source, i * this.blockSize, block, 0, Math.Min(this.blockSize, length - (i * this.blockSize)));
                this.encode(block);
                Array.Copy(block, 0, result, i * (this.blockSize + this.ecSize), this.blockSize + this.ecSize);
            }
            return result.Select(x => (byte)x).ToArray();
        }

        public byte[] decode(byte[] message)
        {
            int size = this.blockSize + this.ecSize;
            int blocks_count = (message.Length / size + ((0 != (message.Length % size)) ? 1 : 0));
            int[] source = message.Select(x => (int)x).ToArray();
            int[] result = new int[blocks_count * this.blockSize];
            for (int i = 0; i < blocks_count; i++)
            {
                int[] block = Enumerable.Repeat(0, size).ToArray();
                Array.Copy(source, i * size, block, 0, Math.Min(size, message.Length - (i * size)));
                if (this.decode(block))
                {
                    Array.Copy(block, 0, result, i * this.blockSize, this.blockSize);
                }
                else
                {
                    Array.Copy(this.errorBlock, 0, result, i * this.blockSize, this.blockSize);
                }
            }
            return result.Select(x => (byte)x).ToArray();
        }

        private GFP buildGenerator(int degree)
        {
            if (degree >= cachedGenerators.Count)
            {
                var lastGenerator = cachedGenerators[cachedGenerators.Count - 1];
                for (int d = cachedGenerators.Count; d <= degree; d++)
                {
                    var nextGenerator = lastGenerator.multiply(new GFP(field, new int[] { 1, field.exp(d - 1 + field.GeneratorBase) }));
                    cachedGenerators.Add(nextGenerator);
                    lastGenerator = nextGenerator;
                }
            }
            return cachedGenerators[degree];
        }

        private void encode(int[] toEncode)
        {
            int ecBytes = this.ecSize;
            if (ecBytes <= 0)
            {
                throw new ArgumentException("Количество избыточных элементов должно быть больше 0");
            }
            var dataBytes = toEncode.Length - ecBytes;
            if (dataBytes <= 0)
            {
                throw new ArgumentException("Нет элементов с данными");
            }
            var generator = buildGenerator(ecBytes);
            var infoCoefficients = new int[dataBytes];
            Array.Copy(toEncode, 0, infoCoefficients, 0, dataBytes);

            var info = new GFP(field, infoCoefficients);
            info = info.multiplyByMonomial(ecBytes, 1);

            var remainder = info.divide(generator)[1];
            var coefficients = remainder.Coefficients;
            var numZeroCoefficients = ecBytes - coefficients.Length;
            for (var i = 0; i < numZeroCoefficients; i++)
            {
                toEncode[dataBytes + i] = 0;
            }

            Array.Copy(coefficients, 0, toEncode, dataBytes + numZeroCoefficients, coefficients.Length);
        }

        private bool decode(int[] received)
        {
            int twoS = this.ecSize;
            var poly = new GFP(field, received);
            var syndromeCoefficients = new int[twoS];
            var noError = true;
            for (var i = 0; i < twoS; i++)
            {
                var eval = poly.evaluateAt(field.exp(i + field.GeneratorBase));
                syndromeCoefficients[syndromeCoefficients.Length - 1 - i] = eval;
                if (eval != 0)
                {
                    noError = false;
                }
            }
            if (noError)
            {
                return true;
            }
            var syndrome = new GFP(field, syndromeCoefficients);
            var sigmaOmega = runEuclideanAlgorithm(field.buildMonomial(twoS, 1), syndrome, twoS);
            if (sigmaOmega == null)
            {
                return false;
            }
            var sigma = sigmaOmega[0];
            var errorLocations = findErrorLocations(sigma);
            if (errorLocations == null)
                return false;

            var omega = sigmaOmega[1];
            var errorMagnitudes = findErrorMagnitudes(omega, errorLocations);
            for (var i = 0; i < errorLocations.Length; i++)
            {
                var position = received.Length - 1 - field.log(errorLocations[i]);
                if (position < 0)
                {
                    return false;
                }
                received[position] = GF.addOrSubtract(received[position], errorMagnitudes[i]);
            }
            return true;
        }

        internal GFP[] runEuclideanAlgorithm(GFP a, GFP b, int R)
        {
            if (a.Degree < b.Degree)
            {
                GFP temp = a;
                a = b;
                b = temp;
            }

            GFP rLast = a;
            GFP r = b;
            GFP tLast = field.Zero;
            GFP t = field.One;
            while (r.Degree >= R / 2)
            {
                GFP rLastLast = rLast;
                GFP tLastLast = tLast;
                rLast = r;
                tLast = t;

                if (rLast.isZero)
                {
                    return null;
                }
                r = rLastLast;
                GFP q = field.Zero;
                int denominatorLeadingTerm = rLast.getCoefficient(rLast.Degree);
                int dltInverse = field.inverse(denominatorLeadingTerm);
                while (r.Degree >= rLast.Degree && !r.isZero)
                {
                    int degreeDiff = r.Degree - rLast.Degree;
                    int scale = field.multiply(r.getCoefficient(r.Degree), dltInverse);
                    q = q.addOrSubtract(field.buildMonomial(degreeDiff, scale));
                    r = r.addOrSubtract(rLast.multiplyByMonomial(degreeDiff, scale));
                }

                t = q.multiply(tLast).addOrSubtract(tLastLast);

                if (r.Degree >= rLast.Degree)
                {
                    return null;
                }
            }
            int sigmaTildeAtZero = t.getCoefficient(0);
            if (sigmaTildeAtZero == 0)
            {
                return null;
            }

            int inverse = field.inverse(sigmaTildeAtZero);
            GFP sigma = t.multiply(inverse);
            GFP omega = r.multiply(inverse);
            return new GFP[] { sigma, omega };
        }
        private int[] findErrorLocations(GFP errorLocator)
        {
            int numErrors = errorLocator.Degree;
            if (numErrors == 1)
            {
                return new int[] { errorLocator.getCoefficient(1) };
            }
            int[] result = new int[numErrors];
            int e = 0;
            for (int i = 1; i < field.Size && e < numErrors; i++)
            {
                if (errorLocator.evaluateAt(i) == 0)
                {
                    result[e] = field.inverse(i);
                    e++;
                }
            }
            if (e != numErrors)
            {
                return null;
            }
            return result;
        }

        private int[] findErrorMagnitudes(GFP errorEvaluator, int[] errorLocations)
        {
            int s = errorLocations.Length;
            int[] result = new int[s];
            for (int i = 0; i < s; i++)
            {
                int xiInverse = field.inverse(errorLocations[i]);
                int denominator = 1;
                for (int j = 0; j < s; j++)
                {
                    if (i != j)
                    {

                        int term = field.multiply(errorLocations[j], xiInverse);
                        int termPlus1 = (term & 0x1) == 0 ? term | 1 : term & ~1;
                        denominator = field.multiply(denominator, termPlus1);
                    }
                }
                result[i] = field.multiply(errorEvaluator.evaluateAt(xiInverse), field.inverse(denominator));
                if (field.GeneratorBase != 0)
                {
                    result[i] = field.multiply(result[i], xiInverse);
                }
            }
            return result;
        }
    }

}
