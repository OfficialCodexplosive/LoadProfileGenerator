﻿#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Automation;
using Automation.ResultFiles;
using Common;
using Common.JSON;
using Common.Tests;
using Database;
using Database.DatabaseMerger;
using Database.Helpers;
using Database.Tests;
using FluentAssertions;
using SimulationEngineLib;
using Xunit;
using Xunit.Abstractions;


namespace SimulationEngine.Tests {
    [SuppressMessage("ReSharper", "RedundantNameQualifier")]
    public class ProgramTests : UnitTestBaseClass
    {

        [JetBrains.Annotations.NotNull]
        public static WorkingDir SetupDB3([JetBrains.Annotations.NotNull] string name, bool clearTemplatedFirst = false) {
            var srcPath = DatabaseSetup.GetSourcepath(null);
            Config.CatchErrors = false;
            var wd = new WorkingDir(name);
            var dstfullName = Path.Combine(wd.WorkingDirectory, "profilegenerator.db3");
            Config.IsInUnitTesting = true;
            if (File.Exists("profilegenerator.db3")) {
                File.Delete("profilegenerator.db3");
            }
            File.Copy(srcPath, dstfullName);

            Directory.SetCurrentDirectory(wd.WorkingDirectory);
            Logger.Info("Using directory:" + Directory.GetCurrentDirectory());
            var connectionString = "Data Source=" + dstfullName;
            // ReSharper disable once UseObjectOrCollectionInitializer
            Simulator sim = new Simulator(connectionString);
            sim.MyGeneralConfig.DeviceProfileHeaderMode = DeviceProfileHeaderMode.Standard;
            if (clearTemplatedFirst) {
                sim.FindAndDeleteAllTemplated();
            }
            DatabaseVersionChecker.CheckVersion(connectionString);
            Logger.Info("Checked database version. It is ok.");
            return wd;
        }

        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.ManualOnly)]
        public void CSVImportTest()
        {
            using (var db1 = new DatabaseSetup(Utili.GetCurrentMethodAndClass() + "_export"))
            {
                //export
                using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
                {
                    var sim = new Simulator(db1.ConnectionString);
                    ModularHouseholdSerializer.ExportAsCSV(sim.ModularHouseholds[0], sim,
                        Path.Combine(wd.WorkingDirectory, "testexportfile.csv"));
                    //import

                    var arguments = new List<string>
            {
                "--ImportHouseholdDefinition",
                "testexportfile.csv"
            };
                    MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
                    db1.Cleanup();
                    wd.CleanUp(1);
                }
            }
        }

        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.ManualOnly)]
        public void CSVImportTest2()
        {
            using (var db1 = new DatabaseSetup(Utili.GetCurrentMethodAndClass() + "_export"))
            {
                //export
                using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
                {
                    const string srcfile = @"v:\work\CHR15a_Sc1.csv";
                    File.Copy(srcfile, Path.Combine(wd.WorkingDirectory, "hh.csv"));
                    var sim = new Simulator("Data Source=profilegenerator.db3") { MyGeneralConfig = { CSVCharacter = ";" } };
                    sim.MyGeneralConfig.SaveToDB();
                    var dbm = new DatabaseMerger(sim);
                    const string importPath = @"v:\work\profilegenerator_hennings.db3";
                    dbm.RunFindItems(importPath, null);
                    dbm.RunImport(null);
                    ModularHouseholdSerializer.ExportAsCSV(sim.ModularHouseholds[0], sim,
                        Path.Combine(wd.WorkingDirectory, "testexportfile.csv"));
                    //import

                    var arguments = new List<string>
            {
                "--ImportHouseholdDefinition",
                "hh.csv"
            };
                    MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
                    db1.Cleanup();
                    wd.CleanUp(1);
                }
            }
        }

        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        public void ListEnviromentalVariables() {
            Config.CatchErrors = false;
            var dict = Environment.GetEnvironmentVariables();
#pragma warning disable CS8605 // Unboxing a possibly null value.
            foreach (DictionaryEntry env in dict) {
#pragma warning restore CS8605 // Unboxing a possibly null value.
                var name = (string) env.Key;
                var value =( (string?) env.Value)??throw new LPGException("value was null");
                Logger.Info(name + " = " + value);
            }
        }

        //[Fact]
        //[Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        //public void MainListLoadTypePriorities()
        //{
        //    using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
        //    {
        //        var arguments = new List<string>
        //    {
        //        "List",
        //        "-LoadtypePriorities"
        //    };
        //        MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
        //        wd.CleanUp(1);
        //    }
        //}

        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.ManualOnly)]
        public void MainTestBatchCommandlineModularHouseholds()
        {
            using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
            {
                var arguments = new List<string>();
                var dstpath = Path.Combine(wd.WorkingDirectory, "calc");
                var args =
                    "Calculate -CalcObjectType ModularHousehold -CalcObjectNumber 0 -OutputFileDefault ReasonableWithChartsAndPDF " +
                    "-StartDate 01.01.2015 -EndDate 10.01.2015 -SkipExisting -OutputDirectory " +
                    dstpath;
                arguments.AddRange(args.Split(' '));
                arguments = arguments.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
                wd.CleanUp(1);
            }
        }

        /*
        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        public void MainTestBatchModularHouseholds() {
            var wd = SetupDB3(Utili.GetCurrentMethodAndClass());
            const string filename = "Start-ModularHousehold.cmd";
            if (File.Exists(filename)) {
                File.Delete(filename);
            }
            var arguments = new List<string>
            {
                "TestBatch",
                "-ModularHouseholds"
            };
            Program.Main(arguments.ToArray());
            (File.Exists(filename)).Should().BeTrue();
            wd.CleanUp(1);
        }*/
        /*
        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        public void MainTestBatchSettlements() {
            const string filename = "Start-Settlement.cmd";
            if (File.Exists(filename)) {
                File.Delete(filename);
            }
            var wd = SetupDB3(nameof(MainTestBatchSettlements));
            var arguments = new List<string>
            {
                "TestBatch",
                "-Settlements"
            };
            Program.Main(arguments.ToArray());
            (File.Exists(filename)).Should().BeTrue();
            wd.CleanUp(1);
        }*/

        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        public void MainTestForHelp()
        {
            using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
            {
                var args = Array.Empty<string>();
                MainSimEngine.Run(args.ToArray(), "simulationengine.exe");
                wd.CleanUp(1);
            }
        }

        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.ManualOnly)]
        public void MainTestLaunchParallelModularHouseholds()
        {
            Config.CatchErrors = false;
            var di = new DirectoryInfo(".");
            var fis = di.GetFiles("*.*");
            var oldDir = Directory.GetCurrentDirectory();
            var db3Path = Path.Combine(oldDir, "profilegenerator-latest.db3");
            using (var wd = new WorkingDir("ModularBatchPar"))
            {
                Thread.Sleep(1000);
                var prevDir = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(wd.WorkingDirectory);
                foreach (var fi in fis)
                {
                    var dstpath = Path.Combine(wd.WorkingDirectory, fi.Name);
                    fi.CopyTo(dstpath);
                }
                if (File.Exists("profilegenerator.db3"))
                {
                    File.Delete("profilegenerator.db3");
                }
                File.Copy(db3Path, "profilegenerator.db3");
                const string filename = "Start-ModularHousehold.cmd";
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                var arguments = new List<string>
            {
                "--Batch-ModularHouseholds"
            };
                MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
                File.Exists(filename).Should().BeTrue();
                arguments.Clear();
                arguments.Add("--LaunchParallel");
                arguments.Add("--NumberCores");
                arguments.Add("4");
                arguments.Add("--Batchfile");
                arguments.Add("Start-ModularHousehold.cmd");
                MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
                Directory.SetCurrentDirectory(prevDir);
                wd.CleanUp(1);
            }
        }

        //[Fact]
        //[Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        //public void MainTestListGeoLocs()
        //{
        //    using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
        //    {
        //        var arguments = new List<string>
        //    {
        //        "List",
        //        "-GeographicLocations"
        //    };
        //        MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
        //        wd.CleanUp(1);
        //    }
        //}

        //[Fact]
        //[Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        //public void ProfilesGeoLocs()
        //{
        //    using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
        //    {
        //        var arguments = new List<string>
        //    {
        //        "Calculate",
        //        "-CalcObjectType",
        //        "ModularHousehold",
        //        "-CalcObjectNumber",
        //        "1",
        //        "-OutputDirectory",
        //        Path.Combine(wd.WorkingDirectory,"Results"),
        //        "-LoadtypePriority",
        //        "Mandatory",
        //        "-SkipExisting",
        //        "-MeasureCalculationTimes",
        //        "-TemperatureProfileIndex",
        //        "0",
        //        "-GeographicLocationIndex",
        //        "10",
        //        "-StartDate",
        //        new DateTime(2018,1,1).ToString(CultureInfo.CurrentCulture),
        //        "-EndDate",
        //        new DateTime(2018,12,31).ToString(CultureInfo.CurrentCulture),
        //        "-EnergyIntensityType",
        //        "Random",
        //        "-ExternalTimeResolution",
        //        "00:15",
        //        "-OutputFileDefault",
        //        "OnlySums",
        //        "-CalcOption",
        //        "LocationsFile"
        //    };
        //        MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
        //        wd.CleanUp(1);
        //    }
        //}

        //[Fact]
        //[Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        //public void MainTestListHouses()
        //{
        //    using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
        //    {
        //        var arguments = new List<string>
        //    {
        //        "List",
        //        "-Houses"
        //    };
        //        MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
        //        wd.CleanUp(1);
        //    }
        //}

        //[Fact]
        //[Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        //public void MainTestListModularHouseholds()
        //{
        //    using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
        //    {
        //        var arguments = new List<string>
        //    {
        //        "List",
        //        "-ModularHouseholds"
        //    };
        //        MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
        //        wd.CleanUp(1);
        //    }
        //}

        //[Fact]
        //[Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        //public void MainTestListSettlements()
        //{
        //    Config.CatchErrors = false;
        //    using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
        //    {
        //        var arguments = new List<string>
        //    {
        //        "List",
        //        "-Settlements"
        //    };
        //        MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
        //        wd.CleanUp(1);
        //    }
        //}

        //[Fact]
        //[Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        //public void MainTestListTemperatureProfiles()
        //{
        //    using (var wd = SetupDB3(Utili.GetCurrentMethodAndClass()))
        //    {
        //        var arguments = new List<string>
        //    {
        //        "List",
        //        "-TemperatureProfiles"
        //    };
        //        MainSimEngine.Run(arguments.ToArray(), "simulationengine.exe");
        //        wd.CleanUp(1);
        //    }
        //}

        /// <summary>
        /// Some commands of the simulationengine.exe should immediately throw an exception, if the db3 file is
        /// not present in the same directory. This test checks if these exceptions are thrown.
        /// </summary>
        /// <remarks>Some commands should not immediately throw an exception as they can use a db3 file located
        /// elsewhere</remarks>
        [Fact]
        [Trait(UnitTestCategories.Category, UnitTestCategories.BasicTest)]
        public void MainTestNoDB3() {
            Config.CatchErrors = false;
            if (File.Exists("profilegenerator.db3")) {
                File.Delete("profilegenerator.db3");
            }
            Config.IsInUnitTesting = true;
            // list all commands that should immediately throw an exception if the db3 file is missing
            var argLists = new List<string[]> {
                new string[] { "CSVImport", "-i", "myfile.csv", "-d", ";", "-n", "myprofile" },
                new string[] { "ImportHouseholdDefinition", "-File", "myfile.csv" },
                new string[] { "CreateExampleHouseJob" },
                new string[] { "CreatePythonBindings" },
                new string[] { "ExportDatabaseObjectsAsJson", "-t", "HouseholdTemplates", "-o", "myexport.json"},
                new string[] { "ImportDatabaseObjectsAsJson", "-t", "HouseholdTemplates",  "-i", "myimport.json" },
            };
            // test if the expected exception is thrown
            foreach (var args in argLists) {
                Assert.Throws<LPGException>(() => MainSimEngine.Run(args.ToArray(), "simulationengine.exe"));
            }
        }

        [Fact]
        public void TestClearTemplated()
        {
            var db = new DatabaseSetup(Utili.GetCurrentMethodAndClass());
            Simulator sim = new Simulator(db.ConnectionString);
            sim.FindAndDeleteAllTemplated();
            var res2 = sim.FindAndDeleteAllTemplated();
            if (res2 != 0)
            {
                throw new LPGException("Found templated");
            }
        }
        public ProgramTests([JetBrains.Annotations.NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }

}