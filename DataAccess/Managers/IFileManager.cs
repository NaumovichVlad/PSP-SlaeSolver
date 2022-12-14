namespace DataAccess.Managers
{
    public interface IFileManager
    {
        double[][] ReadMatrix(string filePathA);
        double[] ReadVector(string filePathB);
        void SaveResults(string resultsPath, double[] results);
        List<string> GetNodesAddresses(string nodesPath);
    }
}
