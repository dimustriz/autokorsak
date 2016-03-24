namespace ShellBasics
{
    public class ShellNameMapping
    {
        private string destinationPath;
        private string renamedDestinationPath;

        public ShellNameMapping(string OldPath, string NewPath)
        {
            destinationPath = OldPath;
            renamedDestinationPath = NewPath;
        }

        public string DestinationPath
        {
            get { return destinationPath; }
        }

        public string RenamedDestinationPath
        {
            get { return renamedDestinationPath; }
        }
    }
}
