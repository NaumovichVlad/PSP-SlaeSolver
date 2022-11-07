using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Methods.Linear
{
    public class GaussMethodSolverLinear : ISlaeSolverLinear
    {
        public double[] Solve(double[][] a, double[] b)
        {
            var n = b.Length;
            var x = new double[n];

            for (var i = 0; i < n - 1; i++)
            {
                var mainRowIndex = FindMainElement(a, i);

                SwapRows(a, b, mainRowIndex, i);
                ExecuteForwardPhaseIteration(a, b, i);
            }

            for (var i = n - 1; i >= 0; i--)
            {
                ExecuteBackPhaseIteration(a, b, x, i);
            }

            return x;
        }

        private void ExecuteForwardPhaseIteration(double[][] matrix, double[] vector, int iteration)
        {
            var n = vector.Length;

            for (var i = iteration + 1; i < n; i++)
            {
                for (var j = iteration + 1; j < n; j++)
                {
                    matrix[i][j] = matrix[i][j] - matrix[iteration][j] * (matrix[i][iteration] / matrix[iteration][iteration]);
                }

                vector[i] = vector[i] - vector[iteration] * matrix[i][iteration] / matrix[iteration][iteration];
            }
        }

        protected virtual void ExecuteForwardPhaseIteration(
            double[][] matrix, double[] mainRow, double[] vector, double mainVector, int iteration, int rowsComplited = 0)
        {
            var n = vector.Length;

            for (var i = rowsComplited; i < n; i++)
            {
                for (var j = iteration + 1; j < n; j++)
                {
                    matrix[i][j] = matrix[i][j] - mainRow[j] * (matrix[i][iteration] / mainRow[iteration]);
                }

                vector[i] = vector[i] - mainVector * matrix[i][iteration] / mainRow[iteration];
            }
        }

        private void ExecuteBackPhaseIteration(double[][] matrix, double[] vector, double[] answers, int iteration)
        {
            double sum = 0;
            var n = vector.Length;

            for (var j = iteration + 1; j < n; j++)
            {
                sum += matrix[iteration][j] * answers[j];
            }

            answers[iteration] = (vector[iteration] - sum) / matrix[iteration][iteration];
        }

        protected int FindMainElement(double[][] matrix, int iteration)
        {
            var max = Math.Abs(matrix[iteration][iteration]);
            var maxIndex = iteration;

            for (var i = iteration + 1; i < matrix.GetLength(0); i++)
            {
                if (Math.Abs(matrix[i][iteration]) > max)
                {
                    max = Math.Abs(matrix[i][iteration]);
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        private void SwapRows(double[][] matrix, double[] vector, int mainRowIndex, int iteration)
        {
            for (var j = 0; j < vector.Length; j++)
            {
                var temp = matrix[iteration][j];

                matrix[iteration][j] = matrix[mainRowIndex][j];
                matrix[mainRowIndex][j] = temp;
            }

            var tmp = vector[iteration];

            vector[iteration] = vector[mainRowIndex];
            vector[mainRowIndex] = tmp;
        }
    }
}
