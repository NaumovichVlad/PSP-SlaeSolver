using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataAccess.Managers
{
    public class FileManagerTxt : IFileManager
    {
        public double[][] ReadMatrix(string filePathA)
        {
            var matrix = new List<double[]>();
            string? line;

            using (var sr = new StreamReader(filePathA))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    matrix.Add(ParseLine(line));
                }
            }

            return matrix.ToArray();
        }

        public double[] ReadVector(string filePathB)
        {
            var vector = new List<double>();
            string? line;

            using (var sr = new StreamReader(filePathB))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    vector.Add(double.Parse(line.Trim()));
                }
            }

            return vector.ToArray();
        }

        private double[] ParseLine(string line)
        {
            var pattern = $"\\s+";
            var elements = Regex.Split(line.Trim(), pattern);

            return elements.Select(e => double.Parse(e)).ToArray();
        }
    }
}
