using System.Text.RegularExpressions;

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

        public void SaveResults(string resultsPath, double[] results)
        {
            using (var sw = new StreamWriter(resultsPath))
            {
                foreach (var element in results)
                {
                    sw.WriteLine($"\t{element}");
                }
            }
        }

        public List<string> GetNodesAddresses(string nodesPath)
        {
            var nodes = new List<string>();
            string? line;

            using (var sr = new StreamReader(nodesPath))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    nodes.Add(line.Trim());
                }
            }

            return nodes;
        }

        private double[] ParseLine(string line)
        {
            var pattern = $"\\s+";
            var elements = Regex.Split(line.Trim(), pattern);

            return elements.Select(e => double.Parse(e)).ToArray();
        }
    }
}
