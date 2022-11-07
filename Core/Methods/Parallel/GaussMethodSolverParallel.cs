using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Methods.Parallel
{
    public class GaussMethodSolverParallel
    {
        protected void ExecuteForwardPhaseIteration(
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

        protected double[] ExecuteBackPhaseIteration(double[][] matrix, double[] vector)
        {
            
            var n = vector.Length;
            var answers = new double[n];

            for (var i = n - 1; i >= 0; i--)
            {
                double sum = 0;

                for (var j = i + 1; j < n; j++)
                {
                    sum += matrix[i][j] * answers[j];
                }

                answers[i] = (vector[i] - sum) / matrix[i][i];
            }

            return answers;
        }

        protected int FindMainElement(double[][] matrix, int iteration, int shift = 0)
        {
            var max = Math.Abs(matrix[shift][iteration]);
            var maxIndex = shift;
            for (var i = shift; i < matrix.GetLength(0); i++)
            {
                if (Math.Abs(matrix[i][iteration]) > max)
                {
                    max = Math.Abs(matrix[i][iteration]);
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        protected void SwapRows(double[][] matrix, double[] vector, int mainRowIndex, int iteration)
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
