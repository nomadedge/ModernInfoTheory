using ModernInfoTheory.ReedSolomon.GF;
using System;
using System.Collections.Generic;

namespace ModernInfoTheory.ReedSolomon.RS
{
    public sealed class ReedSolomonDecoder
    {
        private readonly GenericGF field;

        public ReedSolomonDecoder(GenericGF field)
        {
            this.field = field;
        }

        public bool Decode(int[] received, int twoS)
        {

            if (received.Length >= field.Size)
            {
                throw new ArgumentException("Message is too long for this field", "received");
            }

            if (twoS <= 0)
            {
                throw new ArgumentException("No error correction bytes provided", "twoS");
            }
            var dataBytes = received.Length - twoS;
            if (dataBytes <= 0)
            {
                throw new ArgumentException("No data bytes provided", "twoS");
            }

            var syndromeCoefficients = new int[twoS];
            var noError = true;

            var poly = new GenericGFPoly(field, received, false);

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

            var syndrome = new GenericGFPoly(field, syndromeCoefficients, false);

            var forneySyndrome = calculateForneySyndromes(syndrome, received.Length);

            var sigma = runBerlekampMasseyAlgorithm(forneySyndrome);

            if (sigma == null)
            {
                return false;
            }

            var errorLocations = findErrorLocations(sigma);
            if (errorLocations == null)
            {
                return false;
            }

            // Prepare errors
            int[] errorPositions = new int[errorLocations.Length];

            for (int i = 0; i < errorLocations.Length; i++)
            {
                errorPositions[i] = field.log(errorLocations[i]);
            }

            var errorLocator = findErrorLocator(errorPositions);
            var omega = findErrorEvaluator(syndrome, errorLocator);

            if (omega == null)
            {
                return false;
            }

            int[] errors = new int[errorPositions.Length];

            for (int i = 0; i < errorPositions.Length; i++)
            {
                errors[i] = field.exp(errorPositions[i]);
            }

            var errorMagnitudes = findErrorMagnitudes(omega, errors);

            if (errorMagnitudes == null)
            {
                return false;
            }

            for (var i = 0; i < errors.Length; i++)
            {
                var position = received.Length - 1 - field.log(errors[i]);
                if (position < 0)
                {
                    // throw new ReedSolomonException("Bad error location");
                    return false;
                }
                received[position] = GenericGF.addOrSubtract(received[position], errorMagnitudes[i]);
            }

            var checkPoly = new GenericGFPoly(field, received, false);

            var error = false;

            for (var i = 0; i < twoS; i++)
            {
                var eval = checkPoly.evaluateAt(field.exp(i + field.GeneratorBase));
                if (eval != 0)
                {
                    error = true;
                }
            }
            if (error)
            {
                return false;
            }

            return true;
        }

        internal GenericGFPoly calculateForneySyndromes(GenericGFPoly syndromes, int messageLength)
        {
            int[] syndromeCoefficients = new int[syndromes.Coefficients.Length];
            Array.Copy(syndromes.Coefficients, 0, syndromeCoefficients, 0, syndromes.Coefficients.Length);

            GenericGFPoly forneySyndromes = new GenericGFPoly(field, syndromeCoefficients, false);


            return forneySyndromes;
        }

        internal GenericGFPoly runBerlekampMasseyAlgorithm(GenericGFPoly syndrome)
        {
            GenericGFPoly sigma = new GenericGFPoly(field, new int[] { 1 }, false);
            GenericGFPoly old = new GenericGFPoly(field, new int[] { 1 }, false);

            for (int i = 0; i < (syndrome.Coefficients.Length); i++)
            {
                int delta = syndrome.getCoefficient(i);
                for (int j = 1; j < sigma.Coefficients.Length; j++)
                {
                    delta ^= field.multiply(sigma.getCoefficient(j), syndrome.getCoefficient(i - j));
                }

                List<int> oldList = new List<int>(old.Coefficients);
                oldList.Add(0);
                old = new GenericGFPoly(field, oldList.ToArray(), false);

                if (delta != 0)
                {
                    if (old.Coefficients.Length > sigma.Coefficients.Length)
                    {
                        GenericGFPoly new_loc = old.multiply(delta);
                        old = sigma.multiply(field.inverse(delta));
                        sigma = new_loc;
                    }

                    sigma = sigma.addOrSubtract(old.multiply(delta));
                }
            }

            List<int> sigmaList = new List<int>(sigma.Coefficients);
            while (Convert.ToBoolean(sigmaList.Count) && sigmaList[0] == 0)
            {
                sigmaList.RemoveAt(0);
            }

            sigma = new GenericGFPoly(field, sigmaList.ToArray(), false);

            return sigma;
        }

        private GenericGFPoly findErrorLocator(int[] errorPositions)
        {
            GenericGFPoly errataLocator = new GenericGFPoly(field, new int[] { 1 }, false);

            foreach (int i in errorPositions)
            {
                errataLocator = errataLocator.multiply(new GenericGFPoly(field, new int[] { 1 }, false).addOrSubtract(new GenericGFPoly(field, new int[] { field.exp(i), 0 }, false)));
            }

            return errataLocator;
        }

        private GenericGFPoly findErrorEvaluator(GenericGFPoly syndrome, GenericGFPoly errataLocations)
        {
            int[] product = syndrome.multiply(errataLocations).Coefficients;

            int[] target = new int[errataLocations.Coefficients.Length - 1];

            Array.Copy(product, product.Length - errataLocations.Coefficients.Length + 1, target, 0, target.Length);

            if (target.Length == 0)
            {
                return null;
            }

            GenericGFPoly omega = new GenericGFPoly(field, target, false);

            return omega;
        }

        private int[] findErrorLocations(GenericGFPoly errorLocator)
        {
            // This is a direct application of Chien's search
            int numErrors = errorLocator.Degree;
            if (numErrors == 1)
            {
                // shortcut
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

        private int[] findErrorMagnitudes(GenericGFPoly errorEvaluator, int[] errorLocations)
        {
            // This is directly applying Forney's Formula
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
                        denominator = field.multiply(denominator, GenericGF.addOrSubtract(1, field.multiply(errorLocations[j], xiInverse)));
                    }
                }

                if (denominator == 0)
                {
                    return null;
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
