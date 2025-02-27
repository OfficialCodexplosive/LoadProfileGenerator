﻿using System.IO;
using Automation;
using ChartCreator2.OxyCharts;
using Common;
using Common.Tests;
using Xunit;
using Xunit.Abstractions;

namespace ChartCreator2.Tests.Oxyplot {

    public class ChartGeneratorTests : UnitTestBaseClass
    {
        [StaFact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.LongTest3)]
        public void RunChartGeneratorTests()
        {
            Config.ReallyMakeAllFilesIncludingBothSums = true;
            CleanTestBase.RunAutomatically(false);
            var cs = new OxyCalculationSetup(Utili.GetCurrentMethodAndClass());
            cs.StartHousehold(1, GlobalConsts.CSVCharacter,
                configSetter: x =>
                {
                    x.ApplyOptionDefault(OutputFileDefault.All);
                    x.Disable(CalcOption.MakeGraphics);
                    x.Disable(CalcOption.MakePDF);
                });
            CalculationProfiler cp = new CalculationProfiler();
            ChartCreationParameters ccp = new ChartCreationParameters(144, 2000, 1000, false, GlobalConsts.CSVCharacter,
                new DirectoryInfo(cs.DstDir));
            using (var fft = cs.GetFileTracker())
            {
                ChartGeneratorManager cgm = new ChartGeneratorManager(cp, fft, ccp);
                Logger.Info("Making picture");
                cgm.Run(cs.DstDir);
            }
            Logger.Info("finished picture");
            cs.CleanUp();
            CleanTestBase.RunAutomatically(true);
        }

        public ChartGeneratorTests([JetBrains.Annotations.NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}