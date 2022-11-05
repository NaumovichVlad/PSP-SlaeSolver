using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Methods.Linear
{
    public class GaussMethodSolverLinear : GaussMethodSolverBase
    {
        public override double[] Solve(double[][] a, double[] b)
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
    }
}
