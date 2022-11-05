using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Methods.Linear.Factories
{
    public static class LinearSolverFactory
    {
        public static ISlaeSolverLinear GetSolver(SlaeSolverMethodsLinear method)
        {
            ISlaeSolverLinear solver;

            switch (method)
            {
                case SlaeSolverMethodsLinear.Gauss:
                    solver = new GaussMethodSolverLinear();
                    break;
                case SlaeSolverMethodsLinear.Seidel:
                    solver = new SeidelSlaeSolverLinear();
                    break;
                default:
                    solver = null;
                    break;
            }

            return solver;
        }
    }
}
