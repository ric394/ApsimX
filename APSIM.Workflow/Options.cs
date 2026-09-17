using CommandLine;

namespace APSIM.Workflow;

/// <summary>
/// Specifies the command line options for the APSIM.Workflow application.
/// </summary>
public class Options
{
    /// <summary>
    /// Gets or sets a value indicating whether verbose output is enabled.
    /// </summary>
    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets the input file to be processed.
    /// </summary>
    [Option('d', "payload-directory", Required = false, HelpText = "Directory path where a WorkFlo payload directory is located. Will typically contain a workflow.yml and .env file.")]
    public string DirectoryPath { get; set; } = "";

    /// <summary>
    /// Gets or sets a value indicating whether the program should print the absolute paths of valid validation directories.
    /// </summary>
    [Option('l', "locations", Required = false, HelpText = "Print the absolute paths of valid validation directories.")]
    public bool ValidationLocations { get; set; }

    /// <summary> Github author ID for the pull request. </summary>
    [Option('g', "githubauthorid", Required = false, HelpText = "The pull requests author GitHub username")]
    public string GitHubAuthorID { get; set; } = "";

    /// <summary> Docker image tag for a pull request used to validate data.</summary>
    [Option('t', "tag", Required = false, HelpText = "The docker image tag to use.")]
    public string DockerImageTag { get; set; } = "latest";

    /// <summary>File to split</summary>
    [Option('s', "splitfiles", Required = false, HelpText = "Apsimx file to split.")]
    public string SplitFiles { get; set; }

    /// <summary>
    /// Gets or sets the path to the APSIMX file in a docker container.
    /// </summary>
    [Option('p', "validation-path", Required = false, HelpText = "The path to a directory containing APSIMX files in the docker container.")]
    public string ValidationPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the commit SHA to use for the workflow.
    /// This is typically the SHA of the commit that triggered the workflow run.
    /// </summary>
    [Option('c', "commit-sha", Required = false, HelpText = "The commit SHA to use for the workflow.")]
    public string CommitSHA { get; set; } = "";

    /// <summary>
    /// Gets or sets the pull request number for the workflow.
    /// This is typically the number of the pull request that triggered the workflow run.
    /// </summary>
    [Option('n', "pr-number", Required = false, HelpText = "The pull request number for the workflow.")]
    public string PullRequestNumber { get; set; } = "";

    /// <summary> Gets the number of simulations/validation locations available. </summary>
    [Option("sim-count", Required = false, HelpText = "The number of simulations/validation locations available.")]
    public bool SimulationCount { get; set; } = false;

    /// <summary> Azure pool to use for the workflow. </summary>
    [Option("azure-pool", Required = false, HelpText = "The Azure pool to use for the workflow.")]
    public string AzurePool { get; set; } = "workflo-pool";

    /// <summary>
    /// Azure Account URL
    /// </summary>
    [Option("account-url", Required= false, HelpText ="Azure account URL.")]
    public string AzureAccountURL {get;set;} = "";

    /// <summary>
    /// Azure Account name
    /// </summary>
    [Option("account-name", Required= false, HelpText ="Azure account name.")]
    public string AzureAccountName {get;set;} = "";


    /// <summary>
    /// Primary access key for the Azure account.
    /// </summary>
    [Option("azure-access-key", Required= false, HelpText ="Azure primary access key.")]
    public string PrimaryAccessKey {get;set;} = "";


    /// <summary>
    /// Admin password for an Azure batch pool.
    /// </summary>
    [Option("pool-admin-password", Required= false, HelpText ="Admin password for an Azure Batch pool.")]
    public string PoolAdminPassword {get;set;} = "";

    /// <summary>
    /// Azure Account URL
    /// </summary>
    [Option("vm-size", Required= false, HelpText ="The valid Azure VM name.")]
    public string VMSize {get;set;} = "Standard_D4d_v5";


    /// <summary>
    /// Azure storage name
    /// </summary>
    [Option("storage-name", Required= false, HelpText ="The Azure storage account name.")]
    public string StorageAccountName {get;set;} = "";

    /// <summary>
    /// Storage connection string
    /// </summary>
    [Option("storage-connection-string", Required= false, HelpText ="The storage connection string.")]
    public string StorageConnectionString {get;set;} = ""; 

    /// <summary>
    /// An env file represented as a string.
    /// </summary>
    [Option("env-string", Required= false, HelpText ="An env file represented as a string.")]
    public string EnvString {get;set;} = "";  

    /// <summary>
    /// A unique job name
    /// </summary>
    [Option("job-name", Required= false, HelpText ="An unique job name.")]
    public string JobName {get;set;} = "";  

    /// <summary>
    /// A unique job name
    /// </summary>
    [Option("key-one", Required= false, HelpText ="An Azure batch account key one or key two")]
    public string KeyOne {get;set;} = "";     
    


}
    
