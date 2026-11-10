using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;

namespace UnitTests.Soils
{
    [TestFixture]
    public class SwimBypassTests
    {
        private sealed class Surface : Model, ISurfaceOrganicMatter
        {
            public double Cover { get; set; }
            public List<ICanopy> Canopies { get; set; }

            public void Add(double biomass, double N, double P, string type, string name,
                            double fractionStanding = 0, double no3 = -1, double nh4 = -1)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class Scenario
        {
            public Swim3 Swim { get; set; }
            public Solute Nitrate { get; set; }
            public Irrigation Irrigation { get; set; }
            public Events Events { get; set; }
            public MockClock Clock { get; set; }
        }

        private static Scenario CreateScenario(double fraction, double depth = 250,
                                               double destinationWater = 0.25, double maxTimestep = 1440)
        {
            double[] thickness = { 100, 100, 200, 300 };
            double[] initialWater = { 0.25, 0.25, destinationWater, 0.25 };
            var physical = new Physical
            {
                Thickness = thickness,
                BD = new[] { 1.3, 1.3, 1.3, 1.3 },
                AirDry = new[] { 0.1, 0.1, 0.1, 0.1 },
                LL15 = new[] { 0.15, 0.15, 0.15, 0.15 },
                DUL = new[] { 0.30, 0.30, 0.30, 0.30 },
                SAT = new[] { 0.45, 0.45, 0.45, 0.45 },
                KS = new[] { 50.0, 50.0, 50.0, 50.0 }
            };
            var nitrate = new Solute
            {
                Name = "NO3",
                Thickness = thickness,
                InitialValues = new[] { 0.0, 0.0, 0.0, 0.0 },
                InitialValuesUnits = Solute.UnitsEnum.kgha,
                FIP = new[] { 1.0, 1.0, 1.0, 1.0 },
                Exco = new[] { 0.0, 0.0, 0.0, 0.0 }
            };
            var swim = new Swim3
            {
                Name = "Swim3",
                BypassFraction = fraction,
                BypassDepth = depth,
                CN2Bare = 0,
                KDul = 0.1,
                PSIDul = -100,
                DTMin = 0,
                DTMax = maxTimestep,
                MaxWaterIncrement = 5,
                SoluteSpaceWeightingFactor = 1,
                Disp = 1,
                A = 2,
                DTHC = 0.1,
                DTHP = 2
            };
            var irrigation = new Irrigation();
            var soil = new Soil
            {
                Children = new List<IModel>
                {
                    physical,
                    new Water { Thickness = thickness, InitialValues = initialWater },
                    nitrate,
                    swim
                }
            };
            var zone = new Zone
            {
                Area = 1,
                Children = new List<IModel> { irrigation, soil, new Surface() }
            };
            var clock = new MockClock { Today = new DateTime(2019, 1, 1) };
            var simulation = new Simulation
            {
                Children = new List<IModel>
                {
                    clock,
                    new Summary { Verbosity = MessageType.Error },
                    zone
                }
            };
            var simulations = new Simulations
            {
                Children = new List<IModel> { new DataStore { UseInMemoryDB = true }, simulation }
            };
            Node.Create(simulations);
            new Links().Resolve(simulation, true);
            var events = new Events(simulation);
            events.ConnectEvents();
            var args = new object[] { simulation, EventArgs.Empty };
            events.Publish("Commencing", args);
            events.Publish("StartOfSimulation", args);
            events.Publish("DoDailyInitialisation", args);
            return new Scenario { Swim = swim, Nitrate = nitrate, Irrigation = irrigation, Events = events, Clock = clock };
        }

        private static void RunDay(Scenario scenario, double rainfall = 0, double irrigation = 0, double nitrate = 0)
        {
            scenario.Swim.PotentialInfiltration = rainfall;
            if (irrigation > 0)
                scenario.Irrigation.Apply(irrigation, duration: 720, no3: nitrate, doOutput: false);
            scenario.Events.Publish("DoSoilWaterMovement", new object[] { scenario.Swim, EventArgs.Empty });
        }

        [Test]
        public void RainfallBypassMovesWaterDeeperAndConservesMass()
        {
            Scenario baseline = CreateScenario(0);
            Scenario bypass = CreateScenario(0.5);
            double initialWater = bypass.Swim.SWmm.Sum();

            RunDay(baseline, rainfall: 20);
            RunDay(bypass, rainfall: 20);

            Assert.That(bypass.Swim.BypassFlow, Is.EqualTo(10).Within(1e-5));
            Assert.That(bypass.Swim.SWmm[0], Is.LessThan(baseline.Swim.SWmm[0]));
            Assert.That(bypass.Swim.SWmm[2], Is.GreaterThan(baseline.Swim.SWmm[2]));
            Assert.That(bypass.Swim.SWmm.Sum() + bypass.Swim.Pond + bypass.Swim.Runoff +
                        bypass.Swim.Drainage + bypass.Swim.Es - initialWater, Is.EqualTo(20).Within(1e-3));
            Assert.That(baseline.Swim.BypassFlow, Is.Zero);
        }

        [Test]
        public void IrrigationSoluteFollowsBypassAndConservesMass()
        {
            Scenario baseline = CreateScenario(0);
            Scenario bypass = CreateScenario(0.5);

            RunDay(baseline, irrigation: 20, nitrate: 10);
            RunDay(bypass, irrigation: 20, nitrate: 10);

            Assert.That(bypass.Swim.BypassFlow, Is.EqualTo(10).Within(1e-5));
            Assert.That(bypass.Nitrate.kgha[2], Is.GreaterThan(baseline.Nitrate.kgha[2]));
            Assert.That(bypass.Nitrate.kgha.Sum() + bypass.Swim.LeachNO3, Is.EqualTo(10).Within(1e-3));
        }

