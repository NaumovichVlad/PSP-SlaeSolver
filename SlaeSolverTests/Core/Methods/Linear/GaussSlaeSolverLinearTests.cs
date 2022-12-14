using Core.Methods.Linear;
using DataAccess.Managers;

namespace SlaeSolverTests.Core.Methods.Linear
{
    [TestClass]
    public class GaussSlaeSolverLinearTests
    {
        private readonly IFileManager _reader = new FileManagerTxt();

        [TestMethod]
        public void Solve_Test1()
        {
            ISlaeSolverLinear solver = new GaussMethodSolverLinear();

            var a = new double[3][]
            {
                new double[3] { 4, -1, 1 },
                new double[3] { 2, 6, -1 },
                new double[3] { 1, 2, -3 },
            };

            var b = new double[3] { 4, 7, 0 };
            var expected = new double[3] { 1, 0.999999, 0.999999 };
            var actual = solver.Solve(a, b);

            Assert.AreEqual(expected.Length, actual.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(Math.Abs(expected[i] - actual[i]) < 0.00001);
            }
        }

        [TestMethod]
        public void Solve_Test2()
        {
            var a = _reader.ReadMatrix("../../../../../TestData/test1.A");
            var b = _reader.ReadVector("../../../../../TestData/test1.B");
            var expected = _reader.ReadVector("../../../../../TestData/test1.des");

            Test(a, b, expected);
        }

        [TestMethod]
        public void Solve_Test3()
        {
            var a = _reader.ReadMatrix("../../../../../TestData/test2.A");
            var b = _reader.ReadVector("../../../../../TestData/test2.B");
            var expected = _reader.ReadVector("../../../../../TestData/test2.des");

            Test(a, b, expected);
        }

        private void Test(double[][] a, double[] b, double[] expected)
        {
            ISlaeSolverLinear solver = new GaussMethodSolverLinear();

            var actual = solver.Solve(a, b);

            Assert.AreEqual(expected.Length, actual.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(Math.Abs(expected[i] - actual[i]) < Math.Pow(10, -5));
            }
        }
    }
}
