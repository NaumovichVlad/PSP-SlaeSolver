using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Managers
{
    public interface IFileManager
    {
        double[][] ReadMatrix(string filePathA);
        double[] ReadVector(string filePathB);
        void SaveResults(string resultsPath, double[] results);
    }
}
