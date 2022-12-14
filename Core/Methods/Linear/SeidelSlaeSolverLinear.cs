using System.Diagnostics;

namespace Core.Methods.Linear
{
    public class SeidelSlaeSolverLinear : ISlaeSolverLinear
    {
        private double _eps = Math.Pow(10, -9);
        public double[] Solve(double[][] a, double[] b)
        {
            var d = new double[a.Length];
            var result = new double[a.Length];
            double dMax = _eps + 1;

            while (dMax > _eps)
            {
                Debug.Print(dMax.ToString());
                for (var i = 0; i < a.Length; ++i)
                {
                    d[i] = result[i];
                    result[i] = b[i];

                    for (var j = 0; j < a.Length; ++j)
                    {
                        if (i != j)
                        {
                            result[i] -= a[i][j] * result[j];
                        }
                    }

                    result[i] = result[i] / a[i][i];
                    d[i] = Math.Abs(d[i] - result[i]);
                }

                dMax = d.Max();
            }

            return result;
        }
    }
}