        [Test]
        public void AllIrrigationCanBypassWithItsSolute()
        {
            Scenario bypass = CreateScenario(1);
            RunDay(bypass, irrigation: 20, nitrate: 10);

            Assert.That(bypass.Swim.BypassFlow, Is.EqualTo(20).Within(1e-5));
            Assert.That(bypass.Nitrate.kgha[2], Is.GreaterThan(9));
            Assert.That(bypass.Nitrate.kgha.Sum() + bypass.Swim.LeachNO3, Is.EqualTo(10).Within(1e-3));
        }

        [Test]
        public void NearlySaturatedDestinationLimitsBypass()
        {
            Scenario bypass = CreateScenario(1, destinationWater: 0.4499);
            double initialWater = bypass.Swim.SWmm.Sum();

            RunDay(bypass, rainfall: 20);

            Assert.That(bypass.Swim.BypassFlow, Is.LessThan(20));
            Assert.That(bypass.Swim.SWmm.Sum() + bypass.Swim.Pond + bypass.Swim.Runoff +
                        bypass.Swim.Drainage + bypass.Swim.Es - initialWater, Is.EqualTo(20).Within(1e-3));
        }

        [Test]
        public void CurveNumberRunoffIsRemovedBeforeBypass()
        {
            Scenario bypass = CreateScenario(0.5);
            bypass.Swim.CN2Bare = 90;
            bypass.Swim.SetSurfaceBCForCurveNumber();
            double initialWater = bypass.Swim.SWmm.Sum();

            RunDay(bypass, rainfall: 50);

            Assert.That(bypass.Swim.Runoff, Is.GreaterThan(0));
            Assert.That(bypass.Swim.BypassFlow, Is.LessThan(25));
            Assert.That(bypass.Swim.SWmm.Sum() + bypass.Swim.Pond + bypass.Swim.Runoff +
                        bypass.Swim.Drainage + bypass.Swim.Es - initialWater, Is.EqualTo(50).Within(1e-3));
        }

        [Test]
        public void BypassIsStableAcrossTimestepLimits()
        {
            Scenario longStep = CreateScenario(0.5);
            Scenario shortStep = CreateScenario(0.5, maxTimestep: 30);
            RunDay(longStep, rainfall: 20);
            RunDay(shortStep, rainfall: 20);

            Assert.That(shortStep.Swim.BypassFlow, Is.EqualTo(longStep.Swim.BypassFlow).Within(1e-4));
            Assert.That(shortStep.Swim.SWmm[2], Is.EqualTo(longStep.Swim.SWmm[2]).Within(0.5));
        }

        [Test]
        public void DailyBypassOutputIsResetOnDryDay()
        {
            Scenario scenario = CreateScenario(0.5);
            RunDay(scenario, rainfall: 20);
            Assert.That(scenario.Swim.BypassFlow, Is.GreaterThan(0));

            scenario.Clock.Today = scenario.Clock.Today.AddDays(1);
            scenario.Events.Publish("DoDailyInitialisation", new object[] { scenario.Swim, EventArgs.Empty });
            RunDay(scenario);

            Assert.That(scenario.Swim.BypassFlow, Is.Zero);
        }

        [TestCase(-0.1, 250, "BypassFraction")]
        [TestCase(1.1, 250, "BypassFraction")]
        [TestCase(double.NaN, 250, "BypassFraction")]
        [TestCase(0.5, 50, "BypassDepth")]
        [TestCase(0.5, 500, "BypassDepth")]
        [TestCase(0.5, double.NaN, "BypassDepth")]
        public void RejectsInvalidParameters(double fraction, double depth, string message)
        {
            Exception error = Assert.Catch<Exception>(() => CreateScenario(fraction, depth));
            Assert.That(error.ToString(), Does.Contain(message));
        }

        [Test]
        public void ChecksParametersChangedAfterInitialisation()
        {
            Scenario scenario = CreateScenario(0);
            scenario.Swim.BypassFraction = 0.5;
            scenario.Swim.BypassDepth = 50;

            Exception error = Assert.Catch<Exception>(() => RunDay(scenario, rainfall: 20));
            Assert.That(error.ToString(), Does.Contain("BypassDepth"));
        }

        [Test]
        public void RejectsSubsurfaceIrrigation()
        {
            Scenario scenario = CreateScenario(0.5);

            Exception error = Assert.Catch<Exception>(() => scenario.Irrigation.Apply(10, depth: 100, doOutput: false));
            Assert.That(error.ToString(), Does.Contain("subsurface irrigation"));
        }

        [Test]
        public void RejectsBypassEnabledAfterSubsurfaceIrrigation()
        {
            Scenario scenario = CreateScenario(0);
            scenario.Irrigation.Apply(10, depth: 100, doOutput: false);
            scenario.Swim.BypassFraction = 0.5;

            Exception error = Assert.Catch<Exception>(() => RunDay(scenario));
            Assert.That(error.ToString(), Does.Contain("subsurface irrigation"));
        }

        [Test]
        public void RejectsConstantPotentialSurfaceBoundary()
        {
            Scenario scenario = CreateScenario(0.5);
            scenario.Swim.SetTopBCForConstantPotential();

            Exception error = Assert.Catch<Exception>(() => RunDay(scenario, rainfall: 20));
            Assert.That(error.ToString(), Does.Contain("constant-potential surface boundary"));
        }
    }
}
