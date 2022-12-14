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
    }
}
