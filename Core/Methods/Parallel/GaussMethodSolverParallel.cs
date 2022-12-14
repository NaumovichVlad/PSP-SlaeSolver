namespace Core.Methods.Parallel
{
    public class GaussMethodSolverParallel
    {
        protected void ExecuteForwardPhaseIteration(
            double[][] matrix, double[] mainRow, double[] vector, double mainVector, int iteration, int rowsComplited)
        {
            var n = vector.Length;

            for (var i = rowsComplited; i < n; i++)
            {
                for (var j = iteration + 1; j < matrix[i].Length; j++)
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
    }
}
