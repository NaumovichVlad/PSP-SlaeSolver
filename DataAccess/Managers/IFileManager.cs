namespace DataAccess.Managers
{
    public interface IFileManager
    {
        double[][] ReadMatrix(string filePathA);
        double[] ReadVector(string filePathB);
        void SaveResults(string resultsPath, double[] results);

        void SaveLoadTestingResults(double[] testingResults, string testingResultsPath);
        List<string> GetNodesAddresses(string nodesPath);
    }
}
