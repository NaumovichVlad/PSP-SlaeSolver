using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Methods.Linear
{
    public interface ISlaeSolverLinear
    {
        double[] Solve(double[][] a, double[] b);
    }
}
