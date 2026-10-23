using APSIM.Core;
using APSIM.Workflow;
using Microsoft.Extensions.Logging.Abstractions;
using Models.Core;
using Models.Factorial;
using Models.Storage;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace UnitTests.APSIMWorkflow
{
    /// <summary>Tests for <see cref="SplitJsonGenerator"/>.</summary>
    [TestFixture]
    public class SplitJsonGeneratorTests
    {
        private string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(tempDir, true);
        }

        [Test]
        public void Generate_CreatesOneGroupPerExperiment()
        {
            SplittingRules rules = SplitJsonGenerator.Generate("Crop", new[] { "T1", "T10" });

            Assert.That(rules.OutputPath, Is.EqualTo("temp/"));
            Assert.That(rules.CopyWeatherFiles, Is.False);
            Assert.That(rules.CopyObservedData, Is.False);
            Assert.That(rules.Groups.Select(g => g.Name), Is.EqualTo(new[] { "Crop-T1", "Crop-T10" }));
            Assert.That(rules.Groups[0].Experiments, Is.EqualTo(new[] { "T1" }));
            Assert.That(rules.Groups[1].Experiments, Is.EqualTo(new[] { "T10" }));
            Assert.That(rules.Groups.All(g => g.Folders.Count == 0 && g.Simulations.Count == 0));
        }

        [Test]
        public void Generate_SanitisesInvalidCharacters()
        {
            SplittingRules rules = SplitJsonGenerator.Generate("Crop", new[] { "a/b:c" });

            Assert.That(rules.Groups[0].Name, Is.EqualTo("Crop-a_b_c"));
            Assert.That(rules.Groups[0].Experiments, Is.EqualTo(new[] { "a/b:c" }));
        }

        [Test]
        public void Generate_MakesGroupNamesUnique()
        {
            SplittingRules rules = SplitJsonGenerator.Generate("Crop", new[] { "a/b", "a:b", "a_b" });

            Assert.That(rules.Groups.Select(g => g.Name).Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void Run_WritesSplitJsonWithAllEnabledExperiments()
        {
            string path = Path.Combine(tempDir, "Test.apsimx");
            Simulations sims = new Simulations()
            {
                Children = new()
                {
                    new DataStore(),
                    new Experiment() { Name = "Exp1" },
                    new Experiment() { Name = "Exp2" },
                    new Experiment() { Name = "Disabled", Enabled = false }
                }
            };
            Node.Create(sims);
            sims.Write(path);
            sims.Node.FindChild<DataStore>().Close();

            string splitPath = SplitJsonGenerator.Run(path, NullLogger.Instance);

            Assert.That(splitPath, Is.EqualTo(Path.Combine(tempDir, "split.json")));
            SplittingRules rules = JsonSerializer.Deserialize<SplittingRules>(File.ReadAllText(splitPath));
            Assert.That(rules.Groups.Select(g => g.Name), Is.EqualTo(new[] { "Test-Exp1", "Test-Exp2" }));
        }

        [Test]
        public void Run_FileWithoutExperiments_WritesNothing()
        {
            string path = Path.Combine(tempDir, "Test.apsimx");
            Simulations sims = new Simulations()
            {
                Children = new()
                {
                    new DataStore(),
                    new Simulation() { Name = "Sim1" }
                }
            };
            Node.Create(sims);
            sims.Write(path);
            sims.Node.FindChild<DataStore>().Close();

            string splitPath = SplitJsonGenerator.Run(path, NullLogger.Instance);

            Assert.That(splitPath, Is.Null);
            Assert.That(File.Exists(Path.Combine(tempDir, "split.json")), Is.False);
        }
    }
}
