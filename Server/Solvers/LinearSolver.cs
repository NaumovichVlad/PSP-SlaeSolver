using Core.Methods.Linear;
using DataAccess.Managers;
using Server.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Solvers
{
    public class LinearSolver
    {
        private readonly ISlaeSolverLinear _solver;
        private readonly IFileManager _fileManager;
        private readonly ITimeLogger _timeLogger;

        public LinearSolver(ISlaeSolverLinear solver, IFileManager fileManager, ITimeLogger timeLogger)
        {
            _solver = solver;
            _fileManager = fileManager;
            _timeLogger = timeLogger;
        }

        public void Solve(string pathA, string pathB, string pathRes)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            var matrix = _fileManager.ReadMatrix(pathA);
            var vector = _fileManager.ReadVector(pathB);

            stopwatch.Stop();

            _timeLogger.ReadingTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            stopwatch.Start();

            var result = _solver.Solve(matrix, vector);

            stopwatch.Stop();

            _timeLogger.SolvingTime = stopwatch.ElapsedMilliseconds;
        }

        public string GetTimeLog()
        {
            return _timeLogger.GetLog();
        }

    }
}
