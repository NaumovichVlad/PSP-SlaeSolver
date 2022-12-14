namespace Core.Methods.Linear
{
    public interface ISlaeSolverLinear
    {
        double[] Solve(double[][] a, double[] b);
    }
}
