#nullable enable
using APSIM.Core;
using Microsoft.Extensions.Logging;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace APSIM.Workflow
{
    /// <summary>
    /// Generates a split.json file (one group per experiment) for an apsimx file.
    /// </summary>
    public static class SplitJsonGenerator
    {
        private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Generate a split.json file next to the apsimx file, overwriting any existing one.
        /// </summary>
        /// <param name="apsimFilepath">Path to the apsimx file.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The path of the split.json file, or null if the file has no experiments and so is not split.</returns>
        public static string? Run(string apsimFilepath, ILogger logger)
        {
            if (!File.Exists(apsimFilepath))
                throw new FileNotFoundException("Apsimx file not found.", apsimFilepath);

            Simulations sims = FileFormat.ReadFromFile<Simulations>(apsimFilepath).Model as Simulations
                ?? throw new Exception("File could not be loaded: " + apsimFilepath);

            List<string> experimentNames = sims.Node.FindChildren<Experiment>(recurse: true)
                                                     .Where(e => e.Enabled)
                                                     .Select(e => e.Name)
                                                     .ToList();

            // Loading the file opens the datastore database; release it.
            sims.Node.FindChild<DataStore>(recurse: true)?.Close();

            if (experimentNames.Count == 0)
            {
                logger.LogInformation("No experiments found in " + apsimFilepath + ". File will not be split.");
                return null;
            }

            SplittingRules rules = Generate(Path.GetFileNameWithoutExtension(apsimFilepath), experimentNames);

            string splitPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(apsimFilepath))!, "split.json");
            File.WriteAllText(splitPath, JsonSerializer.Serialize(rules, jsonOptions));
            logger.LogInformation("  created:" + splitPath);
            return splitPath;
        }

        /// <summary>
        /// Create splitting rules with one group per experiment.
        /// </summary>
        /// <param name="fileStem">Apsimx file name without extension, used to prefix group names.</param>
        /// <param name="experimentNames">Names of the experiments.</param>
        public static SplittingRules Generate(string fileStem, IEnumerable<string> experimentNames)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);
            List<SplittingGroup> groups = new();

            foreach (string experimentName in experimentNames.Distinct())
            {
                string cleaned = new string(experimentName.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
                string groupName = $"{fileStem}-{cleaned}";
                string unique = groupName;
                for (int i = 2; !usedNames.Add(unique); i++)
                    unique = $"{groupName}_{i}";

                groups.Add(new SplittingGroup
                {
                    Name = unique,
                    Folders = new List<string>(),
                    Experiments = new List<string> { experimentName },
                    Simulations = new List<string>()
                });
            }

            return new SplittingRules
            {
                OutputPath = "temp/",
                CopyWeatherFiles = false,
                CopyObservedData = false,
                Groups = groups
            };
        }
    }
}
